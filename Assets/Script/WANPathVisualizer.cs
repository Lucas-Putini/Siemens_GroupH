using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WANPathVisualizer: Highlights and animates signal travel across WAN nodes on a rotating globe.
/// Implements:
/// - Lists for tracking highlighted nodes/lines (Project Requirement: Collections - Lists & Dictionaries)
/// - Coroutines for asynchronous animation (Project Requirement: Algorithms - Coroutines & IEnumerator)
/// References external data/methods from:
/// - WANConnectionManager (LineMap dictionary)
/// - WANNodeManager (node GameObjects under nodesParent)
/// - EarthSpinner (SpinToFace method) * I currently disabled this feature. *
/// </summary>
public class WANPathVisualizer : MonoBehaviour
{
    [Header("References")]
    public Material lineMaterial;    // Default material for resetting LineRenderers (WANConnectionManager sets initial lines)
    public Material nodeMaterial;    // Default material for resetting node renderers (WANNodeManager instantiates nodes)
    public Transform linesParent;    // Parent transform for all connection lines (set by WANDesignManager)
    public Transform nodesParent;    // Parent transform containing node GameObjects (set by WANDesignManager)
    public EarthSpinner earthSpinner; // Reference to EarthSpinner script for globe rotation (EarthSpinner.cs)

    [Header("Signal Animation")]
    public GameObject signalSpherePrefab; // Prefab for the moving signal indicator
    public float signalSpeed = 3f;        // Speed scalar for signal movement

    // Internal state: tracking highlighted renderers
    private List<Renderer> highlightedNodes = new();       // Tracks node GameObject renderers (uses System.Collections.Generic.List<T>)
    private List<LineRenderer> highlightedLines = new();   // Tracks connection LineRenderers
    private GameObject signalSphereInstance;               // Runtime instance of the signal prefab

    /// <summary>
    /// Highlights a given path of WANNodes:
    /// - Colors start node green, end node red, middle nodes yellow
    /// - Colors connecting lines yellow or green :) not sure yet
    /// Uses FindNodeRenderer (search under nodesParent or by name) and WANConnectionManager.LineMap lookup
    /// </summary>
    public void HighlightPath(List<WANNode> path)
    {
        if (path == null || path.Count < 2) return;  // Validate input
        ClearHighlight();                              // Reset any existing highlights

        // Iterate nodes: apply color based on position in path
        for (int i = 0; i < path.Count; i++)
        {
            Renderer nodeRend = FindNodeRenderer(path[i].name); // Node lookup from WANNodeManager instantiation
            if (nodeRend == null) continue;

            if (i == 0)
                SetNodeColor(nodeRend, Color.green);    // Start
            else if (i == path.Count - 1)
                SetNodeColor(nodeRend, Color.red);      // End
            else
                SetNodeColor(nodeRend, Color.yellow);   // Intermediate

            highlightedNodes.Add(nodeRend);
        }

        // Iterate connections: apply yellow
        for (int i = 0; i < path.Count - 1; i++)
        {
            LineRenderer lr = FindConnectionLine(path[i], path[i + 1]); // Dictionary lookup in WANConnectionManager.LineMap
            if (lr == null) continue;

            SetLineColor(lr, Color.yellow);
            highlightedLines.Add(lr);
        }
    }

    /// <summary>
    /// Prepares and starts the signal animation along the given path:
    /// - Clears existing animation/data
    /// - Computes path midpoint to rotate globe
    /// - Starts coroutine chain SpinAndAnimate
    /// </summary>
    public void AnimateSignalAlongPath(List<WANNode> path)
    {
        StopAllCoroutines();                           // Stop any ongoing animations
        if (signalSphereInstance != null)
            Destroy(signalSphereInstance);             // Remove existing sphere

        if (path == null || path.Count < 2) return;    // Validate input

        // Compute average world position of nodes
        Vector3 midpoint = Vector3.zero;
        foreach (WANNode node in path)
        {
            Renderer rend = FindNodeRenderer(node.name);
            midpoint += (rend != null) ? rend.transform.position : Vector3.zero;
        }
        midpoint /= path.Count;

        // Start coroutine to spin globe then animate signal
        StartCoroutine(SpinAndAnimate(path, midpoint));
    }

