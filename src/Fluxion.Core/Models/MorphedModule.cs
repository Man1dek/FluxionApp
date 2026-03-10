namespace Fluxion.Core.Models;

/// <summary>
/// The output of the Kinetic Morphing Engine — a dynamically generated
/// learning module tailored to the learner's current state.
/// </summary>
public class MorphedModule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The knowledge node this module is about.</summary>
    public Guid NodeId { get; set; }

    public string NodeTitle { get; set; } = string.Empty;

    /// <summary>AI-generated learning content (markdown).</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Adjusted difficulty (may differ from the node's base difficulty).</summary>
    public int AdjustedDifficulty { get; set; }

    /// <summary>Selected content format based on cognitive load.</summary>
    public ContentFormat SelectedFormat { get; set; }

    /// <summary>AI-generated practice questions.</summary>
    public List<string> PracticeQuestions { get; set; } = [];

    /// <summary>Why the engine chose this module and difficulty.</summary>
    public string MorphingRationale { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
