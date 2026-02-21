using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Optional: moves the train along a <see cref="RailwayPath"/> when it's not being grabbed.
/// Uses simple "snap-to-closest" stepping, so it never leaves the rails.
/// </summary>
[AddComponentMenu("Siemens/Interaction/Train On Rail Auto Mover")]
[DisallowMultipleComponent]
public sealed class TrainOnRailAutoMover : MonoBehaviour
{
    [SerializeField]
    RailwayPath m_Path;

    [SerializeField]
    [Tooltip("World units per second along the rail direction.")]
    float m_Speed = 0.5f;

    [SerializeField]
    [Tooltip("Direction along the rail. Use 1 for forward, -1 for reverse.")]
    int m_Direction = 1;

    [SerializeField]
    [Tooltip("If set, auto-move is paused while this interactable is selected (grabbed).")]
    XRGrabInteractable m_GrabInteractable;

    [SerializeField]
    [Tooltip("If enabled, rotation is aligned to the rail tangent while auto-moving.")]
    bool m_AlignRotationToRail = true;

    [SerializeField]
    Vector3 m_UpVector = Vector3.up;

    public RailwayPath Path
    {
        get => m_Path;
        set => m_Path = value;
    }

    public float Speed
    {
        get => m_Speed;
        set => m_Speed = value;
    }

    /// <summary>
    /// Direction along the path tangent. Set to 1 (forward) or -1 (reverse).
    /// </summary>
    public int Direction
    {
        get => m_Direction >= 0 ? 1 : -1;
        set => m_Direction = value >= 0 ? 1 : -1;
    }

    void Reset()
    {
        m_GrabInteractable = GetComponent<XRGrabInteractable>();
    }

    void Update()
    {
        if (m_Path == null)
            return;

        if (m_GrabInteractable != null && m_GrabInteractable.isSelected)
            return;

        // Always keep snapped to the rail (even when stopped), then optionally move along the tangent.
        if (!m_Path.TryGetClosestPoint(transform.position, out var closestNow, out var tangentNow))
            return;

        transform.position = closestNow;

        var up = m_UpVector.sqrMagnitude > 1e-6f ? m_UpVector.normalized : Vector3.up;
        var dirSign = (m_Speed >= 0f ? 1f : -1f) * (m_Direction >= 0 ? 1f : -1f);
        var moveTangent = tangentNow * dirSign;

        if (m_AlignRotationToRail)
        {
            if (Vector3.Cross(moveTangent, up).sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(moveTangent, up);
        }

        if (Mathf.Abs(m_Speed) <= 1e-6f)
            return;

        // Step along the rail direction, then snap back to the rail.
        // This keeps the train constrained without needing a spline-distance parameter.
        var proposed = closestNow + moveTangent.normalized * (Mathf.Abs(m_Speed) * Time.deltaTime);

        if (!m_Path.TryGetClosestPoint(proposed, out var closest, out var tangent))
            return;

        transform.position = closest;

        if (m_AlignRotationToRail)
        {
            var desired = tangent * dirSign;
            if (Vector3.Cross(desired, up).sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(desired, up);
        }
    }
}

