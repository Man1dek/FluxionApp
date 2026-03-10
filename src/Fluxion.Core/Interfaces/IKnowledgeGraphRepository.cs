using Fluxion.Core.Models;

namespace Fluxion.Core.Interfaces;

/// <summary>
/// Abstraction over the Knowledge Graph store.
/// Implement with Neo4j, Cosmos DB Gremlin, or the included in-memory graph.
/// </summary>
public interface IKnowledgeGraphRepository
{
    // ── Nodes ──────────────────────────────────────────────

    Task<KnowledgeNode> AddNodeAsync(KnowledgeNode node);
    Task<KnowledgeNode?> GetNodeAsync(Guid nodeId);
    Task<IReadOnlyList<KnowledgeNode>> GetAllNodesAsync();
    Task<IReadOnlyList<KnowledgeNode>> GetNodesByTagAsync(string tag);

    // ── Edges ──────────────────────────────────────────────

    Task<KnowledgeEdge> AddEdgeAsync(KnowledgeEdge edge);
    Task<IReadOnlyList<KnowledgeEdge>> GetEdgesFromAsync(Guid sourceNodeId);
    Task<IReadOnlyList<KnowledgeEdge>> GetEdgesToAsync(Guid targetNodeId);

    /// <summary>
    /// Returns the prerequisite nodes that must be mastered before
    /// the learner can attempt <paramref name="nodeId"/>.
    /// </summary>
    Task<IReadOnlyList<KnowledgeNode>> GetPrerequisitesAsync(Guid nodeId);

    /// <summary>
    /// Returns the candidate "next" nodes for a learner, i.e. nodes whose
    /// prerequisites have all been mastered.
    /// </summary>
    Task<IReadOnlyList<KnowledgeNode>> GetNextCandidatesAsync(
        Dictionary<Guid, double> currentMastery);

    // ── Learner Profiles ───────────────────────────────────

    Task<LearnerProfile> CreateLearnerAsync(LearnerProfile profile);
    Task<LearnerProfile?> GetLearnerAsync(Guid learnerId);
    Task UpdateMasteryAsync(Guid learnerId, Guid nodeId, double newScore);
    Task AddSessionRecordAsync(Guid learnerId, SessionRecord record);

    // ── Graph Analytics ────────────────────────────────────

    /// <summary>
    /// Returns a topologically-sorted learning path from current mastery
    /// to a target node, respecting prerequisite chains.
    /// </summary>
    Task<IReadOnlyList<KnowledgeNode>> GetLearningPathAsync(
        Guid targetNodeId, Dictionary<Guid, double> currentMastery);
}
