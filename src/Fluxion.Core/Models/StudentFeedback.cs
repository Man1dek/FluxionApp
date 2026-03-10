namespace Fluxion.Core.Models;

/// <summary>
/// Result returned after evaluating a student's submission.
/// </summary>
public class StudentFeedback
{
    public double MasteryScore { get; set; }
    public string EvaluationDetails { get; set; } = string.Empty;
    public bool NodeMastered { get; set; }
    public string NodeTitle { get; set; } = string.Empty;
}
