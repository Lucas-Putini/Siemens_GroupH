using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WANConnectionManager: Builds and renders connections between WANNode markers on the globe.
/// Implements:
/// - Collections: List<GameObject> for activeLines; Dictionary<(string,string),LineRenderer> for LineMap (Project Req: Collections - Lists & Dictionaries)
/// - Data Structures: tuple keys in Dictionary for bidirectional lookup (Project Req: Tuples & Structs)
/// - Geometry: Vector3.Angle for angular threshold; Vector3.Slerp for curved arcs (Project Req: Algorithms - Geometry)
/// - Unity basics: Instantiate, Transform, Renderer, LineRenderer configs (Project Req: Classes & Unity API)
/// - Loops and conditionals for neighbor selection and connection logic
/// </summary>
public class WANConnectionManager : MonoBehaviour
{
    [Header("Connection Rendering")]
    public GameObject wanLinePrefab;       // Prefab containing a LineRenderer component
    public Transform lineContainer;        // Parent for all instantiated line objects
    public float lineWidth = 0.3f;         // Width of each line segment
    public Material lineMaterial;          // Default material for lines (with color)

    // Tracks instantiated line GameObjects (List<T>)
    private List<GameObject> activeLines = new List<GameObject>();

    // Global map of node-pair keys to LineRenderer for quick lookup (Dictionary with tuple keys)
    public static Dictionary<(string, string), LineRenderer> LineMap = new Dictionary<(string, string), LineRenderer>();

    /// <summary>
    /// Connects each node to its nearest neighbor in each direction (front, back, left, right) within an angular threshold.
    /// Clears existing lines and neighbor lists, then:
    /// - Uses Vector3.Angle to filter nodes by angular proximity.
    /// - Uses local directional logic (Quaternion.FromToRotation) to categorize neighbors.
    /// - Calls AddConnection for each unique pair.
    /// </summary>
    public void ConnectAndDraw(List<WANNode> nodes, float nodeRadius)
    {
        // 1. Remove previous visuals and data
        ClearLines();

        // 2. Reset neighbor lists on each node (List<WANNode>.Clear)
        foreach (WANNode n in nodes)
            n.neighbors.Clear();

        float angleThreshold = 60f; // Maximum angle difference for potential connections

        // For each node, find nearest neighbor in each principal direction
        foreach (WANNode from in nodes)
        {
            // Directional map for nearest neighbor in each quadrant
            Dictionary<string, WANNode> directions = new Dictionary<string, WANNode>();

            foreach (WANNode to in nodes)
            {
                if (to == from || from.neighbors.Contains(to))
                    continue; // Skip self and existing links

                float angle = Vector3.Angle(from.position, to.position);
                if (angle > angleThreshold) continue; // Too far apart on globe

                // Transform vector to local forward space of 'from'
                Vector3 delta = to.position - from.position;
                Vector3 localDir = Quaternion.FromToRotation(from.position.normalized, Vector3.forward) * delta;

                // Determine relative direction and pick closest by distance
                if (localDir.x > 0 && (!directions.ContainsKey("Right") || delta.magnitude < (directions["Right"].position - from.position).magnitude))
                    directions["Right"] = to;
                if (localDir.x < 0 && (!directions.ContainsKey("Left") || delta.magnitude < (directions["Left"].position - from.position).magnitude))
                    directions["Left"] = to;
                if (localDir.z > 0 && (!directions.ContainsKey("Front") || delta.magnitude < (directions["Front"].position - from.position).magnitude))
                    directions["Front"] = to;
                if (localDir.z < 0 && (!directions.ContainsKey("Back") || delta.magnitude < (directions["Back"].position - from.position).magnitude))
                    directions["Back"] = to;
            }

            // Create connections for each selected neighbor
            foreach (WANNode neighbor in directions.Values)
            {
                if (!from.neighbors.Contains(neighbor))
                {
                    AddConnection(from, neighbor, nodeRadius); // Draws curved line
                    from.neighbors.Add(neighbor);              // Update neighbor lists
                    neighbor.neighbors.Add(from);
                }
            }
        }
    }

    /// <summary>
    /// Instantiates a curved LineRenderer between two nodes:
    /// - Calculates start/end points offset from earth center by nodeRadius + marker radius.
    /// - Uses Slerp to interpolate arc positions across segmentCount.
    /// - Configures widths, colors, and world-space coordinates.
    /// - Stores the LineRenderer in activeLines and LineMap for lookup.
    /// </summary>
    private void AddConnection(WANNode from, WANNode to, float nodeRadius)
    {
        // Instantiate a new line object under the container
        GameObject lineObj = Instantiate(wanLinePrefab, Vector3.zero, Quaternion.identity, lineContainer);
        LineRenderer lr = lineObj.GetComponent<LineRenderer>();

        // Apply material and basic properties
        if (lineMaterial != null)
            lr.material = lineMaterial;
        lr.useWorldSpace = true;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.startColor = lr.endColor = (lineMaterial != null ? lineMaterial.color : Color.cyan);

        int segmentCount = 60;
        lr.positionCount = segmentCount;

        // Compute globe center in world coords
        Vector3 center = from.marker.transform.parent.TransformPoint(Vector3.zero);

        // Direction vectors from center to each marker
        Vector3 dirA = (from.marker.transform.position - center).normalized;
        Vector3 dirB = (to.marker.transform.position - center).normalized;

        // Compute marker radii for surface offset
        float mRadA = GetMarkerRadius(from.marker);
        float mRadB = GetMarkerRadius(to.marker);
        Vector3 startPoint = center + dirA * (nodeRadius + mRadA * 0.5f);
        Vector3 endPoint = center + dirB * (nodeRadius + mRadB * 0.5f);

        // Populate curved arc via spherical interpolation
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector3 arcDir = Vector3.Slerp(dirA, dirB, t).normalized;
            float arcRad = Mathf.Lerp((nodeRadius + mRadA * 0.5f), (nodeRadius + mRadB * 0.5f), t);
            lr.SetPosition(i, center + arcDir * arcRad);
        }

        activeLines.Add(lineObj); // Track for clearing

        // Store bidirectional lookup keys in dictionary
        LineMap[(from.name, to.name)] = lr;
        LineMap[(to.name, from.name)] = lr;
    }

    /// <summary>
    /// Computes a GameObject's "radius" via its Renderer.bounds.extents or localScale fallback.
    /// Demonstrates property access and component usage (Project Req: Classes & Structs)
    /// </summary>
    private float GetMarkerRadius(GameObject marker)
    {
        if (marker.TryGetComponent<Renderer>(out var rend))
            return Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.y, rend.bounds.extents.z);
        // Fallback to half of localScale.x
        return marker.transform.localScale.x * 0.5f;
    }

    /// <summary>
    /// Clears all line GameObjects and empties tracking structures (List.Clear & Dictionary.Clear).
    /// </summary>
    public void ClearLines()
    {
        foreach (GameObject go in activeLines)
            Destroy(go);
        activeLines.Clear();
        LineMap.Clear();
    }
}
