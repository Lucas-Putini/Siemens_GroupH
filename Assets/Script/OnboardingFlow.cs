using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Lightweight onboarding flow that reuses the existing Case1Panel UI.
/// It is designed for fast PoC delivery: minimal setup and no prefab rebuild.
/// </summary>
[AddComponentMenu("Siemens/UI/Onboarding Flow")]
[DisallowMultipleComponent]
public sealed class OnboardingFlow : MonoBehaviour
{
    [Serializable]
    public struct Slide
    {
        public string title;

        [TextArea(3, 8)]
        public string body;
    }

    [Header("UI")]
    [SerializeField]
    GameObject m_PanelRoot;

    [SerializeField]
    TMP_Text m_GuideText;

    [SerializeField]
    Button m_NextButton;

    [SerializeField]
    TMP_Text m_NextButtonLabel;

    [SerializeField]
    Button m_SkipButton;

    [Header("Flow")]
    [SerializeField]
    Slide[] m_Slides = Array.Empty<Slide>();

    [SerializeField]
    bool m_ShowOnlyOnce = true;

    [SerializeField]
    bool m_ForceShow;

    [SerializeField]
    string m_PlayerPrefsKey = "siemens.grouph.onboarding.completed";

    [SerializeField]
    bool m_HidePanelWhenFinished = true;

    [SerializeField]
    bool m_RestoreHiddenObjectsOnFinish;

    [Header("Editor Test")]
    [SerializeField]
    bool m_EnableEditorHotkeys = true;

    [SerializeField]
    KeyCode m_NextKey = KeyCode.RightArrow;

    [SerializeField]
    KeyCode m_AltNextKey = KeyCode.Space;

    [SerializeField]
    KeyCode m_SkipKey = KeyCode.Escape;

    [Header("Optional: Hide Legacy UI During Onboarding")]
    [SerializeField]
    GameObject[] m_HideDuringOnboarding = Array.Empty<GameObject>();

    [Header("XR / 3D Interaction")]
    [Tooltip("If true, the panel background and guide text will not block raycasts. OVR hand/controller rays pass through to grabbable objects (Grabbable, HandGrabInteractable). Next/Skip buttons still receive input.")]
    [SerializeField]
    bool m_Allow3DInteractionDuringOnboarding = true;

    [Header("Events")]
    [SerializeField]
    UnityEvent m_OnCompleted = new UnityEvent();

    [SerializeField]
    UnityEvent m_OnSkipped = new UnityEvent();

    readonly Dictionary<GameObject, bool> m_PreviousActiveState = new Dictionary<GameObject, bool>();

    bool m_PanelRaycastTargetWasEnabled;
    bool m_PanelRaycastWasModified;
    Image m_PanelBackgroundImage;
    bool m_GuideTextRaycastTargetWasEnabled;
    bool m_GuideTextRaycastWasModified;

    int m_CurrentSlide;
    bool m_Running;

    void Reset()
    {
        if (m_PanelRoot == null)
            m_PanelRoot = gameObject;

        AutoBind();
        EnsureDefaultSlides();
        EnsureDefaultHiddenTargets();
    }

    void Awake()
    {
        if (m_PanelRoot == null)
            m_PanelRoot = gameObject;

        AutoBind();
        EnsureDefaultSlides();
        EnsureDefaultHiddenTargets();
    }

    void OnEnable()
    {
        if (ShouldSkipByPrefs())
        {
            if (m_HidePanelWhenFinished && m_PanelRoot != null)
                m_PanelRoot.SetActive(false);

            enabled = false;
            return;
        }

        if (!HasRequiredReferences())
        {
            Debug.LogWarning("[OnboardingFlow] Missing required references. Hiding panel so you can interact with the scene.");
            if (m_PanelRoot != null)
                m_PanelRoot.SetActive(false);
            enabled = false;
            return;
        }

        m_NextButton.onClick.AddListener(HandleNextClicked);
        m_SkipButton.onClick.AddListener(HandleSkipClicked);

        Begin();
    }

    void OnDisable()
    {
        SetPanelBlocksRaycasts(true);

        if (m_NextButton != null)
            m_NextButton.onClick.RemoveListener(HandleNextClicked);

        if (m_SkipButton != null)
            m_SkipButton.onClick.RemoveListener(HandleSkipClicked);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!m_EnableEditorHotkeys || !m_Running)
            return;

        if (IsNextPressed())
        {
            HandleNextClicked();
            return;
        }

        if (IsSkipPressed())
            HandleSkipClicked();
#endif
    }

    bool IsNextPressed()
    {
        if (Input.GetKeyDown(m_NextKey) || Input.GetKeyDown(m_AltNextKey))
            return true;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null)
            return false;

        if (kb.rightArrowKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            return true;
#endif

        return false;
    }

    bool IsSkipPressed()
    {
        if (Input.GetKeyDown(m_SkipKey))
            return true;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null)
            return false;

        if (kb.escapeKey.wasPressedThisFrame)
            return true;
