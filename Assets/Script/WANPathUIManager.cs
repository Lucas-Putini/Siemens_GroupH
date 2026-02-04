using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// WANPathUIManager handles all UI interactions for selecting nodes, sending messages,
/// and displaying path animations and notifications.
/// Implements:
/// - Collections: List<T> for node names and WANNode lists (Project Req: Lists & Dictionaries)
/// - Coroutines: IEnumerator for delayed UI behavior (Project Req: Coroutines & IEnumerator)
/// References external scripts:
/// - WANDesignManager for resetting networks
/// - WANNodeManager for accessing generated nodes
/// - WANConnectionManager (path computation via WANPathfinder)
/// - WANPathVisualizer for highlighting and animating paths
/// </summary>
public class WANPathUIManager : MonoBehaviour
{
    [Header("WAN Managers")]
    public WANDesignManager designManager;     // Manages network creation/reset (WANDesignManager.cs)
    public WANNodeManager nodeManager;         // Generates and stores WANNode objects (WANNodeManager.cs)
    public WANConnectionManager connectionManager; // Provides WANConnectionManager.LineMap & network data
    public WANPathVisualizer pathVisualizer;   // Visualizes paths (WANPathVisualizer.cs)

    [Header("UI Elements")]
    public TMP_Dropdown networkDropdown;       // Dropdown for selecting different generated networks
    public TMP_Dropdown caseSelector;          // Dropdown to select use cases (UI Panel control)
    public GameObject case1Panel;              // Panel for "Case 1" UI elements
    public TMP_Text descriptionText;           // Text field for dynamic descriptions
    public TMP_InputField messageInput;        // Input for user message (simulated)
    public TMP_Dropdown fromDropdown;          // Dropdown for "from" node selection
    public TMP_Dropdown toDropdown;            // Dropdown for "to" node selection
    public Button sendButton;                  // Button to trigger path calculation & animation
    public Button closeButton;                 // Button to close case1Panel
    public Button resetButton;                 // Button to reset network (calls WANDesignManager.ResetAll)
    public GameObject notificationPanel;       // Panel for sending UI notifications
    public TMP_Text notificationText;          // Text element inside notificationPanel

    // Internal list of all nodes used to populate dropdowns
    private List<WANNode> allNodes = new();   // Uses System.Collections.Generic.List<WANNode>

    /// <summary>
    /// Unity Start: binds UI callbacks and initializes dropdowns if nodes exist.
    /// </summary>
    void Start()
    {
        // Bind case selector change (Coroutine not needed here)
        if (caseSelector != null)
            caseSelector.onValueChanged.AddListener(OnCaseSelected);

        // Bind close button to hide Case1 panel
        if (closeButton != null)
            closeButton.onClick.AddListener(() => case1Panel.SetActive(false));

        // Bind send button to path computation
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendClicked);

        // Bind reset button to network reset
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        // Hide panels by default
        if (case1Panel != null)
            case1Panel.SetActive(false);
        if (notificationPanel != null)
            notificationPanel.SetActive(false);

