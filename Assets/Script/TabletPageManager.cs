using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// TabletPageManager: Manages a set of topic pages, showing one at a time via UI Toggles.
/// Implements:
/// - Structs: Serializable TopicPage struct grouping a Toggle and GameObject (Project Req: Structs)
/// - Collections: List<TopicPage> for dynamic page management (Project Req: Lists & Collections)
/// - Event handling: Toggle.onValueChanged listeners for UI interaction (Project Req: Unity Events)
/// - Loops & Conditionals: iterating topics to show/hide pages (Project Req: Control Flow)
/// </summary>
public class TabletPageManager : MonoBehaviour
{
    [System.Serializable]
    /// <summary>
    /// Serializable struct to pair a UI Toggle with a corresponding page GameObject.
    /// Demonstrates struct usage and serialization for Unity inspector (Project Req: Structs & Attributes)
    /// </summary>
    public struct TopicPage
    {
        public Toggle toggle;      // UI Toggle component (UnityEngine.UI)
        public GameObject page;    // GameObject representing the page content
    }

    [Header("Topics")]
    public List<TopicPage> topics;  // List of topic pages to manage (List<T>, Project Req: Collections)

    /// <summary>
    /// Unity Start: binds Toggle events and initializes first page.
    /// </summary>
    void Start()
    {
        // Loop through each topic to bind its toggle listener
        for (int i = 0; i < topics.Count; i++)
        {
            int idx = i;  // Capture index for closure
            topics[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn) ShowPage(idx);  // Show page when toggle is activated
            });
        }

        // Show the first page on startup
        ShowPage(0);
        topics[0].toggle.isOn = true;
    }

    /// <summary>
    /// Activates the selected page and deactivates all others.
    /// Uses loop and conditional to set GameObject active states and toggle values.
    /// </summary>
    void ShowPage(int index)
    {
        for (int i = 0; i < topics.Count; i++)
        {
            bool isActive = (i == index);
            topics[i].page.SetActive(isActive);       // Show/hide page
            topics[i].toggle.isOn = isActive;         // Update toggle UI
        }
    }
}
