using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WANNodeManager: Generates and manages WANNode instances on a spherical Earth model.
/// Topics from Project Requirements:
/// - Collections: List<WANNode> for generated nodes; List<string> for network devices (Project Req: Lists & Collections)
/// - Math & Algorithms: Fibonacci Sphere (golden ratio) algorithm for even node distribution on sphere surface (Project Req: Algorithms)
/// - Classes & Structs: WANNode instantiation and GameObject marker creation
/// - Geometry: Vector3 calculations for sphere coordinates
/// </summary>
public class WANNodeManager : MonoBehaviour
{
    [Header("Scene References")]
    public GameObject earth;               // Reference to Earth GameObject with MeshRenderer for radius computation
    public GameObject nodeMarkerPrefab;    // Prefab for visual node markers
    public Transform nodeContainer;        // Parent container for all node markers (keeps scene hierarchy organized)

    [Header("Node Distribution Settings")]
    public int numberOfNodes = 40;         // Total number of nodes to generate (List count)
    [Tooltip("Multiplier to push nodes out slightly from the surface. 1.0 = on surface, >1 = above surface.")]
    public float radiusMultiplier = 1.02f; // Scales EarthRadius to place nodes just above the surface
    public float nodeMarkerSize = 0.5f;    // Scale factor for instantiated marker GameObjects

    [Header("Node Appearance")]
    public Material nodeMaterial;          // Material assigned to marker renderers
    public List<string> countryNames;      // Optional custom names for first N nodes (uses List<string>)

    /// <summary>
    /// Public accessor for generated node data
    /// Uses a List<WANNode> to store instances (Project Req: Collections)
    /// </summary>
    public List<WANNode> GeneratedNodes { get; private set; } = new List<WANNode>();

    /// <summary>
    /// Computes the visible Earth radius via MeshRenderer.bounds.extents
    /// Demonstrates property syntax and Renderer access (Project Req: Classes & Properties)
    /// </summary>
    private float EarthRadius
    {
        get
        {
            var renderer = earth.GetComponent<Renderer>(); // Get MeshRenderer component
            if (renderer == null)
            {
                Debug.LogWarning("[WANNodeManager] Earth has no Renderer! Using fallback radius 0.5.");
                return 0.5f;
            }
            // Determine sphere radius by max extent of bounds
            float maxExtent = Mathf.Max(renderer.bounds.extents.x,
                                        renderer.bounds.extents.y,
                                        renderer.bounds.extents.z);
            return maxExtent;
        }
    }

    /// <summary>
    /// Calculates node placement radius: EarthRadius * multiplier (Method, Project Req: Classes)
    /// </summary>
    public float GetNodeRadius() => EarthRadius * radiusMultiplier;

    /// <summary>
    /// Generates node positions on sphere using Fibonacci sphere algorithm:
    /// 1. Clear existing markers and data (method call)
    /// 2. Compute radius and golden ratio constant
    /// 3. Loop numberOfNodes times:
    ///    - Calculate spherical coordinates (inclination & azimuth)
    ///    - Convert to Cartesian Vector3 direction * radius
    ///    - Instantiate WANNode object and marker GameObject
    /// Uses:
    /// - List.Clear and List.Add (Project Req: Collections)
    /// - Instantiate (UnityEngine.Object creation)
    /// - Vector3 operations (Math)
    /// </summary>
    public void GenerateNodes()
    {
        ClearExistingNodes();               // Remove old markers (method defined below)
        GeneratedNodes.Clear();             // Clear data list

        float radius = GetNodeRadius();     // Compute final radius
        float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;               // Constant for Fibonacci sphere
        float angleIncrement = Mathf.PI * 2 * goldenRatio;       // Angular step

        Debug.Log($"[WANNodeManager] Earth visible radius: {EarthRadius}");
        Debug.Log($"[WANNodeManager] Node placement radius: {radius}");

        // Loop to create nodes
        for (int i = 0; i < numberOfNodes; i++)
        {
            float t = i / (float)numberOfNodes;                      // Fraction [0,1)
            float inclination = Mathf.Acos(1 - 2 * t);               // Polar angle
            float azimuth = angleIncrement * i;                      // Azimuthal angle

            // Direction vector on unit sphere
            Vector3 direction = new Vector3(
                Mathf.Sin(inclination) * Mathf.Cos(azimuth),
                Mathf.Sin(inclination) * Mathf.Sin(azimuth),
                Mathf.Cos(inclination)
            ).normalized;

            Vector3 localPos = direction * radius;                  // Position relative to center
            string nodeName = (i < countryNames.Count)
                ? countryNames[i]                                  // Use provided country name
                : $"Node{i}";                                    // Fallback name

            // Instantiate node data object
            WANNode node = new WANNode(nodeName, localPos);        // Class constructor (Project Req: Classes & Structs)
            GeneratedNodes.Add(node);                              // Store in list

            // Create visual marker
            GameObject marker = Instantiate(nodeMarkerPrefab, nodeContainer);
            marker.transform.localPosition = localPos;              // Position marker
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one * nodeMarkerSize;
            marker.name = nodeName;                                // Name GameObject

            // Assign material if available
            if (nodeMaterial != null && marker.TryGetComponent<Renderer>(out var rend))
            {
                rend.material = nodeMaterial;
            }

            node.marker = marker;   // Link back to WANNode instance
        }

        Debug.Log($"[WANNodeManager] Generated {GeneratedNodes.Count} nodes.");
    }

    /// <summary>
    /// Clears all existing node marker GameObjects and data list.
    /// Demonstrates Transform iteration and Destroy calls.
    /// </summary>
    public void ClearExistingNodes()
    {
        foreach (Transform child in nodeContainer)
        {
            Destroy(child.gameObject);   // Remove from scene
        }
        GeneratedNodes.Clear();          // Clear data list
    }
}
