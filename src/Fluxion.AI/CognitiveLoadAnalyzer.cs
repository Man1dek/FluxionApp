using Fluxion.Core.Models;

namespace Fluxion.AI;

/// <summary>
/// Analyses session signals to produce a real-time cognitive load index (0.0–1.0).
///
/// Design Note – AMD Acceleration:
/// For production deployments on AMD-accelerated infrastructure, this analyzer
/// can be extended to use ONNX Runtime with DirectML (AMD GPU) for local
/// inference of a trained cognitive-load prediction model. The current
/// implementation uses a heuristic formula suitable for prototyping.
///
/// To enable ONNX + DirectML:
///   1. Install: Microsoft.ML.OnnxRuntime.DirectML NuGet package
///   2. Export a trained model (e.g. from PyTorch) to ONNX format
///   3. Create an InferenceSession with the DirectML execution provider
///   4. Replace the heuristic in CalculateFromSession with model inference
/// </summary>
public class CognitiveLoadAnalyzer
{
    /// <summary>
    /// Weights for the cognitive load heuristic.
    /// Tunable per deployment.
    /// </summary>
    private const double ErrorWeight = 0.35;
    private const double TimeWeight = 0.30;
    private const double HintWeight = 0.20;
    private const double TrendWeight = 0.15;

    /// <summary>
    /// Calculates cognitive load from the most recent session signals.
    /// </summary>
    /// <param name="recentSessions">Last N sessions (most recent first)</param>
    /// <param name="expectedTimeSeconds">Expected time for the current difficulty</param>
    /// <returns>Cognitive load index 0.0 (relaxed) – 1.0 (overloaded)</returns>
    public double Calculate(IReadOnlyList<SessionRecord> recentSessions, int expectedTimeSeconds = 300)
    {
        if (recentSessions.Count == 0)
            return 0.3; // Default to moderate for new learners

        var latest = recentSessions[0];

        // ── Error component ─────────────────────────────────
        // More errors → higher cognitive load
        var errorRatio = Math.Min(latest.ErrorCount / 5.0, 1.0);

        // ── Time component ──────────────────────────────────
        // Taking much longer than expected → higher load
        var timeRatio = expectedTimeSeconds > 0
            ? Math.Min((double)latest.TimeSpentSeconds / expectedTimeSeconds, 2.0) / 2.0
            : 0.5;

        // ── Hint component ──────────────────────────────────
        // More hints → higher load
        var hintRatio = Math.Min(latest.HintRequests / 3.0, 1.0);

        // ── Trend component ─────────────────────────────────
        // Declining mastery scores → higher load
        var trendLoad = CalculateMasteryTrend(recentSessions);

        var rawLoad = (errorRatio * ErrorWeight) +
                      (timeRatio * TimeWeight) +
                      (hintRatio * HintWeight) +
                      (trendLoad * TrendWeight);

        return Math.Clamp(rawLoad, 0.0, 1.0);
    }

    /// <summary>
    /// Calculates mastery trend from recent sessions.
    /// Declining scores produce a value closer to 1.0,
    /// improving scores produce a value closer to 0.0.
    /// </summary>
    private double CalculateMasteryTrend(IReadOnlyList<SessionRecord> sessions)
    {
        if (sessions.Count < 2)
            return 0.5; // Neutral with insufficient data

        // Calculate slope using simple linear regression on last 5 sessions
        var recent = sessions.Take(5).ToList();
        var n = recent.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

        for (int i = 0; i < n; i++)
        {
            double x = i;
            double y = recent[i].ScoreAchieved;
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        var denominator = (n * sumX2) - (sumX * sumX);
        if (Math.Abs(denominator) < 1e-10)
            return 0.5;

        var slope = ((n * sumXY) - (sumX * sumY)) / denominator;

        // Negative slope (declining) → high load (closer to 1)
        // Positive slope (improving) → low load (closer to 0)
        // Map slope from [-1, 1] to [1, 0]
        return Math.Clamp(0.5 - slope, 0.0, 1.0);
    }

    /// <summary>
    /// Determines the optimal content format based on cognitive load.
    /// </summary>
    public ContentFormat RecommendFormat(double cognitiveLoad, ContentFormat preferred)
    {
        return cognitiveLoad switch
        {
            // Overloaded → switch to visual/simple format
            > 0.75 => ContentFormat.Visual,

            // High load → use mixed to break monotony
            > 0.55 => ContentFormat.Mixed,

            // Moderate → use learner's preference
            > 0.30 => preferred,

            // Low load → interactive challenges to prevent boredom
            _ => ContentFormat.Interactive
        };
    }
}
