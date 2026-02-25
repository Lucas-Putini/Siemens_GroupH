using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OnboardingCanvasPlayer : MonoBehaviour
{
    [Serializable]
    public class Slide
    {
        public string title;
        [TextArea(3, 10)] public string body;
    }

    [Header("What to show/hide")]
    [SerializeField] private GameObject root; // set to Case1Panel (recommended)

    [Header("Single TMP text box")]
    [SerializeField] private TMP_Text onboardingText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton; // X
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;

    [Header("Behavior")]
    [SerializeField] private bool showOnStart = true;

    [SerializeField]
    private Slide[] slides = new Slide[]
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

    private int index;

    private void Awake()
    {
        if (root == null) root = gameObject;

        if (closeButton != null) closeButton.onClick.AddListener(() => root.SetActive(false));
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (backButton != null) backButton.onClick.AddListener(Back);
    }

    private void Start()
    {
        if (showOnStart) { root.SetActive(true); Apply(); }
        else root.SetActive(false);
    }

    private void Next()
    {
        index = Mathf.Min(index + 1, slides.Length - 1);
        Apply();
    }

    private void Back()
    {
        index = Mathf.Max(index - 1, 0);
        Apply();
    }

    private void Apply()
    {
        if (slides == null || slides.Length == 0 || onboardingText == null) return;

        var s = slides[index];

        // Smaller title so it doesn't push body into buttons
        onboardingText.text = $"<size=115%><b>{s.title}</b></size>\n\n{s.body}";

        // Update button states
        if (backButton != null) backButton.interactable = index > 0;
        if (nextButton != null) nextButton.interactable = index < slides.Length - 1;

        // Force TMP refresh (helps in builds sometimes)
        onboardingText.ForceMeshUpdate();
    }
}