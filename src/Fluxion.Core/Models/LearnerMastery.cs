namespace Fluxion.Core.Models;

public class LearnerMastery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LearnerProfileId { get; set; }
    public Guid NodeId { get; set; }
    public double MasteryScore { get; set; }
}
