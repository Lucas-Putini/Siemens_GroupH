using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WANPathfinder: Implements shortest-path computation on a WAN graph using Dijkstra's algorithm.
/// Topics from Project Requirements:
/// - Collections: Dictionary<TKey,TValue> for distances & previous nodes, List<T> for unvisited nodes, HashSet<T> for visited check, Queue<T> for BFS.
/// - Graph Algorithms: Dijkstra's algorithm for shortest path, BFS for node discovery.
/// - Sorting: List.Sort used to select the next node with minimal tentative distance.
/// - Data Structures: uses arrays, lists, dictionaries, sets, and queues as specified.
/// </summary>
public static class WANPathfinder
{
    /// <summary>
    /// Finds the shortest path between two WANNode instances.
    /// Uses Dijkstra's algorithm:
    /// 1. Initialize all node distances to infinity.
    /// 2. Use a List<WANNode> as a priority queue (sorted by distance).
    /// 3. Relax edges by iterating neighbors and updating distances.
    /// 4. Stop when endNode is reached or all nodes visited.
    /// 5. Reconstruct path from end to start using 'previous' dictionary.
    /// </summary>
    public static List<WANNode> FindShortestPath(WANNode startNode, WANNode endNode)
    {
        // Initialize dictionaries for tracking distances and path reconstruction
        Dictionary<WANNode, float> distances = new Dictionary<WANNode, float>();       // Node -> tentative distance
        Dictionary<WANNode, WANNode> previous = new Dictionary<WANNode, WANNode>();   // Node -> previous node in optimal path
        List<WANNode> unvisited = new List<WANNode>();                                 // List for nodes not yet processed

        // Discover all nodes reachable from start using BFS
        foreach (WANNode node in GetAllNodes(startNode))
        {
            distances[node] = float.MaxValue;  // Set initial distance to 'infinity'
            previous[node] = null;             // No predecessor yet
            unvisited.Add(node);               // Mark as unvisited
        }

        // Distance to start is zero
        distances[startNode] = 0f;

        // Main Dijkstra loop
        while (unvisited.Count > 0)
        {
            // Sort unvisited nodes by current distance (sorting operation)
            unvisited.Sort((a, b) => distances[a].CompareTo(distances[b]));
            WANNode current = unvisited[0];        // Node with smallest tentative distance
            unvisited.RemoveAt(0);                // Remove from unvisited

            if (current == endNode)
                break;                             // Destination reached

            // Relax edges: update neighbor distances
            foreach (WANNode neighbor in current.neighbors)
            {
                // Edge weight = Euclidean distance between node positions
                float edgeWeight = Vector3.Distance(current.position, neighbor.position);
                float tentative = distances[current] + edgeWeight;
                if (tentative < distances[neighbor])
                {
                    distances[neighbor] = tentative;  // Update distance
                    previous[neighbor] = current;    // Record path
                }
            }
        }

        // Reconstruct path from endNode to startNode
        List<WANNode> path = new List<WANNode>();
        WANNode step = endNode;
        while (step != null)
        {
            path.Insert(0, step);               // Prepend node to path
            step = previous[step];               // Move to predecessor
        }

        return path;
    }

    /// <summary>
    /// Discovers all nodes reachable from a starting node via BFS.
    /// Uses HashSet<T> to prevent revisiting nodes, Queue<T> for FIFO traversal.
    /// </summary>
    private static HashSet<WANNode> GetAllNodes(WANNode start)
    {
        HashSet<WANNode> visited = new HashSet<WANNode>();  // Tracks visited nodes
        Queue<WANNode> queue = new Queue<WANNode>();        // FIFO queue for traversal

        queue.Enqueue(start);
        visited.Add(start);

        // Standard BFS loop
        while (queue.Count > 0)
        {
            WANNode current = queue.Dequeue();
            foreach (WANNode neighbor in current.neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);    // Mark visited
                    queue.Enqueue(neighbor);  // Enqueue for further exploration
                }
            }
        }

        return visited;
    }
}
