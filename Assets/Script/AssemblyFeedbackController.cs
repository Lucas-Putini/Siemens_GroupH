using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// Observes chip snap completion without modifying ChipSnapZone.
/// A slot is considered complete when the configured chip is parented under the slot snap target.
/// </summary>
[AddComponentMenu("Siemens/Feedback/Assembly Feedback Controller")]
[DisallowMultipleComponent]
public sealed class AssemblyFeedbackController : MonoBehaviour
{
    [Serializable]
    public struct SlotBinding
    {
        [Tooltip("Snap zone that receives this chip.")]
        public ChipSnapZone zone;

        [Tooltip("Expected chip for this snap zone.")]
        public ChipId chip;
    }

    [Serializable]
    public sealed class ProgressEvent : UnityEvent<int, int> { }

    [Header("Slots")]
    [SerializeField]
    [Tooltip("List of zone + chip pairs that must be snapped.")]
    SlotBinding[] m_Slots = Array.Empty<SlotBinding>();

    [Header("Audio")]
    [SerializeField]
    [Tooltip("Optional source used to play feedback clips. If empty, one is auto-created at runtime.")]
    AudioSource m_AudioSource;

    [SerializeField]
    [Tooltip("Played once whenever a new slot is completed.")]
    AudioClip m_OnSnapClip;

    [SerializeField]
    [Range(0f, 1f)]
    float m_OnSnapVolume = 1f;

    [SerializeField]
    [Tooltip("Played once when all slots are complete.")]
    AudioClip m_OnAllCompleteClip;

    [SerializeField]
    [Range(0f, 1f)]
    float m_OnAllCompleteVolume = 1f;

    [Header("Text Feedback")]
    [SerializeField]
    [Tooltip("Progress format used while assembly is in progress. {0}=snapped, {1}=total.")]
    string m_ProgressFormat = "Board assembly: {0}/{1}";

    [SerializeField]
    [Tooltip("Shown once all slots are complete.")]
    string m_AllCompleteText = "2/2 - Place board on table";

#if TMP_PRESENT
    [SerializeField]
    [Tooltip("Optional TextMeshPro target for feedback text.")]
    TMP_Text m_TmpText;
#endif

    [SerializeField]
    [Tooltip("Optional legacy UI Text target for feedback text.")]
    Text m_UiText;

    [Header("Events")]
    [SerializeField]
    [Tooltip("Invoked whenever progress changes: (snappedCount, totalCount).")]
    ProgressEvent m_OnProgressChanged = new ProgressEvent();

    [SerializeField]
    [Tooltip("Invoked once when all configured slots are complete.")]
    UnityEvent m_OnAssemblyReady = new UnityEvent();

    bool[] m_SlotCompleted;
    int m_CurrentSnappedCount;
    bool m_ReadyRaised;

    public int TotalSlots => m_Slots?.Length ?? 0;
    public int SnappedCount => m_CurrentSnappedCount;
    public bool IsAssemblyReady => TotalSlots > 0 && m_CurrentSnappedCount >= TotalSlots;

    void Awake()
    {
        EnsureState();

        if (m_AudioSource == null)
            m_AudioSource = GetComponent<AudioSource>();

        if (m_AudioSource == null && (m_OnSnapClip != null || m_OnAllCompleteClip != null))
        {
            m_AudioSource = gameObject.AddComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
            m_AudioSource.spatialBlend = 0f;
        }
    }

    void OnEnable()
    {
        EnsureState();
        RecalculateState(fireEvents: false);
        UpdateFeedbackText();
    }

    void OnValidate()
    {
        EnsureState();
    }

    void Update()
    {
        RecalculateState(fireEvents: true);
    }

    void EnsureState()
    {
        var total = TotalSlots;
        if (m_SlotCompleted == null || m_SlotCompleted.Length != total)
            m_SlotCompleted = new bool[total];
    }

    void RecalculateState(bool fireEvents)
    {
        EnsureState();

        var previousCount = m_CurrentSnappedCount;
        var snappedNow = 0;

        for (var i = 0; i < TotalSlots; i++)
        {
            var isComplete = IsSlotComplete(m_Slots[i]);
            if (isComplete) snappedNow++;

            if (isComplete && !m_SlotCompleted[i] && fireEvents)
                PlayOneShot(m_OnSnapClip, m_OnSnapVolume);

            m_SlotCompleted[i] = isComplete;
        }

        m_CurrentSnappedCount = snappedNow;

        if (m_CurrentSnappedCount != previousCount)
        {
            if (fireEvents)
                m_OnProgressChanged?.Invoke(m_CurrentSnappedCount, TotalSlots);

            UpdateFeedbackText();
        }

        var readyNow = IsAssemblyReady;
        if (readyNow && !m_ReadyRaised)
        {
            m_ReadyRaised = true;
            if (fireEvents)
            {
                PlayOneShot(m_OnAllCompleteClip, m_OnAllCompleteVolume);
                m_OnAssemblyReady?.Invoke();
            }
            UpdateFeedbackText();
        }
        else if (!readyNow && m_ReadyRaised)
        {
            // Supports reset/unsnap scenarios.
            m_ReadyRaised = false;
            UpdateFeedbackText();
        }
    }

    bool IsSlotComplete(SlotBinding slot)
    {
        if (slot.zone == null || slot.chip == null)
            return false;

        if (slot.chip.type != slot.zone.accepts)
            return false;

        var target = slot.zone.snapTarget != null ? slot.zone.snapTarget : slot.zone.transform;
        if (target == null)
            return false;

        return slot.chip.transform.IsChildOf(target);
    }

    void UpdateFeedbackText()
    {
        var text = IsAssemblyReady
            ? m_AllCompleteText
            : string.Format(m_ProgressFormat, m_CurrentSnappedCount, TotalSlots);

#if TMP_PRESENT
        if (m_TmpText != null)
            m_TmpText.text = text;
#endif
        if (m_UiText != null)
            m_UiText.text = text;
    }

    void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip == null || m_AudioSource == null)
            return;

        m_AudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
