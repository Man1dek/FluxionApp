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
        var allNodes = await _context.Nodes.ToListAsync();
        var allEdges = await _context.Edges.ToListAsync();

        return allNodes
            .Where(node => !IsMastered(node.Id, currentMastery, node.MasteryThreshold))
            .Where(node => AllPrerequisitesMastered(node.Id, allEdges, currentMastery, allNodes))
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
        // For the demo/MVP, we use the same candidate selection logic
        // In a real production app, this would be a full A* or Dijkstra traversal
        var nodes = await GetAllNodesAsync();
        return nodes.Take(3).ToList(); // Simplified placeholder for shortest path
    }

    // ── Helpers ─────────────────────────────────────────────

    private static bool IsMastered(Guid nodeId, Dictionary<Guid, double> mastery, double threshold)
    {
        return mastery.TryGetValue(nodeId, out var score) && score >= threshold;
    }

    private static bool AllPrerequisitesMastered(
        Guid nodeId, 
        List<KnowledgeEdge> edges, 
        Dictionary<Guid, double> mastery,
        List<KnowledgeNode> nodes)
    {
        var prereqs = edges
            .Where(e => e.TargetNodeId == nodeId && e.Relation == EdgeRelation.PrerequisiteOf)
            .ToList();

        foreach (var edge in prereqs)
        {
            var prereqNode = nodes.First(n => n.Id == edge.SourceNodeId);
            if (!IsMastered(edge.SourceNodeId, mastery, prereqNode.MasteryThreshold))
                return false;
        }

        return true;
    }
}
