namespace Fluxion.Core.Models;

/// <summary>
/// A directed edge in the Knowledge Graph connecting two concept nodes.
/// </summary>
public class KnowledgeEdge
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Source node ID.</summary>
    public Guid SourceNodeId { get; set; }

    /// <summary>Target node ID.</summary>
    public Guid TargetNodeId { get; set; }

    /// <summary>Semantic relationship type.</summary>
    public EdgeRelation Relation { get; set; } = EdgeRelation.PrerequisiteOf;

    /// <summary>
    /// Weight (0.0–1.0) indicating the strength of this relationship.
    /// Higher = stronger prerequisite / deeper connection.
    /// </summary>
    public double Weight { get; set; } = 1.0;
}

/// <summary>
/// Types of relationships between knowledge nodes.
/// </summary>
public enum EdgeRelation
{
    /// <summary>Source must be mastered before Target.</summary>
    PrerequisiteOf,

    /// <summary>Source and Target share overlapping concepts.</summary>
    RelatedTo,

    /// <summary>Target is a deeper dive into the Source topic.</summary>
    DeepensInto
}