        // Initialize dropdowns if nodeManager has generated nodes
        if (nodeManager != null && nodeManager.GeneratedNodes.Count > 0)
            Initialize(nodeManager.GeneratedNodes); // Fill "from"/"to" dropdowns
    }

    /// <summary>
    /// Populates "from" and "to" dropdowns with node names.
    /// Called on fresh network generation.
    /// </summary>
    public void Initialize(List<WANNode> nodes)
    {
        allNodes = nodes ?? new List<WANNode>();      // Ensure non-null list

        fromDropdown.ClearOptions();                  // Clear previous entries
        toDropdown.ClearOptions();

        // Build list of names from WANNode.name (List<string>)
        List<string> names = new List<string>();
        foreach (WANNode node in allNodes)
            names.Add(node.name);

        // Add options to dropdowns
        fromDropdown.AddOptions(names);
        toDropdown.AddOptions(names);

        // Reset selections without triggering listeners
        fromDropdown.SetValueWithoutNotify(0);
        toDropdown.SetValueWithoutNotify(allNodes.Count > 1 ? 1 : 0);

        // Refresh UI display
        fromDropdown.RefreshShownValue();
        toDropdown.RefreshShownValue();

        // Clear any previous message input
        if (messageInput != null)
            messageInput.text = string.Empty;
    }

    /// <summary>
    /// Clears UI elements to default state and clears visual highlights.
    /// </summary>
    public void ClearUI()
    {
        if (caseSelector != null)
            caseSelector.value = 0;
        if (case1Panel != null)
            case1Panel.SetActive(false);
        if (fromDropdown != null && fromDropdown.options.Count > 0)
            fromDropdown.value = 0;
        if (toDropdown != null && toDropdown.options.Count > 1)
            toDropdown.value = 1;
        if (messageInput != null)
            messageInput.text = string.Empty;

        // Clear any path highlights
        if (pathVisualizer != null)
            pathVisualizer.ClearHighlight(); // Calls WANPathVisualizer.ClearHighlight()
    }

    /// <summary>
    /// Listener for case selection dropdown.
    /// Toggles visibility of Case 1 panel based on selected option text.
    /// </summary>
    void OnCaseSelected(int index)
    {
        if (caseSelector == null || case1Panel == null) return;
        string selected = caseSelector.options[index].text;
        case1Panel.SetActive(selected == "Case 1");
    }

    /// <summary>
    /// Triggered by sendButton: computes shortest path,
    /// then highlights and animates via WANPathVisualizer,
    /// and shows a timed notification using Coroutine.
    /// </summary>
    void OnSendClicked()
    {
        // Stop any ongoing animation
        if (pathVisualizer != null)
            pathVisualizer.StopAllCoroutines(); // Coroutine management

        int fromIndex = fromDropdown != null ? fromDropdown.value : 0;
        int toIndex = toDropdown != null ? toDropdown.value : 0;

        // Validate distinct selection
        if (fromIndex == toIndex)
        {
            Debug.LogWarning("You must select two different nodes.");
            return;
        }

        // Validate indices within bounds
        if (allNodes == null || fromIndex >= allNodes.Count || toIndex >= allNodes.Count)
        {
            Debug.LogWarning("Node index out of range.");
            return;
        }

        // Lookup WANNode objects
        WANNode fromNode = allNodes[fromIndex];
        WANNode toNode = allNodes[toIndex];

        // Compute shortest path via static WANPathfinder (Project Req: Graph Algorithms)
        List<WANNode> path = WANPathfinder.FindShortestPath(fromNode, toNode);

        if (path != null && path.Count > 1)
        {
            // Highlight and animate signal
            if (pathVisualizer != null)
            {
                pathVisualizer.HighlightPath(path);          // Highlights nodes & lines
                pathVisualizer.AnimateSignalAlongPath(path); // Animates sphere
            }

            // Hide case1Panel and show notification
            if (case1Panel != null)
                case1Panel.SetActive(false);
            if (notificationPanel != null && notificationText != null)
            {
                notificationText.text = "Please take attention on Network around earth.";
                notificationPanel.SetActive(true);
                // Hide notification after delay via Coroutine
                StartCoroutine(HideNotificationAfterDelay(15f));
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No valid path found between selected nodes.");
            // Clear any partial highlights
            if (pathVisualizer != null)
                pathVisualizer.ClearHighlight();
        }
    }

    /// <summary>
    /// Coroutine: waits for specified seconds then hides notificationPanel.
    /// Uses UnityEngine.WaitForSeconds (Project Req: Coroutines & IEnumerator)
    /// </summary>
    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    /// <summary>
    /// Listener for resetButton: resets all networks via WANDesignManager.ResetAll().
    /// </summary>
    void OnResetClicked()
    {
        if (designManager != null)
            designManager.ResetAll(); // Resets network generation
        else
            Debug.LogWarning("designManager reference missing on UI Manager!");
    }
}
