using UnityEngine;

/// <summary>
/// Scenario gate that becomes open when a <see cref="SurfaceSnapZone"/> reports all items snapped
/// (or at least a required count).
/// </summary>
[AddComponentMenu("Siemens/Scenario/Surface Snap Gate")]
[DisallowMultipleComponent]
public sealed class SurfaceSnapGate : ScenarioGate
{
    [SerializeField]
    SurfaceSnapZone m_SnapZone;

    [SerializeField]
    [Tooltip("If > 0, gate opens when SnappedCount >= this. If <= 0, requires all items in the snap zone.")]
    int m_RequiredSnappedCount = 0;

    public override bool IsOpen
    {
        get
        {
            if (m_SnapZone == null)
                return false;

            if (m_RequiredSnappedCount > 0)
                return m_SnapZone.SnappedCount >= m_RequiredSnappedCount;

            return m_SnapZone.AllSnapped;
        }
    }
}

