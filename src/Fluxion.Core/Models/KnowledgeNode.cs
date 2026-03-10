namespace Fluxion.Core.Models;

/// <summary>
/// A vertex in the Knowledge Graph representing a single learnable concept.
/// </summary>
public class KnowledgeNode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Short title, e.g. "Variables & Data Types".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Rich description of the concept.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Difficulty on a 1-10 scale (1 = beginner, 10 = expert).</summary>
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>Default content delivery format.</summary>
    public ContentFormat DefaultFormat { get; set; } = ContentFormat.Text;

    /// <summary>Mastery score (0.0–1.0) the learner must reach before this node is "complete".</summary>
    public double MasteryThreshold { get; set; } = 0.7;

    /// <summary>Tags for categorising (e.g. "csharp", "fundamentals").</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Estimated minutes to complete.</summary>
    public int EstimatedMinutes { get; set; } = 15;
}

/// <summary>
/// How content can be delivered.
/// </summary>
public enum ContentFormat
{
    Text,
    Visual,
    Interactive,
    Video,
    Mixed
}