#endif

        return false;
    }

    void Begin()
    {
        m_CurrentSlide = 0;
        m_Running = true;

        SetHiddenObjectsActive(false);
        SetPanelBlocksRaycasts(!m_Allow3DInteractionDuringOnboarding);
        ApplySlide();
    }

    bool ShouldSkipByPrefs()
    {
        if (m_ForceShow || !m_ShowOnlyOnce)
            return false;

        if (string.IsNullOrWhiteSpace(m_PlayerPrefsKey))
            return false;

        return PlayerPrefs.GetInt(m_PlayerPrefsKey, 0) == 1;
    }

    bool HasRequiredReferences()
    {
        return m_GuideText != null && m_NextButton != null && m_SkipButton != null && m_Slides.Length > 0;
    }

    void HandleNextClicked()
    {
        if (!m_Running)
            return;

        var nextIndex = m_CurrentSlide + 1;
        if (nextIndex >= m_Slides.Length)
        {
            Complete(skipped: false);
            return;
        }

        m_CurrentSlide = nextIndex;
        ApplySlide();
    }

    void HandleSkipClicked()
    {
        if (!m_Running)
            return;

        Complete(skipped: true);
    }

    public void DebugNextSlide()
    {
        HandleNextClicked();
    }

    public void DebugSkip()
    {
        HandleSkipClicked();
    }

    void ApplySlide()
    {
        var slide = m_Slides[Mathf.Clamp(m_CurrentSlide, 0, m_Slides.Length - 1)];

        if (!string.IsNullOrWhiteSpace(slide.title))
            m_GuideText.text = "<b>" + slide.title + "</b>\n\n" + slide.body;
        else
            m_GuideText.text = slide.body;

        if (m_NextButtonLabel != null)
        {
            var isLast = m_CurrentSlide >= m_Slides.Length - 1;
            m_NextButtonLabel.text = isLast ? "Start" : "Next";
        }
    }

    void Complete(bool skipped)
    {
        m_Running = false;

        SetPanelBlocksRaycasts(true);

        if (!string.IsNullOrWhiteSpace(m_PlayerPrefsKey))
        {
            PlayerPrefs.SetInt(m_PlayerPrefsKey, 1);
            PlayerPrefs.Save();
        }

        if (m_RestoreHiddenObjectsOnFinish)
            RestoreHiddenObjects();

        if (m_HidePanelWhenFinished && m_PanelRoot != null)
            m_PanelRoot.SetActive(false);

        if (skipped)
            m_OnSkipped.Invoke();
        else
            m_OnCompleted.Invoke();
    }

    void SetPanelBlocksRaycasts(bool blocks)
    {
        if (m_PanelRoot == null)
            return;

        if (m_PanelBackgroundImage == null)
        {
            m_PanelBackgroundImage = m_PanelRoot.GetComponent<Image>();
            if (m_PanelBackgroundImage == null)
                m_PanelBackgroundImage = m_PanelRoot.GetComponentInChildren<Image>(true);
        }

        if (blocks)
        {
            if (m_PanelBackgroundImage != null && m_PanelRaycastWasModified)
            {
                m_PanelBackgroundImage.raycastTarget = m_PanelRaycastTargetWasEnabled;
                m_PanelRaycastWasModified = false;
            }
            if (m_GuideText != null && m_GuideTextRaycastWasModified)
            {
                m_GuideText.raycastTarget = m_GuideTextRaycastTargetWasEnabled;
                m_GuideTextRaycastWasModified = false;
            }
        }
        else if (m_Allow3DInteractionDuringOnboarding)
        {
            if (m_PanelBackgroundImage != null)
            {
                m_PanelRaycastTargetWasEnabled = m_PanelBackgroundImage.raycastTarget;
                m_PanelBackgroundImage.raycastTarget = false;
                m_PanelRaycastWasModified = true;
            }
            if (m_GuideText != null)
            {
                m_GuideTextRaycastTargetWasEnabled = m_GuideText.raycastTarget;
                m_GuideText.raycastTarget = false;
                m_GuideTextRaycastWasModified = true;
            }
        }
    }

    void SetHiddenObjectsActive(bool active)
    {
        m_PreviousActiveState.Clear();

        foreach (var go in m_HideDuringOnboarding)
        {
            if (go == null)
                continue;

            if (!m_PreviousActiveState.ContainsKey(go))
                m_PreviousActiveState.Add(go, go.activeSelf);

            go.SetActive(active);
        }
    }

    void RestoreHiddenObjects()
    {
        foreach (var pair in m_PreviousActiveState)
        {
            if (pair.Key != null)
                pair.Key.SetActive(pair.Value);
        }
    }

    void AutoBind()
    {
        if (m_PanelRoot == null)
            m_PanelRoot = gameObject;

        if (m_GuideText == null)
        {
            var t = FindNamedChild(m_PanelRoot.transform, "Guide(text)");
            if (t != null)
                m_GuideText = t.GetComponent<TMP_Text>();
        }

        if (m_NextButton == null)
        {
            var t = FindNamedChild(m_PanelRoot.transform, "SendButton");
            if (t != null)
                m_NextButton = t.GetComponent<Button>();
        }

        if (m_NextButtonLabel == null && m_NextButton != null)
            m_NextButtonLabel = m_NextButton.GetComponentInChildren<TMP_Text>(true);

        if (m_SkipButton == null)
        {
            var t = FindNamedChild(m_PanelRoot.transform, "CloseMenu");
            if (t != null)
                m_SkipButton = t.GetComponent<Button>();
        }
    }

    void EnsureDefaultSlides()
    {
        if (m_Slides != null && m_Slides.Length > 0)
            return;

        m_Slides = new[]
        {
            new Slide
            {
                title = "Welcome",
                body = "Welcome to the Siemens Rail Training proof of concept.\n\nThis experience demonstrates how technicians can train virtually without travel."
            },
            new Slide
            {
                title = "Scenario",
                body = "A sensor fault is affecting train stop behavior across three stations.\n\nThe train keeps circulating and operations are not performing as expected."
            },
            new Slide
            {
                title = "Your Objective",
                body = "Inspect and repair the station sensor setup by placing the correct components in the correct positions."
            },
            new Slide
            {
                title = "How To Interact",
                body = "Grab parts, move them to the work area, and assemble the faulty sensor path.\n\nWhen the setup is corrected, the system can return to proper operation."
            },
            new Slide
            {
                title = "Success Criteria",
                body = "Complete the repair workflow to validate this virtual training concept for Siemens railway maintenance teams."
            }
        };
    }

    void EnsureDefaultHiddenTargets()
    {
        if (m_HideDuringOnboarding != null && m_HideDuringOnboarding.Length > 0)
            return;

        var names = new[]
        {
            "From",
            "To",
            "Message",
            "InputField (TMP)",
            "NotificationPanel",
            "NetworkDropdown",
            "CaseSelector",
            "ResetButton"
        };

        var unique = new List<GameObject>();
        foreach (var name in names)
        {
            var target = FindNamedObject(name);
            if (target != null && !unique.Contains(target))
                unique.Add(target);
        }

        m_HideDuringOnboarding = unique.ToArray();
    }

    [ContextMenu("Clear Onboarding Progress")]
    public void ClearOnboardingProgress()
    {
        if (string.IsNullOrWhiteSpace(m_PlayerPrefsKey))
            return;

        PlayerPrefs.DeleteKey(m_PlayerPrefsKey);
        PlayerPrefs.Save();
    }

    static GameObject FindNamedObject(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var tf in allTransforms)
        {
            if (tf == null || tf.gameObject == null)
                continue;

            if (!tf.gameObject.scene.IsValid())
                continue;

            if (tf.name == name)
                return tf.gameObject;
        }

        return null;
    }

    static Transform FindNamedChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            if (t.name == childName)
                return t;
        }

        return null;
    }
}