    /// <summary>
    /// Coroutine: first rotates globe to face midpoint, then animates signal
    /// Calls EarthSpinner.SpinToFace(midpoint) from EarthSpinner.cs
    /// </summary>
    private IEnumerator SpinAndAnimate(List<WANNode> path, Vector3 midpoint)
    {
        if (earthSpinner != null)
            yield return StartCoroutine(earthSpinner.SpinToFace(midpoint)); // External call

        yield return StartCoroutine(AnimateSignalCoroutine(path));         // Continue to animation
    }

    /// <summary>
    /// Core animation coroutine:
    /// - Instantiates signalSpherePrefab at start node
    /// - Moves along each segment of the LineRenderer's curve
    /// - Calculates segment distances and total distance (Array of floats, loops)
    /// - Lerps position over time based on signalSpeed
    /// - Handles direction based on proximity test
    /// - Colors lines green as the sphere passes (List management)
    /// - Colors nodes after visit (yellow internal, red final)
    /// </summary>
    private IEnumerator AnimateSignalCoroutine(List<WANNode> path)
    {
        ClearHighlight();  // Reset visuals

        Renderer startRend = FindNodeRenderer(path[0].name);
        if (startRend == null || signalSpherePrefab == null)
        {
            Debug.LogWarning("Missing start renderer or prefab");
            yield break;     // Abort if critical
        }

        SetNodeColor(startRend, Color.green);
        highlightedNodes.Add(startRend);
        signalSphereInstance = Instantiate(signalSpherePrefab, startRend.transform.position, Quaternion.identity);

        // Iterate path segments
        for (int i = 0; i < path.Count - 1; i++)
        {
            Renderer fromRend = FindNodeRenderer(path[i].name);
            Renderer toRend = FindNodeRenderer(path[i + 1].name);
            if (fromRend == null || toRend == null) break;

            LineRenderer lr = FindConnectionLine(path[i], path[i + 1]);
            if (lr != null)
            {
                int segCount = lr.positionCount;                         // Number of curve segments
                float totalDist = 0f;
                float[] segDists = new float[segCount];                  // Array to store each segment length

                // Compute distances
                for (int s = 1; s < segCount; s++)
                {
                    float d = Vector3.Distance(lr.GetPosition(s - 1), lr.GetPosition(s));
                    segDists[s] = d;
                    totalDist += d;
                }

                float travelTime = totalDist / signalSpeed;

                // Determine forward/backward based on distance to fromNode
                Vector3 startPt = lr.GetPosition(0);
                Vector3 endPt = lr.GetPosition(segCount - 1);
                bool reverse = Vector3.Distance(startPt, fromRend.transform.position) >
                               Vector3.Distance(endPt, fromRend.transform.position);

                // Animate each segment
                if (!reverse)
                {
                    signalSphereInstance.transform.position = startPt;
                    for (int seg = 1; seg < segCount; seg++)
                    {
                        float segTime = travelTime * (segDists[seg] / totalDist);
                        float t = 0f;
                        Vector3 a = lr.GetPosition(seg - 1);
                        Vector3 b = lr.GetPosition(seg);
                        while (t < 1f)
                        {
                            t += Time.deltaTime / segTime;
                            signalSphereInstance.transform.position = Vector3.Lerp(a, b, Mathf.Clamp01(t));
                            yield return null;
                        }
                    }
                }
                else
                {
                    signalSphereInstance.transform.position = endPt;
                    for (int seg = segCount - 2; seg >= 0; seg--)
                    {
                        float segTime = travelTime * (segDists[seg + 1] / totalDist);
                        float t = 0f;
                        Vector3 a = lr.GetPosition(seg + 1);
                        Vector3 b = lr.GetPosition(seg);
                        while (t < 1f)
                        {
                            t += Time.deltaTime / segTime;
                            signalSphereInstance.transform.position = Vector3.Lerp(a, b, Mathf.Clamp01(t));
                            yield return null;
                        }
                    }
                }

                // Mark line as passed
                SetLineColor(lr, Color.green);
                highlightedLines.Add(lr);
            }
            else
            {
                // Fallback straight move if no curve data
                Vector3 a = fromRend.transform.position;
                Vector3 b = toRend.transform.position;
                float t = 0f;
                float tTime = Vector3.Distance(a, b) / signalSpeed;
                while (t < 1f)
                {
                    t += Time.deltaTime / tTime;
                    signalSphereInstance.transform.position = Vector3.Lerp(a, b, t);
                    yield return null;
                }
            }

            // Color visited node: intermediate = yellow, final = red
            if (i < path.Count - 2)
                SetNodeColor(toRend, Color.yellow);
            else
                SetNodeColor(toRend, Color.red);

            highlightedNodes.Add(toRend);
        }
    }

