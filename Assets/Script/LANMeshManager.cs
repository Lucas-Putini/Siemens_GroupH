using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LANMeshManager handles the LAN mesh topology scenario for the "LAN Design" option.
/// Implements:
/// - Arrays: Vector3[] corners (Project Req: Arrays)
/// - Collections: List<GameObject> for PCs and lines (Project Req: Lists)
/// - Dictionaries: adjacency list for mesh (Project Req: Dictionaries)
/// - Stack: DFS traversal (Project Req: Stacks & Graph Algorithms)
/// - Coroutines: signal animation and traversal highlights (Project Req: Coroutines)
/// - Unity API: LineRenderer, Instantiate/Destroy, UI callbacks (Project Req: Classes & Unity)
/// </summary>
public class LANMeshManager : MonoBehaviour
{
    [Header("CANVAS & UI")]
    public Canvas lanCanvas;                // The LAN scenario canvas (CanvasCase2)
    public RectTransform panelRect;         // Panel whose corners define spawn area

    [Header("Prefabs & Materials")]
    public GameObject signalSpherePrefab;   // Sphere prefab for corner animation
    public GameObject pcPrefab;             // PC prefab for mesh nodes
    public Material pcMaterial;             // Material for PCs
    public Material frameMaterial;          // Material for frame & mesh lines

    [Header("Controls")]
    public Button startButton;              // Begin scenario
    public Button closeButton;              // End scenario

    [Header("Line Settings")]
    public float frameWidth = 0.02f;        // Width for frame and mesh lines

    // Internal state
    private Vector3[] corners;                              // Array: panel corners
    private GameObject frameObject;                         // Frame outline
    private List<GameObject> meshPCs = new List<GameObject>();   // Spawned PCs
    private List<GameObject> meshLines = new List<GameObject>(); // Spawned mesh links

