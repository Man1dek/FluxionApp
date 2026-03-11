using Fluxion.AI;
using Fluxion.Core.Interfaces;
using Fluxion.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Fluxion.Core.Tests;

/// <summary>
/// Tests for MorphingEngine.CalculateAdjustedDifficulty (EMA logic).
/// The method is marked internal with [InternalsVisibleTo] for testability.
/// </summary>
public class MorphingEngineTests
{
    /// <summary>
    /// Helper to create a MorphingEngine without requiring real AI kernel/graph.
    /// CalculateAdjustedDifficulty doesn't use them so we pass nulls.
    /// </summary>
    private static MorphingEngine CreateEngine()
    {
        // CalculateAdjustedDifficulty is a pure calculation that doesn't use
        // Kernel, graph, or cognitiveAnalyzer, so we can pass nulls safely.
        return new MorphingEngine(
            kernel: null!,
            graph: null!,
            cognitiveAnalyzer: null!,
            logger: null!);
    }

    [Fact]
    public void CalculateAdjustedDifficulty_EmptySessions_ReturnsBaseDifficulty()
    {
        var engine = CreateEngine();
        var sessions = new List<SessionRecord>();

        var result = engine.CalculateAdjustedDifficulty(5, sessions, cognitiveLoad: 0.5);

        Assert.Equal(5, result);
    }

    [Fact]
    public void CalculateAdjustedDifficulty_HighMasteryLowLoad_IncreasesDifficulty()
    {
        var engine = CreateEngine();

        // All sessions scored 0.9 — learner is cruising
        var sessions = Enumerable.Range(0, 5)
            .Select(i => new SessionRecord
            {
                ScoreAchieved = 0.9,
                CompletedAt = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();

        var result = engine.CalculateAdjustedDifficulty(5, sessions, cognitiveLoad: 0.2);

        // EMA of all 0.9 scores > 0.85 and load 0.2 < 0.3 → adjustment +2
        Assert.Equal(7, result);
    }

    [Fact]
    public void CalculateAdjustedDifficulty_LowMasteryHighLoad_DecreasesDifficulty()
    {
        var engine = CreateEngine();

        // All sessions scored 0.3 — learner is struggling
        var sessions = Enumerable.Range(0, 5)
            .Select(i => new SessionRecord
            {
                ScoreAchieved = 0.3,
                CompletedAt = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();

        var result = engine.CalculateAdjustedDifficulty(5, sessions, cognitiveLoad: 0.8);

        // EMA of 0.3 < 0.45, so adjustment is -2
        Assert.Equal(3, result);
    }

    [Fact]
    public void CalculateAdjustedDifficulty_EmaWeightsRecentSessionsMore()
    {
        var engine = CreateEngine();

        // Sessions ordered most-recent-first: recent ones have HIGH scores,
        // older ones have LOW scores. With correct EMA (seed from oldest,
        // iterate toward newest), the EMA should be pulled UP by recent scores.
        var sessions = new List<SessionRecord>
        {
            new() { ScoreAchieved = 0.95, CompletedAt = DateTime.UtcNow.AddMinutes(-1) },  // most recent
            new() { ScoreAchieved = 0.90, CompletedAt = DateTime.UtcNow.AddMinutes(-2) },
            new() { ScoreAchieved = 0.85, CompletedAt = DateTime.UtcNow.AddMinutes(-3) },
            new() { ScoreAchieved = 0.30, CompletedAt = DateTime.UtcNow.AddMinutes(-4) },  // older, low
            new() { ScoreAchieved = 0.20, CompletedAt = DateTime.UtcNow.AddMinutes(-5) },  // oldest, low
        };

        var result = engine.CalculateAdjustedDifficulty(5, sessions, cognitiveLoad: 0.2);

        // With the corrected EMA (recent data has highest weight), the trend
        // should show improvement. The result should be >= base difficulty.
        Assert.True(result >= 5, $"Expected difficulty >= 5 but got {result}. " +
                                  "EMA should weight recent high scores more than older low scores.");
    }

    [Fact]
    public void CalculateAdjustedDifficulty_ResultIsClampedBetween1And10()
    {
        var engine = CreateEngine();

        // Very high scores should not push difficulty above 10
        var highSessions = Enumerable.Range(0, 5)
            .Select(i => new SessionRecord
            {
                ScoreAchieved = 0.95,
                CompletedAt = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();

        var highResult = engine.CalculateAdjustedDifficulty(10, highSessions, cognitiveLoad: 0.1);
        Assert.True(highResult <= 10, $"Difficulty should not exceed 10, got {highResult}");

        // Very low scores should not push difficulty below 1
        var lowSessions = Enumerable.Range(0, 5)
            .Select(i => new SessionRecord
            {
                ScoreAchieved = 0.1,
                CompletedAt = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();

        var lowResult = engine.CalculateAdjustedDifficulty(1, lowSessions, cognitiveLoad: 0.9);
        Assert.True(lowResult >= 1, $"Difficulty should not go below 1, got {lowResult}");
    }
}
