namespace Fluxion.Core.Models;

public class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Guid? LearnerProfileId { get; set; }
    public LearnerProfile? LearnerProfile { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