    /// <summary>
    /// Clears all visual highlights and destroys the signal sphere.
    /// Implements reset via default materials (List clear operations).
    /// </summary>
    public void ClearHighlight()
    {
        foreach (Renderer rend in highlightedNodes)
            if (rend != null && nodeMaterial != null)
                rend.material = nodeMaterial;
        highlightedNodes.Clear();   // List.Clear()

        foreach (LineRenderer lr in highlightedLines)
            if (lr != null && lineMaterial != null)
                lr.material = lineMaterial;
        highlightedLines.Clear();   // List.Clear()

        if (signalSphereInstance != null)
            Destroy(signalSphereInstance);
    }

    /// <summary>
    /// Finds a node's Renderer by name under nodesParent or globally.
    /// Uses Transform.Find (hierarchical lookup) or GameObject.Find.
    /// </summary>
    private Renderer FindNodeRenderer(string nodeName)
    {
        if (nodesParent != null)
        {
            Transform node = nodesParent.Find(nodeName);
            if (node != null)
                return node.GetComponent<Renderer>();
        }
        GameObject go = GameObject.Find(nodeName);  // Fallback global lookup
        return go ? go.GetComponent<Renderer>() : null;
    }

    /// <summary>
    /// Sets a node's material color and emission.
    /// Creates a new Material instance from nodeMaterial (Project Req: Classes & Structs - Material instantiation)
    /// </summary>
    private void SetNodeColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        Material mat = new Material(nodeMaterial);  // Instantiate new Material object
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 2f);
        mat.SetColor("_Color", color);
        renderer.material = mat;
    }

    /// <summary>
    /// Looks up the LineRenderer between two nodes via WANConnectionManager.LineMap dictionary.
    /// (Project Req: Collections - Dictionaries)
    /// </summary>
    private LineRenderer FindConnectionLine(WANNode from, WANNode to)
    {
        if (WANConnectionManager.LineMap.TryGetValue((from.name, to.name), out LineRenderer lr))
            return lr;
        return null;
    }

    /// <summary>
    /// Sets a line's material color and renderer colors.
    /// Creates a new Material instance from lineMaterial (Project Req: Classes & Structs)
    /// </summary>
    private void SetLineColor(LineRenderer lr, Color color)
    {
        if (lr == null) return;
        Material mat = new Material(lineMaterial);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 2f);
        mat.SetColor("_Color", color);
        lr.material = mat;
        lr.startColor = color;
        lr.endColor = color;
    }

    /// <summary>
    /// Helper: checks approximate equality of two Vector3 positions (unused).
    /// </summary>
    private bool ApproximatelySame(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b) < 0.05f;
    }
}
