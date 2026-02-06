using System;
using UnityEngine;

/// <summary>
/// Defines a railway centerline using a polyline (waypoints) and provides queries like
/// closest point projection and tangent direction.
/// </summary>
[DisallowMultipleComponent]
public sealed class RailwayPath : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Ordered waypoints that define the centerline of the railway. Must contain at least 2 points.")]
    Transform[] m_Waypoints = Array.Empty<Transform>();

    [SerializeField]
    [Tooltip("Connect last waypoint back to first.")]
    bool m_Loop;

    [Header("Gizmos")]
    [SerializeField]
    bool m_DrawGizmos = true;

    [SerializeField]
    Color m_GizmoColor = new Color(0.2f, 0.9f, 0.9f, 1f);

    [SerializeField]
    float m_GizmoSphereRadius = 0.04f;

    public Transform[] waypoints
    {
        get => m_Waypoints;
        set => m_Waypoints = value ?? Array.Empty<Transform>();
    }

    public bool loop
    {
        get => m_Loop;
        set => m_Loop = value;
    }

    public int WaypointCount => m_Waypoints?.Length ?? 0;

    public bool TryGetClosestPoint(Vector3 worldPoint, out Vector3 closestPoint, out Vector3 tangent)
    {
        closestPoint = default;
        tangent = Vector3.forward;

        if (m_Waypoints == null || m_Waypoints.Length < 2)
            return false;

        var bestSqrDist = float.PositiveInfinity;
        var bestPoint = Vector3.zero;
        var bestTangent = Vector3.forward;

        var count = m_Waypoints.Length;
        var lastSegmentStart = m_Loop ? count : count - 1;

        for (var i = 0; i < lastSegmentStart; i++)
        {
            var aTf = m_Waypoints[i];
            var bTf = m_Waypoints[(i + 1) % count];
            if (aTf == null || bTf == null)
                continue;

            var a = aTf.position;
            var b = bTf.position;
            var ab = b - a;
            var abSqr = ab.sqrMagnitude;
            if (abSqr < 1e-8f)
                continue;

            var t = Vector3.Dot(worldPoint - a, ab) / abSqr;
            t = Mathf.Clamp01(t);
            var q = a + ab * t;

            var sqrDist = (worldPoint - q).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                bestPoint = q;
                bestTangent = ab.normalized;
            }
        }

        if (float.IsPositiveInfinity(bestSqrDist))
            return false;

        closestPoint = bestPoint;
        tangent = bestTangent;
        return true;
    }

    void OnDrawGizmos()
    {
        if (!m_DrawGizmos)
            return;

        if (m_Waypoints == null || m_Waypoints.Length < 2)
            return;

        Gizmos.color = m_GizmoColor;

        var count = m_Waypoints.Length;
        var lastSegmentStart = m_Loop ? count : count - 1;

        for (var i = 0; i < lastSegmentStart; i++)
        {
            var aTf = m_Waypoints[i];
            var bTf = m_Waypoints[(i + 1) % count];
            if (aTf == null || bTf == null)
                continue;

            Gizmos.DrawLine(aTf.position, bTf.position);
        }

        foreach (var wp in m_Waypoints)
        {
            if (wp == null)
                continue;

            Gizmos.DrawSphere(wp.position, m_GizmoSphereRadius);
        }
    }
}

