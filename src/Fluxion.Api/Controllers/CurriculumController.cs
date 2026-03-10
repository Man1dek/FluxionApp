using Fluxion.AI;
using Fluxion.Core.Interfaces;
using Fluxion.Core.Models;
using Fluxion.Api.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using Microsoft.AspNetCore.Authorization;

namespace Fluxion.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CurriculumController : ControllerBase
{
    private readonly IKnowledgeGraphRepository _graph;
    private readonly MorphingEngine _morphingEngine;
    private readonly IHubContext<CurriculumHub> _hubContext;

    public CurriculumController(
        IKnowledgeGraphRepository graph,
        MorphingEngine morphingEngine,
        IHubContext<CurriculumHub> hubContext)
    {
        _graph = graph;
        _morphingEngine = morphingEngine;
        _hubContext = hubContext;
    }

    // ── Graph Endpoints ─────────────────────────────────

    /// <summary>GET /api/curriculum/graph/nodes — all knowledge nodes</summary>
    [HttpGet("graph/nodes")]
    public async Task<IActionResult> GetAllNodes()
    {
        var nodes = await _graph.GetAllNodesAsync();
        return Ok(nodes);
    }

    /// <summary>GET /api/curriculum/graph/nodes/{id} — single node</summary>
    [HttpGet("graph/nodes/{id:guid}")]
    public async Task<IActionResult> GetNode(Guid id)
    {
        var node = await _graph.GetNodeAsync(id);
        return node is null ? NotFound() : Ok(node);
    }

    /// <summary>GET /api/curriculum/graph/path/{targetId}?learnerId=... — learning path</summary>
    [HttpGet("graph/path/{targetId:guid}")]
    public async Task<IActionResult> GetLearningPath(Guid targetId, [FromQuery] Guid learnerId)
    {
        var learner = await _graph.GetLearnerAsync(learnerId);
        if (learner is null) return NotFound("Learner not found");

        var path = await _graph.GetLearningPathAsync(targetId, learner.GetMasteryDict());
        return Ok(path);
    }

    // ── Learner Endpoints ───────────────────────────────

    /// <summary>POST /api/curriculum/learners — create a new learner</summary>
    [HttpPost("learners")]
    public async Task<IActionResult> CreateLearner([FromBody] CreateLearnerRequest request)
    {
        var profile = new LearnerProfile
        {
            DisplayName = request.DisplayName,
            PreferredFormat = request.PreferredFormat
        };

        var created = await _graph.CreateLearnerAsync(profile);
        return CreatedAtAction(nameof(GetLearner), new { id = created.Id }, created);
    }

    /// <summary>GET /api/curriculum/learners/{id}</summary>
    [HttpGet("learners/{id:guid}")]
    public async Task<IActionResult> GetLearner(Guid id)
    {
        var learner = await _graph.GetLearnerAsync(id);
        return learner is null ? NotFound() : Ok(learner);
    }

    // ── Morphing Engine Endpoints ───────────────────────

    /// <summary>GET /api/curriculum/next/{learnerId} — get the next morphed module</summary>
    [HttpGet("next/{learnerId:guid}")]
    public async Task<IActionResult> GetNextModule(Guid learnerId)
    {
        try
        {
            var module = await _morphingEngine.MorphNextModuleAsync(learnerId);

            // Push real-time update to the learner's SignalR connection
            await _hubContext.Clients.Group(learnerId.ToString())
                .SendAsync("ModuleMorphed", module);

            return Ok(module);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>POST /api/curriculum/submit — submit a student response</summary>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitResponse([FromBody] SubmitResponseRequest request)
    {
        try
        {
            var feedback = await _morphingEngine.SubmitResponseAsync(
                request.LearnerId,
                request.NodeId,
                request.Response,
                request.TimeSpentSeconds,
                request.HintRequests,
                request.ErrorCount);

            // Push real-time mastery update
            await _hubContext.Clients.Group(request.LearnerId.ToString())
                .SendAsync("MasteryUpdated", feedback);

            return Ok(feedback);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

// ── Request DTOs ─────────────────────────────────────────

public record CreateLearnerRequest(
    string DisplayName,
    ContentFormat PreferredFormat = ContentFormat.Text);

public record SubmitResponseRequest(
    Guid LearnerId,
    Guid NodeId,
    string Response,
    int TimeSpentSeconds = 0,
    int HintRequests = 0,
    int ErrorCount = 0);
