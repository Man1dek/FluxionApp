namespace Fluxion.Core.Models;

/// <summary>
/// Represents a learner's current state in the Kinetic Curriculum.
/// </summary>
public class LearnerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Mastery scores per knowledge node.
    /// </summary>
    public List<LearnerMastery> MasteryEntries { get; set; } = [];

    /// <summary>Learner's preferred content format.</summary>
    public ContentFormat PreferredFormat { get; set; } = ContentFormat.Text;

    /// <summary>
    /// Current cognitive load index (0.0 = relaxed, 1.0 = overloaded).
    /// Updated in real-time by the Cognitive Load Analyzer.
    /// </summary>
    public double CognitiveLoadIndex { get; set; } = 0.0;

    /// <summary>Ordered history of completed sessions.</summary>
    public List<SessionRecord> SessionHistory { get; set; } = [];

    /// <summary>Helper to get mastery as a dictionary for AI/logic processing.</summary>
    public Dictionary<Guid, double> GetMasteryDict() => 
        MasteryEntries.ToDictionary(m => m.NodeId, m => m.MasteryScore);

    /// <summary>When the learner profile was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A record of a single learning session for analytics and morphing.
/// </summary>
public class SessionRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LearnerProfileId { get; set; }

    /// <summary>Which node was studied.</summary>
    public Guid NodeId { get; set; }

    /// <summary>Mastery score achieved (0.0–1.0).</summary>
    public double ScoreAchieved { get; set; }

    /// <summary>Time spent in seconds.</summary>
    public int TimeSpentSeconds { get; set; }

    /// <summary>Number of hints requested during the session.</summary>
    public int HintRequests { get; set; }

    /// <summary>Number of incorrect attempts.</summary>
    public int ErrorCount { get; set; }

    /// <summary>Content format used during this session.</summary>
    public ContentFormat FormatUsed { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
