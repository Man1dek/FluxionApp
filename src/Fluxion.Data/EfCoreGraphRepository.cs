using Fluxion.Core.Interfaces;
using Fluxion.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Fluxion.Data;

public class EfCoreGraphRepository : IKnowledgeGraphRepository
{
    private readonly FluxionDbContext _context;

    public EfCoreGraphRepository(FluxionDbContext context)
    {
        _context = context;
    }

    // ── Nodes ──────────────────────────────────────────────

    public async Task<KnowledgeNode> AddNodeAsync(KnowledgeNode node)
    {
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();
        return node;
    }

    public async Task<KnowledgeNode?> GetNodeAsync(Guid nodeId)
    {
        return await _context.Nodes.FindAsync(nodeId);
    }

    public async Task<IReadOnlyList<KnowledgeNode>> GetAllNodesAsync()
    {
        return await _context.Nodes.ToListAsync();
    }

    public async Task<IReadOnlyList<KnowledgeNode>> GetNodesByTagAsync(string tag)
    {
        return await _context.Nodes
            .Where(n => n.Tags.Contains(tag))
            .ToListAsync();
    }

    // ── Edges ──────────────────────────────────────────────

    public async Task<KnowledgeEdge> AddEdgeAsync(KnowledgeEdge edge)
    {
        _context.Edges.Add(edge);
        await _context.SaveChangesAsync();
        return edge;
    }

    public async Task<IReadOnlyList<KnowledgeEdge>> GetEdgesFromAsync(Guid sourceNodeId)
    {
        return await _context.Edges
            .Where(e => e.SourceNodeId == sourceNodeId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<KnowledgeEdge>> GetEdgesToAsync(Guid targetNodeId)
    {
        return await _context.Edges
            .Where(e => e.TargetNodeId == targetNodeId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<KnowledgeNode>> GetPrerequisitesAsync(Guid nodeId)
    {
        var prereqIds = await _context.Edges
            .Where(e => e.TargetNodeId == nodeId && e.Relation == EdgeRelation.PrerequisiteOf)
            .Select(e => e.SourceNodeId)
            .ToListAsync();

        return await _context.Nodes
            .Where(n => prereqIds.Contains(n.Id))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<KnowledgeNode>> GetNextCandidatesAsync(
        Dictionary<Guid, double> currentMastery)
    {
        // Get IDs of nodes that the learner has already mastered
        var masteredNodeIds = currentMastery
            .Where(kvp => kvp.Value >= 0.7) // default threshold; refined below
            .Select(kvp => kvp.Key)
            .ToHashSet();

        // Pull only unmastered nodes from the database
        var unmasteredNodes = await _context.Nodes
            .Where(n => !masteredNodeIds.Contains(n.Id))
            .ToListAsync();

        // Further filter: only keep nodes whose mastery is actually below their specific threshold
        unmasteredNodes = unmasteredNodes
            .Where(n => !IsMastered(n.Id, currentMastery, n.MasteryThreshold))
            .ToList();

        if (unmasteredNodes.Count == 0)
            return unmasteredNodes;

        // Pull only prerequisite edges for the unmastered nodes
        var unmasteredNodeIds = unmasteredNodes.Select(n => n.Id).ToList();
        var prerequisiteEdges = await _context.Edges
            .Where(e => unmasteredNodeIds.Contains(e.TargetNodeId) 
                     && e.Relation == EdgeRelation.PrerequisiteOf)
            .ToListAsync();

        // Filter to nodes whose prerequisites are all mastered
        return unmasteredNodes
            .Where(node =>
            {
                var prereqs = prerequisiteEdges
                    .Where(e => e.TargetNodeId == node.Id)
                    .ToList();

                return prereqs.All(edge =>
                    currentMastery.TryGetValue(edge.SourceNodeId, out var score)
                    && score >= (unmasteredNodes.FirstOrDefault(n => n.Id == edge.SourceNodeId)?.MasteryThreshold ?? 0.7));
            })
            .ToList();
    }

    // ── Learner Profiles ───────────────────────────────────

    public async Task<LearnerProfile> CreateLearnerAsync(LearnerProfile profile)
    {
        _context.Learners.Add(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    public async Task<LearnerProfile?> GetLearnerAsync(Guid learnerId)
    {
        return await _context.Learners
            .Include(l => l.MasteryEntries)
            .Include(l => l.SessionHistory)
            .FirstOrDefaultAsync(l => l.Id == learnerId);
    }

    public async Task UpdateMasteryAsync(Guid learnerId, Guid nodeId, double newScore)
    {
        var entry = await _context.MasteryEntries
            .FirstOrDefaultAsync(m => m.LearnerProfileId == learnerId && m.NodeId == nodeId);

        if (entry == null)
        {
            _context.MasteryEntries.Add(new LearnerMastery
            {
                LearnerProfileId = learnerId,
                NodeId = nodeId,
                MasteryScore = newScore
            });
        }
        else
        {
            entry.MasteryScore = Math.Max(entry.MasteryScore, newScore); // Keep highest
        }

        await _context.SaveChangesAsync();
    }

    public async Task AddSessionRecordAsync(Guid learnerId, SessionRecord record)
    {
        record.LearnerProfileId = learnerId;
        _context.Sessions.Add(record);
        await _context.SaveChangesAsync();
    }

    // ── Graph Analytics ────────────────────────────────────

    public async Task<IReadOnlyList<KnowledgeNode>> GetLearningPathAsync(
        Guid targetNodeId, Dictionary<Guid, double> currentMastery)
    {
        // BFS backwards from the target node through prerequisite edges,
        // collecting all unmastered nodes in the dependency chain.
        var allNodes = await _context.Nodes.ToDictionaryAsync(n => n.Id);
        var allEdges = await _context.Edges
            .Where(e => e.Relation == EdgeRelation.PrerequisiteOf)
            .ToListAsync();

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        var path = new List<KnowledgeNode>();

        queue.Enqueue(targetNodeId);
        visited.Add(targetNodeId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            if (!allNodes.TryGetValue(currentId, out var currentNode))
                continue;

            // Add to path if not yet mastered
            if (!IsMastered(currentId, currentMastery, currentNode.MasteryThreshold))
            {
                path.Add(currentNode);
            }

            // Walk backwards through prerequisite edges
            var prereqSourceIds = allEdges
                .Where(e => e.TargetNodeId == currentId)
                .Select(e => e.SourceNodeId);

            foreach (var prereqId in prereqSourceIds)
            {
                if (visited.Add(prereqId))
                {
                    queue.Enqueue(prereqId);
                }
            }
        }

        // Reverse so prerequisites come first (topological order)
        path.Reverse();
        return path;
    }

    // ── Helpers ─────────────────────────────────────────────

    private static bool IsMastered(Guid nodeId, Dictionary<Guid, double> mastery, double threshold)
    {
        return mastery.TryGetValue(nodeId, out var score) && score >= threshold;
    }
}
