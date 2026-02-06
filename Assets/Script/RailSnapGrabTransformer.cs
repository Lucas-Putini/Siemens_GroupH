using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

/// <summary>
/// XR grab transformer that constrains a grabbed object to a <see cref="RailwayPath"/>.
/// This is ideal for VR/MR: users can grab the train normally, but it will always snap to the track.
/// </summary>
[AddComponentMenu("Siemens/Interaction/Rail Snap Grab Transformer")]
public sealed class RailSnapGrabTransformer : XRBaseGrabTransformer
{
    [SerializeField]
    [Tooltip("The railway path (centerline) the object will be constrained to.")]
    RailwayPath m_Path;

    [SerializeField]
    [Tooltip("If enabled, only XZ will snap to the rail and the current Y offset will be preserved.")]
    bool m_PreserveVerticalOffset = true;

    [SerializeField]
    [Tooltip("If enabled, the object rotation will be aligned to the rail tangent while grabbed.")]
    bool m_AlignRotationToRail = true;

    [SerializeField]
    [Tooltip("Up vector used when aligning rotation to the rail tangent (typically world up).")]
    Vector3 m_UpVector = Vector3.up;

    [SerializeField]
    [Tooltip("Local forward axis of the object that should point along the rail (usually Z+).")]
    Vector3 m_LocalForwardAxis = Vector3.forward;

    /// <inheritdoc />
    protected override RegistrationMode registrationMode => RegistrationMode.SingleAndMultiple;

    float m_InitialYOffset;
    bool m_HasInitialYOffset;

    /// <inheritdoc />
    public override void OnLink(XRGrabInteractable grabInteractable)
    {
        base.OnLink(grabInteractable);
        m_HasInitialYOffset = false;
    }

    /// <inheritdoc />
    public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
    {
        if (m_Path == null)
            return;

        if (!m_Path.TryGetClosestPoint(targetPose.position, out var closest, out var tangent))
            return;

        if (m_PreserveVerticalOffset)
        {
            if (!m_HasInitialYOffset)
            {
                m_InitialYOffset = targetPose.position.y - closest.y;
                m_HasInitialYOffset = true;
            }

            closest.y += m_InitialYOffset;
        }

        targetPose.position = closest;

        if (m_AlignRotationToRail)
        {
            var up = m_UpVector.sqrMagnitude > 1e-6f ? m_UpVector.normalized : Vector3.up;

            // Flatten tangent if it's nearly vertical to avoid invalid LookRotation.
            if (Vector3.Cross(tangent, up).sqrMagnitude < 1e-6f)
                return;

            var desiredRailRotation = Quaternion.LookRotation(tangent, up);

            // Apply a correction so the chosen local forward axis points along the rail.
            var currentForwardWorld = targetPose.rotation * m_LocalForwardAxis.normalized;
            var forwardCorrection = Quaternion.FromToRotation(currentForwardWorld, desiredRailRotation * Vector3.forward);
            targetPose.rotation = forwardCorrection * targetPose.rotation;
        }
    }
}

