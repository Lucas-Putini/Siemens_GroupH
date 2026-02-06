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
    [Tooltip("If set, auto-move is paused while this interactable is selected (grabbed).")]
    XRGrabInteractable m_GrabInteractable;

    [SerializeField]
    [Tooltip("If enabled, rotation is aligned to the rail tangent while auto-moving.")]
    bool m_AlignRotationToRail = true;

    [SerializeField]
    Vector3 m_UpVector = Vector3.up;

    void Reset()
    {
        m_GrabInteractable = GetComponent<XRGrabInteractable>();
    }

    void Update()
    {
        if (m_Path == null || m_Speed == 0f)
            return;

        if (m_GrabInteractable != null && m_GrabInteractable.isSelected)
            return;

        // Step forward in the current forward direction, then snap back to the rail.
        // This keeps the train constrained without needing a spline-distance parameter.
        var proposed = transform.position + transform.forward * (m_Speed * Time.deltaTime);

        if (!m_Path.TryGetClosestPoint(proposed, out var closest, out var tangent))
            return;

        transform.position = closest;

        if (m_AlignRotationToRail)
        {
            var up = m_UpVector.sqrMagnitude > 1e-6f ? m_UpVector.normalized : Vector3.up;
            if (Vector3.Cross(tangent, up).sqrMagnitude < 1e-6f)
                return;

            transform.rotation = Quaternion.LookRotation(tangent, up);
        }
    }
}

