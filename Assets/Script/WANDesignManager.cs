using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// WANDesignManager:  
/// • Manages selection between WAN, LAN, and VPN modes  
/// • Orchestrates WAN node & connection setup  
/// • Delegates LAN to LANMeshManager  
/// </summary>
public class WANDesignManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown networkDropdown;           // Dropdown to pick network type

    [Header("WAN References")]
    public WANNodeManager nodeManager;             // Generates WAN nodes
    public WANConnectionManager connectionManager; // Draws WAN connections
    public WANPathUIManager uiManager;             // Manages WAN path UI

    [Header("LAN Reference")]
    public LANMeshManager lanMeshManager;          // Handles LAN canvas & mesh scenario

    [Header("Earth Setup")]
    public GameObject earth;                       // Earth model for WAN
    public Transform nodesContainer;               // Parent for WAN nodes under Earth
    public Transform linesContainer;               // Parent for WAN lines under Earth

    /// <summary>
    /// Unity Start:  
    /// 1. Populate the networkDropdown  
    /// 2. Bind OnNetworkSelected callback  
    /// 3. Configure WAN containers under earth  
    /// </summary>
    void Start()
    {
        if (networkDropdown != null)
        {
            networkDropdown.ClearOptions();
            networkDropdown.AddOptions(new List<string>
            {
                "Select Network",
                "WAN Design",
                "LAN Design",    // NEW
                "VPN Design"
            });
            networkDropdown.onValueChanged.AddListener(OnNetworkSelected);
            networkDropdown.value = 0;  // default to 'Select'
        }

        // Parent WAN containers under Earth so they move/rotate together
        nodesContainer.SetParent(earth.transform, false);
        linesContainer.SetParent(earth.transform, false);
        ResetContainers();

        // Give WAN managers their references
        nodeManager.earth = earth;
        nodeManager.nodeContainer = nodesContainer;
        connectionManager.lineContainer = linesContainer;
    }

    /// <summary>
    /// Reset transforms of node/line containers to identity (zero position/rotation, unit scale)
    /// </summary>
    void ResetContainers()
    {
        nodesContainer.localPosition = Vector3.zero;
        nodesContainer.localRotation = Quaternion.identity;
        nodesContainer.localScale = Vector3.one;

        linesContainer.localPosition = Vector3.zero;
        linesContainer.localRotation = Quaternion.identity;
        linesContainer.localScale = Vector3.one;
    }

    /// <summary>
    /// Called whenever networkDropdown.value changes.
    /// Routes to WAN flow, LAN flow, or a full clear for VPN/Select.
    /// </summary>
    void OnNetworkSelected(int index)
    {
        string selected = networkDropdown.options[index].text;

        if (selected == "WAN Design")
        {
            // --- WAN Flow ---
            ResetContainers();
            nodeManager.ClearExistingNodes();
            connectionManager.ClearLines();
            nodeManager.GenerateNodes();

            float radius = GetEarthRadius() * nodeManager.radiusMultiplier;
            connectionManager.ConnectAndDraw(nodeManager.GeneratedNodes, radius);

            uiManager.Initialize(nodeManager.GeneratedNodes);

            // Hide LAN canvas if previously visible
            if (lanMeshManager != null && lanMeshManager.lanCanvas != null)
                lanMeshManager.lanCanvas.gameObject.SetActive(false);
        }
        else if (selected == "LAN Design")
        {
            // --- LAN Flow ---
            // 1) Clear out any WAN visuals/UI
            nodeManager.ClearExistingNodes();
            connectionManager.ClearLines();
            uiManager.ClearUI();

            // 2) Show LAN canvas & start the rectangle animation
            if (lanMeshManager != null)
            {
                lanMeshManager.lanCanvas.gameObject.SetActive(true);
                lanMeshManager.lanCanvas.gameObject.SetActive(true);
            }
        }
        else
        {
            // --- VPN or Select Network: wipe all visuals/UI ---
            nodeManager.ClearExistingNodes();
            connectionManager.ClearLines();
            uiManager.ClearUI();

            if (lanMeshManager != null && lanMeshManager.lanCanvas != null)
                lanMeshManager.lanCanvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Fully clears WAN and LAN, used by ResetAll()
    /// </summary>
    public void ClearAll()
    {
        nodeManager.ClearExistingNodes();
        connectionManager.ClearLines();
        uiManager.ClearUI();
        if (lanMeshManager != null && lanMeshManager.lanCanvas != null)
            lanMeshManager.lanCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Resets the dropdown to default and invokes ClearAll().
    /// </summary>
    public void ResetAll()
    {
        if (networkDropdown != null)
        {
            networkDropdown.onValueChanged.RemoveListener(OnNetworkSelected);
            networkDropdown.value = 0;
            networkDropdown.RefreshShownValue();
            networkDropdown.onValueChanged.AddListener(OnNetworkSelected);
        }
        ClearAll();
    }

    /// <summary>
    /// Computes Earth’s visible radius: SphereCollider.radius * max(worldScale).
    /// </summary>
    float GetEarthRadius()
    {
        SphereCollider sc = earth.GetComponent<SphereCollider>();
        float maxScale = Mathf.Max(
            earth.transform.localScale.x,
            earth.transform.localScale.y,
            earth.transform.localScale.z);
        return sc.radius * maxScale;
    }
}
