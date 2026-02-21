using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A reusable "gate" that can enable/disable scenarios (e.g., train movement) based on
/// whether a user has assembled devices correctly.
/// </summary>
[AddComponentMenu("Siemens/Scenario/Scenario Gate (Base)")]
public abstract class ScenarioGate : MonoBehaviour
{
    [Serializable]
    public sealed class BoolEvent : UnityEvent<bool> { }

    [SerializeField]
    [Tooltip("Invoked whenever the gate open state changes.")]
    BoolEvent m_OnGateChanged = new BoolEvent();

    bool m_Last;
    bool m_HasLast;

    public abstract bool IsOpen { get; }

    public event Action<bool> GateChanged;

    protected virtual void OnEnable()
    {
        // Force initial event emission on first Update.
        m_HasLast = false;
    }

    protected virtual void Update()
    {
        var now = IsOpen;
        if (m_HasLast && now == m_Last)
            return;

        m_Last = now;
        m_HasLast = true;
        GateChanged?.Invoke(now);
        m_OnGateChanged?.Invoke(now);
    }
}