/// <summary>
/// Auto-attaches onboarding to the existing Case1Panel so the team can test immediately.
/// If you manually add OnboardingFlow in the scene, this bootstrap does nothing.
/// </summary>
public static class OnboardingFlowBootstrap
{
    const string k_PlayerPrefsKey = "siemens.grouph.onboarding.completed";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (UnityEngine.Object.FindFirstObjectByType<OnboardingFlow>() != null)
            return;

        var panel = GameObject.Find("Case1Panel");
        if (panel == null)
            return;

        if (panel.GetComponent<OnboardingFlow>() == null)
            panel.AddComponent<OnboardingFlow>();
    }

    public static void ClearCompletionFlag()
    {
        PlayerPrefs.DeleteKey(k_PlayerPrefsKey);
        PlayerPrefs.Save();
    }

    public static OnboardingFlow FindActiveFlow()
    {
        return UnityEngine.Object.FindFirstObjectByType<OnboardingFlow>(FindObjectsInactive.Include);
    }
}

#if UNITY_EDITOR
public static class OnboardingFlowEditorMenu
{
    [UnityEditor.MenuItem("Tools/Siemens/Onboarding/Clear Completion Flag")]
    static void ClearCompletionFlagMenu()
    {
        OnboardingFlowBootstrap.ClearCompletionFlag();
        Debug.Log("[OnboardingFlow] Completion flag cleared.");
    }

    [UnityEditor.MenuItem("Tools/Siemens/Onboarding/Next Slide (Play Mode)")]
    static void NextSlideMenu()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[OnboardingFlow] Enter Play Mode first.");
            return;
        }

        var flow = OnboardingFlowBootstrap.FindActiveFlow();
        if (flow == null)
        {
            Debug.LogWarning("[OnboardingFlow] No active flow found.");
            return;
        }

        flow.DebugNextSlide();
    }

    [UnityEditor.MenuItem("Tools/Siemens/Onboarding/Skip (Play Mode)")]
    static void SkipMenu()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[OnboardingFlow] Enter Play Mode first.");
            return;
        }

        var flow = OnboardingFlowBootstrap.FindActiveFlow();
        if (flow == null)
        {
            Debug.LogWarning("[OnboardingFlow] No active flow found.");
            return;
        }

        flow.DebugSkip();
    }
}
#endif