    void Awake()
    {
        // Hide LAN canvas initially
        if (lanCanvas != null)
            lanCanvas.gameObject.SetActive(false);

        // Bind UI callbacks
        if (startButton != null)
            startButton.onClick.AddListener(OnStartScenario);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseScenario);
    }

    /// <summary>
    /// Fetches world-space positions of the panel's four corners.
    /// </summary>
    private void InitializeCorners()
    {
        corners = new Vector3[4];
        Vector3[] worldCorners = new Vector3[4];
        panelRect.GetWorldCorners(worldCorners);
        for (int i = 0; i < 4; i++)
            corners[i] = worldCorners[i];
    }

    /// <summary>
    /// Draws a rectangular frame around the panel using a LineRenderer.
    /// </summary>
    private void DrawRectangleFrame()
    {
        if (corners == null || corners.Length < 4)
            InitializeCorners();

        if (frameObject != null)
            Destroy(frameObject);

        frameObject = new GameObject("LAN_Frame");
        frameObject.transform.SetParent(panelRect, worldPositionStays: true);
        LineRenderer lr = frameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 5;
        lr.material = frameMaterial;
        lr.startWidth = lr.endWidth = frameWidth;

        lr.SetPosition(0, corners[0]);
        lr.SetPosition(1, corners[1]);
        lr.SetPosition(2, corners[2]);
        lr.SetPosition(3, corners[3]);
        lr.SetPosition(4, corners[0]);
    }

    /// <summary>
    /// Animates a signal sphere around the panel corners.
    /// </summary>
    private IEnumerator AnimateAroundCorners()
    {
        if (signalSpherePrefab == null || corners == null)
            yield break;

        GameObject sphere = Instantiate(signalSpherePrefab, corners[0], Quaternion.identity);
        float duration = 1.0f;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 startPos = corners[i];
            Vector3 endPos = corners[(i + 1) % corners.Length];
            float elapsed = 0f;
            while (elapsed < duration)
            {
                sphere.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            sphere.transform.position = endPos;
        }
        Destroy(sphere);
    }

    /// <summary>
    /// Starts the LAN scenario: draws frame, animates signal, spawns PCs, builds mesh, and runs DFS.
    /// </summary>
    private void OnStartScenario()
    {
        if (lanCanvas != null)
            lanCanvas.gameObject.SetActive(true);

        // FRAME + SIGNAL
        InitializeCorners();
        DrawRectangleFrame();
        StartCoroutine(AnimateAroundCorners());

        // SPAWN PCs in 4x2 grid within panel
        meshPCs.ForEach(Destroy);
        meshPCs.Clear();
        int cols = 4, rows = 2;
        for (int i = 0; i < cols * rows; i++)
        {
            float u = (i % cols) / (float)(cols - 1);
            float v = (i / cols) / (float)(rows - 1);
            Vector3 bl = corners[0];
            Vector3 br = corners[3];
            Vector3 tl = corners[1];
            Vector3 tr = corners[2];
            Vector3 bottomPos = Vector3.Lerp(bl, br, u);
            Vector3 topPos = Vector3.Lerp(tl, tr, u);
            Vector3 pos = Vector3.Lerp(bottomPos, topPos, v);

            // Instantiate PC prefab as child of panelRect
            GameObject pc = Instantiate(pcPrefab, pos, Quaternion.identity, panelRect);
            // Combine panel-based rotation with prefab's original rotation
            // Compute rotation aligning prefab’s forward to panel normal
            Quaternion panelRot = Quaternion.LookRotation(panelRect.forward, panelRect.up);
            // Apply prefab’s original rotation, then rotate 90° around local X to face 'up' on the panel
            Quaternion adjustRot = Quaternion.Euler(90f, 0f, 0f);
            // Combine panel alignment, prefab base rotation, an X-flip plus a 180° yaw to face screen up
            Quaternion extraFlip = Quaternion.Euler(90f, 180f, 0f);  // 90° around X then 180° around Y
            pc.transform.rotation = panelRot * pcPrefab.transform.rotation * extraFlip;
            if (pc.TryGetComponent<Renderer>(out var rend))
                rend.material = pcMaterial;
            meshPCs.Add(pc);
        }

        // BUILD FULL MESH ADJACENCY
        var adjacency = new Dictionary<GameObject, List<GameObject>>();
        foreach (var pc in meshPCs)
            adjacency[pc] = new List<GameObject>();
        meshLines.ForEach(Destroy);
        meshLines.Clear();
        for (int i = 0; i < meshPCs.Count; i++)
        {
            for (int j = i + 1; j < meshPCs.Count; j++)
            {
                var a = meshPCs[i];
                var b = meshPCs[j];
                GameObject link = new GameObject("MeshLink");
                link.transform.SetParent(panelRect, worldPositionStays: true);
                var lr = link.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.material = frameMaterial;
                lr.startWidth = lr.endWidth = frameWidth * 0.5f;
                lr.SetPosition(0, a.transform.position);
                lr.SetPosition(1, b.transform.position);
                meshLines.Add(link);
                adjacency[a].Add(b);
                adjacency[b].Add(a);
            }
        }

        // DFS TRAVERSAL HIGHLIGHT
        StartCoroutine(RunDFS(meshPCs[0], adjacency));
    }

    /// <summary>
    /// Depth-first traversal using a stack, highlighting each PC.
    /// </summary>
    private IEnumerator RunDFS(GameObject start, Dictionary<GameObject, List<GameObject>> adj)
    {
        var stack = new Stack<GameObject>();
        var visited = new HashSet<GameObject>();
        stack.Push(start);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!visited.Add(node)) continue;
            var orig = node.transform.localScale;
            node.transform.localScale = orig * 1.5f;
            yield return new WaitForSeconds(0.3f);
            node.transform.localScale = orig;
            foreach (var nb in adj[node])
                if (!visited.Contains(nb)) stack.Push(nb);
        }
    }

    /// <summary>
    /// Closes the LAN scenario: hides canvas and destroys all generated objects.
    /// </summary>
    private void OnCloseScenario()
    {
        if (lanCanvas != null)
            lanCanvas.gameObject.SetActive(false);
        if (frameObject != null)
            Destroy(frameObject);
        meshPCs.ForEach(Destroy);
        meshPCs.Clear();
        meshLines.ForEach(Destroy);
        meshLines.Clear();
    }
}
