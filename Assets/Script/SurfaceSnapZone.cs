using System;
using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Simplified snap system: when parts are released near a breadboard "surface zone",
/// they are pulled onto the top face of a BoxCollider and optionally locked (rigidbody becomes kinematic).
///
/// This intentionally avoids per-pin/per-hole setup. It is meant for "good enough" assembly gating:
/// bring the object close to the breadboard and it will attach.
/// </summary>
[AddComponentMenu("Siemens/Interaction/Surface Snap Zone")]
[DisallowMultipleComponent]
public sealed class SurfaceSnapZone : MonoBehaviour
{
    [Serializable]
    public struct SnapItem
    {
        public enum SnapMode
        {
            [Tooltip("Snap a single point (translation only). Good for boards/sensors where rotation doesn't matter.")]
            SingleAnchor = 0,

            [Tooltip("Snap two points (rigid translation + rotation). Ideal for wires where Pin_1 and Pin_2 must land on the board.")]
            TwoAnchors = 1,
        }

        [Tooltip("Optional label for debugging / inspector clarity.")]
        public string name;

        [Header("Object to snap")]
        [Tooltip("Root transform to move/lock (the grabbed object root).")]
        public Transform partRoot;

        [Tooltip("Optional rigidbody on the root (auto-fetched at runtime if missing).")]
        public Rigidbody partRigidbody;

        [Tooltip("Optional Oculus Grabbable on the root (auto-fetched at runtime if missing).")]
        public Grabbable grabbable;

        [Header("Snap mode")]
        [Tooltip("Single anchor = translation-only. Two anchors = translation + rotation, so both anchors land on the surface.")]
        public SnapMode mode;

        [Tooltip("Anchor on the part used as the snap point. If null, partRoot is used.")]
        public Transform anchor;

        [Tooltip("Second anchor (only used for TwoAnchors). For wires: set anchor=Pin_1 and anchorB=Pin_2.")]
        public Transform anchorB;

        [Header("Snap settings")]
        [Tooltip("Max distance (meters) from the anchor to the breadboard top surface to begin snapping.")]
        public float snapDistance;

        [Tooltip("How fast to pull the part onto the surface.")]
        public float pullSpeed;

        [Tooltip("If enabled, sets rigidbody.isKinematic=true once snapped.")]
        public bool lockWhenSnapped;
    }

    [SerializeField]
    [Tooltip("BoxCollider that represents the breadboard snap area. The object will be pulled to the TOP face.")]
    BoxCollider m_SnapArea;

    [SerializeField]
    [Tooltip("Parts that can snap to this zone.")]
    SnapItem[] m_Items = Array.Empty<SnapItem>();

    [SerializeField]
    [Tooltip("Default snap distance (meters) used when an item snapDistance is <= 0.")]
    float m_DefaultSnapDistance = 0.12f;

    [SerializeField]
    [Tooltip("Default pull speed used when an item pullSpeed is <= 0.")]
    float m_DefaultPullSpeed = 12f;

    bool[] m_Snapped;
    bool[] m_Snapping;
    Vector3[] m_TargetA;
    Vector3[] m_TargetB;

    public int ItemCount => m_Items?.Length ?? 0;

    public int SnappedCount
    {
        get
        {
            EnsureRuntimeState();
            var count = 0;
            for (var i = 0; i < m_Snapped.Length; i++)
                if (m_Snapped[i]) count++;
            return count;
        }
    }

    public bool AllSnapped => ItemCount > 0 && SnappedCount >= ItemCount;

    void Awake()
    {
        EnsureRuntimeState();
        AutoFetchMissingReferences();
    }

    void OnValidate()
    {
        // Keep runtime arrays in sync while editing.
        EnsureRuntimeState();
    }

    void EnsureRuntimeState()
    {
        var count = m_Items?.Length ?? 0;
        if (m_Snapped == null || m_Snapped.Length != count)
            m_Snapped = new bool[count];
        if (m_Snapping == null || m_Snapping.Length != count)
            m_Snapping = new bool[count];
        if (m_TargetA == null || m_TargetA.Length != count)
            m_TargetA = new Vector3[count];
        if (m_TargetB == null || m_TargetB.Length != count)
            m_TargetB = new Vector3[count];
    }

    void AutoFetchMissingReferences()
    {
        if (m_Items == null)
            return;

        for (var i = 0; i < m_Items.Length; i++)
        {
            var item = m_Items[i];
            if (item.partRoot != null)
            {
                if (item.partRigidbody == null)
                    item.partRigidbody = item.partRoot.GetComponent<Rigidbody>();
                if (item.grabbable == null)
                    item.grabbable = item.partRoot.GetComponent<Grabbable>();
            }
            m_Items[i] = item;
        }
    }

