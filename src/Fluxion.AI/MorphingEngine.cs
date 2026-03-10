using System.Text.Json;
using Fluxion.Core.Interfaces;
using Fluxion.Core.Models;
using Microsoft.SemanticKernel;
using Polly;
using Polly.Retry;

namespace Fluxion.AI;

/// <summary>
/// The Kinetic Morphing Engine — the heart of Fluxion.
///
/// This engine dynamically "morphs" the next learning module by:
///   1. Analysing the learner's recent performance trend
///   2. Computing cognitive load from session signals
///   3. Adjusting difficulty using exponential moving average
///   4. Selecting the optimal content format
///   5. Calling the Semantic Kernel to generate personalised content
/// </summary>
public class MorphingEngine
{
    private readonly Kernel _kernel;
    private readonly IKnowledgeGraphRepository _graph;
    private readonly CognitiveLoadAnalyzer _cognitiveAnalyzer;
    private readonly AsyncRetryPolicy _retryPolicy;

    public MorphingEngine(
        Kernel kernel,
        IKnowledgeGraphRepository graph,
        CognitiveLoadAnalyzer cognitiveAnalyzer)
    {
        _kernel = kernel;
        _graph = graph;
        _cognitiveAnalyzer = cognitiveAnalyzer;

        // Exponential backoff: retry 3 times (2s, 4s, 8s) on transient AI errors
        _retryPolicy = Policy
            .Handle<Exception>(ex => ex.Message.Contains("429") || ex.Message.Contains("503") || ex.Message.Contains("rate limit"))
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    /// <summary>
    /// Generates the next morphed module for a learner.
    /// This is the primary orchestration entry point.
    /// </summary>
    public async Task<MorphedModule> MorphNextModuleAsync(Guid learnerId)
    {
        var learner = await _graph.GetLearnerAsync(learnerId)
            ?? throw new ArgumentException($"Learner {learnerId} not found");

        // Step 1: Update cognitive load from recent sessions
        var recentSessions = learner.SessionHistory
            .OrderByDescending(s => s.CompletedAt)
            .Take(5)
            .ToList();

        learner.CognitiveLoadIndex = _cognitiveAnalyzer.Calculate(recentSessions);

        // Step 2: Get candidate next nodes from the graph
        var candidates = await _graph.GetNextCandidatesAsync(learner.GetMasteryDict());

        if (candidates.Count == 0)
        {
            return new MorphedModule
            {
                NodeTitle = "🎉 Curriculum Complete!",
                Content = "Congratulations! You have mastered all available modules in this curriculum.",
                AdjustedDifficulty = 0,
                SelectedFormat = ContentFormat.Text,
                MorphingRationale = "All nodes in the knowledge graph have been mastered."
            };
        }

        // Step 3: Ask the AI to recommend the best next node
        var selectedNode = await SelectBestNodeAsync(candidates, learner);

        // Step 4: Calculate adjusted difficulty
        var adjustedDifficulty = CalculateAdjustedDifficulty(
            selectedNode.DifficultyLevel, recentSessions, learner.CognitiveLoadIndex);

        // Step 5: Select content format based on cognitive load
        var selectedFormat = _cognitiveAnalyzer.RecommendFormat(
            learner.CognitiveLoadIndex, learner.PreferredFormat);

        // Step 6: Generate the module content using Semantic Kernel
        var content = await GenerateContentAsync(
            selectedNode, adjustedDifficulty, selectedFormat, learner);

        var morphedModule = new MorphedModule
        {
            NodeId = selectedNode.Id,
            NodeTitle = selectedNode.Title,
            Content = content,
            AdjustedDifficulty = adjustedDifficulty,
            SelectedFormat = selectedFormat,
            PracticeQuestions = ExtractPracticeQuestions(content),
            MorphingRationale = BuildRationale(
                selectedNode, adjustedDifficulty, selectedFormat, learner.CognitiveLoadIndex)
        };

        return morphedModule;
    }

    /// <summary>
    /// Processes a student's submission, updates mastery, and returns feedback.
    /// </summary>
    public async Task<StudentFeedback> SubmitResponseAsync(
        Guid learnerId, Guid nodeId, string response, int timeSpentSeconds,
        int hintRequests, int errorCount)
    {
        var learner = await _graph.GetLearnerAsync(learnerId)
            ?? throw new ArgumentException($"Learner {learnerId} not found");

        var node = await _graph.GetNodeAsync(nodeId)
            ?? throw new ArgumentException($"Node {nodeId} not found");

        // Call AI to evaluate the response
        var plugin = new Plugins.CurriculumPlugin();
        var evaluationJson = await plugin.EvaluateStudentResponseAsync(
            _kernel,
            node.Title,
            node.DifficultyLevel,
            response,
            node.Description);

        // Parse the mastery score from AI response
        var masteryScore = ParseMasteryScore(evaluationJson);

        // Record the session
        var session = new SessionRecord
        {
            NodeId = nodeId,
            ScoreAchieved = masteryScore,
            TimeSpentSeconds = timeSpentSeconds,
            HintRequests = hintRequests,
            ErrorCount = errorCount,
            FormatUsed = learner.PreferredFormat
        };

        await _graph.AddSessionRecordAsync(learnerId, session);
        await _graph.UpdateMasteryAsync(learnerId, nodeId, masteryScore);

        return new StudentFeedback
        {
            MasteryScore = masteryScore,
            EvaluationDetails = evaluationJson,
            NodeMastered = masteryScore >= node.MasteryThreshold,
            NodeTitle = node.Title
        };
    }

    // ── Private Helpers ─────────────────────────────────────

    /// <summary>
    /// Uses an exponential moving average of recent mastery scores to
    /// adjust the next module's difficulty.
    /// </summary>
    private int CalculateAdjustedDifficulty(
        int baseDifficulty, IReadOnlyList<SessionRecord> recentSessions, double cognitiveLoad)
    {
        if (recentSessions.Count == 0)
            return baseDifficulty;

        // EMA with alpha = 0.4 (recent sessions weighted more)
        const double alpha = 0.4;
        double ema = recentSessions[0].ScoreAchieved;

        for (int i = 1; i < recentSessions.Count; i++)
        {
            ema = (alpha * recentSessions[i].ScoreAchieved) + ((1 - alpha) * ema);
        }

        // High mastery trend + low cognitive load → increase difficulty
        // Low mastery trend + high cognitive load → decrease difficulty
        var adjustment = ema switch
        {
            > 0.85 when cognitiveLoad < 0.3 => 2,   // Cruising → challenge more
            > 0.85 => 1,                              // Doing well
            > 0.65 => 0,                              // On track
            > 0.45 when cognitiveLoad > 0.7 => -2,   // Struggling + overloaded
            > 0.45 => -1,                             // Struggling slightly
            _ => -2                                   // Really struggling
        };

        return Math.Clamp(baseDifficulty + adjustment, 1, 10);
    }

    private async Task<KnowledgeNode> SelectBestNodeAsync(
        IReadOnlyList<KnowledgeNode> candidates, LearnerProfile learner)
    {
        if (candidates.Count == 1)
            return candidates[0];

        try
        {
            // Ask AI for recommendation
            var candidatesJson = JsonSerializer.Serialize(
                candidates.Select(c => new { c.Title, c.DifficultyLevel }));

            var masteryJson = JsonSerializer.Serialize(
                learner.GetMasteryDict().ToDictionary(
                    kvp => kvp.Key.ToString()[..8], kvp => kvp.Value));

            var plugin = new Plugins.CurriculumPlugin();
            var recommendation = await _retryPolicy.ExecuteAsync(() => 
                plugin.RecommendNextNodesAsync(_kernel, candidatesJson, masteryJson, learner.CognitiveLoadIndex));

            // Try to match the first recommended title to a candidate
            foreach (var candidate in candidates)
            {
                if (recommendation.Contains(candidate.Title, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }
        }
        catch
        {
            // Fallback to heuristic if AI fails
        }

        // Heuristic fallback: pick the lowest-difficulty unmastered candidate
        return candidates
            .OrderBy(c => Math.Abs(c.DifficultyLevel - GetAverageMastery(learner) * 10))
            .First();
    }

    private async Task<string> GenerateContentAsync(
        KnowledgeNode node, int difficulty, ContentFormat format, LearnerProfile learner)
    {
        try
        {
            var plugin = new Plugins.CurriculumPlugin();
            var gaps = string.Join(", ",
                learner.GetMasteryDict()
                    .Where(kvp => kvp.Value < 0.5)
                    .Select(kvp => kvp.Key.ToString()[..8]));

            return await _retryPolicy.ExecuteAsync(() => 
                plugin.GenerateModuleContentAsync(
                    _kernel,
                    node.Title,
                    node.Description,
                    difficulty,
                    format.ToString(),
                    gaps.Length > 0 ? gaps : "None identified"));
        }
        catch (Exception ex)
        {
            // Fallback content if AI generation fails
            return $"""
                # {node.Title}
                
                ## Learning Objective
                {node.Description}
                
                ## Difficulty: {difficulty}/10
                
                *AI content generation is temporarily unavailable. Error: {ex.Message}*
                
                Please review the topic description above and try again shortly.
                """;
        }
    }

    private static double ParseMasteryScore(string evaluationJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(evaluationJson);
            if (doc.RootElement.TryGetProperty("masteryScore", out var scoreElement))
                return Math.Clamp(scoreElement.GetDouble(), 0.0, 1.0);
        }
        catch { }
        return 0.5; // Default to moderate if parsing fails
    }

    private static List<string> ExtractPracticeQuestions(string content)
    {
        // Simple extraction: find lines starting with numbered questions
        return content.Split('\n')
            .Where(line =>
            {
                var trimmed = line.TrimStart();
                return trimmed.Length > 3 &&
                       char.IsDigit(trimmed[0]) &&
                       trimmed[1] == '.' &&
                       trimmed[2] == ' ';
            })
            .Select(line => line.Trim())
            .Take(5)
            .ToList();
    }

    private static double GetAverageMastery(LearnerProfile learner)
    {
        return learner.MasteryEntries.Count > 0
            ? learner.MasteryEntries.Average(m => m.MasteryScore)
            : 0.0;
    }

    private static string BuildRationale(
        KnowledgeNode node, int adjustedDifficulty, ContentFormat format, double cognitiveLoad)
    {
        var loadLevel = cognitiveLoad switch
        {
            > 0.75 => "HIGH — switching to lighter content",
            > 0.55 => "MODERATE — balanced approach",
            > 0.30 => "NORMAL — following preferences",
            _ => "LOW — increasing challenge"
        };

        return $"Selected '{node.Title}' (base difficulty {node.DifficultyLevel} → " +
               $"adjusted {adjustedDifficulty}). Format: {format}. " +
               $"Cognitive Load: {cognitiveLoad:P0} ({loadLevel}).";
    }
}
