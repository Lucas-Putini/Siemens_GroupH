using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WANNode: Represents a node in the WAN graph.
/// Implements:
/// - Classes & Structs: custom class with properties and constructor (Project Req: Classes & Structs)
/// - Collections: List<WANNode> for neighbor storage (Project Req: Lists & Collections)
/// - Equality & Hashing: overrides Equals and GetHashCode for Dictionary/HashSet usage (Project Req: Data Structures)
/// - Optional identification: generationID to track node creation runs (Project Req: Structs & Classes)
/// </summary>
public class WANNode
{
    // Public node identifier (used as key in dictionaries and UI labeling)
    public string name;

    // 3D position on the Earth's surface (Vector3: Project Req: Structs)
    public Vector3 position;

    // Adjacency list for graph connections (List<T>: Project Req: Lists)
    public List<WANNode> neighbors = new List<WANNode>();

    // Linked GameObject marker for visualization (UnityEngine.GameObject)
    public GameObject marker;

    // Identifier for the generation batch, useful when regenerating networks (int type: Project Req: Primitive Types)
    public int generationID;

    /// <summary>
    /// Constructor: Initializes name, position, and optional generationID.
    /// Demonstrates class instantiation and constructor overloading.
    /// </summary>
    public WANNode(string name, Vector3 position, int generationID = 0)
    {
        this.name = name;
        this.position = position;
        this.generationID = generationID;
    }

    /// <summary>
    /// Overrides Equals to allow correct key comparisons in dictionaries and hash sets.
    /// Includes generationID to distinguish nodes across regenerations.
    /// </summary>
    public override bool Equals(object obj)
    {
        if (obj is not WANNode other) return false;
        // Compare by name, position, and generationID
        return name == other.name
            && position == other.position
            && generationID == other.generationID;
    }

    /// <summary>
    /// Generates a hash code combining name, position, and generationID.
    /// Ensures consistency with Equals override for Dictionary/HashSet usage.
    /// </summary>
    public override int GetHashCode()
    {
        // XOR of individual hash codes (struct and primitive types)
        return name.GetHashCode()
             ^ position.GetHashCode()
             ^ generationID.GetHashCode();
    }
}