    void Update()
    {
        if (m_SnapArea == null || m_Items == null || m_Items.Length == 0)
            return;

        EnsureRuntimeState();

        for (var i = 0; i < m_Items.Length; i++)
        {
            if (m_Snapped[i])
                continue;

            var item = m_Items[i];
            if (item.partRoot == null)
                continue;

            // Don't snap while user is holding it.
            if (item.grabbable != null && item.grabbable.SelectingPointsCount > 0)
                continue;

            var snapDist = item.snapDistance > 0f ? item.snapDistance : Mathf.Max(0.01f, m_DefaultSnapDistance);
            var pullSpeed = item.pullSpeed > 0f ? item.pullSpeed : Mathf.Max(1f, m_DefaultPullSpeed);

            if (item.mode == SnapItem.SnapMode.TwoAnchors)
            {
                var a = item.anchor != null ? item.anchor : item.partRoot;
                var b = item.anchorB;
                if (b == null)
                    continue;

                if (!m_Snapping[i])
                {
                    var ta = GetTopSurfacePointClamped(a.position);
                    var tb = GetTopSurfacePointClamped(b.position);
                    if (Vector3.Distance(a.position, ta) > snapDist) continue;
                    if (Vector3.Distance(b.position, tb) > snapDist) continue;

                    // Capture stable targets once, so the snap converges.
                    m_TargetA[i] = ta;
                    m_TargetB[i] = tb;
                    m_Snapping[i] = true;
                }

                var targetA = m_TargetA[i];
                var targetB = m_TargetB[i];

                // Solve a rigid transform so both anchors land on the captured targets.
                // Use minimal-rotation from current pose to avoid unexpected twisting.
                var localA = item.partRoot.InverseTransformPoint(a.position);
                var localB = item.partRoot.InverseTransformPoint(b.position);
                var localVec = localB - localA;
                if (localVec.sqrMagnitude < 1e-8f)
                    continue;

                var currentWorldVec = item.partRoot.rotation * localVec;
                var targetWorldVec = (targetB - targetA);
                if (targetWorldVec.sqrMagnitude < 1e-8f)
                    continue;

                var deltaRot = Quaternion.FromToRotation(currentWorldVec, targetWorldVec.normalized);
                var targetRootRot = deltaRot * item.partRoot.rotation;
                var targetRootPos = targetA - (targetRootRot * localA);

                item.partRoot.rotation = Quaternion.Slerp(item.partRoot.rotation, targetRootRot, Time.deltaTime * pullSpeed);
                item.partRoot.position = Vector3.Lerp(item.partRoot.position, targetRootPos, Time.deltaTime * pullSpeed);

                // Final lock
                if (Vector3.Distance(a.position, targetA) < 0.003f &&
                    Vector3.Distance(b.position, targetB) < 0.003f)
                {
                    item.partRoot.rotation = targetRootRot;
                    item.partRoot.position = targetRootPos;
                    m_Snapped[i] = true;
                    m_Snapping[i] = false;

                    if (item.lockWhenSnapped && item.partRigidbody != null)
                        item.partRigidbody.isKinematic = true;
                }
            }
            else
            {
                var anchor = item.anchor != null ? item.anchor : item.partRoot;

                if (!m_Snapping[i])
                {
                    var targetAnchorPos = GetTopSurfacePointClamped(anchor.position);
                    if (Vector3.Distance(anchor.position, targetAnchorPos) > snapDist)
                        continue;

                    // Capture stable target once, so it converges cleanly.
                    m_TargetA[i] = targetAnchorPos;
                    m_Snapping[i] = true;
                }

                var targetAnchor = m_TargetA[i];

                // Translation-only snap: keep rotation as-is, move root so anchor hits the target position.
                var delta = targetAnchor - anchor.position;
                var targetRootPos = item.partRoot.position + delta;

                item.partRoot.position = Vector3.Lerp(item.partRoot.position, targetRootPos, Time.deltaTime * pullSpeed);

                // Final lock
                if (Vector3.Distance(anchor.position, targetAnchor) < 0.003f)
                {
                    item.partRoot.position = targetRootPos;
                    m_Snapped[i] = true;
                    m_Snapping[i] = false;

                    if (item.lockWhenSnapped && item.partRigidbody != null)
                        item.partRigidbody.isKinematic = true;
                }
            }
        }
    }

    /// <summary>
    /// Returns the closest point on the TOP face of the box collider, clamped in local X/Z.
    /// </summary>
    Vector3 GetTopSurfacePointClamped(Vector3 worldPoint)
    {
        // Convert point into collider local space, relative to the collider center.
        var local = m_SnapArea.transform.InverseTransformPoint(worldPoint) - m_SnapArea.center;
        var half = m_SnapArea.size * 0.5f;

        local.x = Mathf.Clamp(local.x, -half.x, half.x);
        local.z = Mathf.Clamp(local.z, -half.z, half.z);
        local.y = half.y; // top face

        var snappedLocal = local + m_SnapArea.center;
        return m_SnapArea.transform.TransformPoint(snappedLocal);
    }

    public bool IsSnapped(int index)
    {
        EnsureRuntimeState();
        if (index < 0 || index >= m_Snapped.Length)
            return false;
        return m_Snapped[index];
    }

    public void UnsnapAll(bool makeNonKinematic = true)
    {
        EnsureRuntimeState();
        for (var i = 0; i < m_Snapped.Length; i++)
            m_Snapped[i] = false;
        for (var i = 0; i < m_Snapping.Length; i++)
            m_Snapping[i] = false;

        if (!makeNonKinematic || m_Items == null)
            return;

        for (var i = 0; i < m_Items.Length; i++)
        {
            var rb = m_Items[i].partRigidbody;
            if (rb != null) rb.isKinematic = false;
        }
    }
}

