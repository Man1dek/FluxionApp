using Fluxion.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Fluxion.Data;

public class FluxionDbContext : DbContext
{
    public FluxionDbContext(DbContextOptions<FluxionDbContext> options) : base(options) { }

    public DbSet<KnowledgeNode> Nodes { get; set; }
    public DbSet<KnowledgeEdge> Edges { get; set; }
    public DbSet<LearnerProfile> Learners { get; set; }
    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<LearnerMastery> MasteryEntries { get; set; }
    public DbSet<SessionRecord> Sessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure KnowledgeNode Tags as a comma-separated string for SQLite
        modelBuilder.Entity<KnowledgeNode>()
            .Property(n => n.Tags)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

        // Configure LearnerProfile relationships
        modelBuilder.Entity<LearnerProfile>()
            .HasMany(l => l.MasteryEntries)
            .WithOne()
            .HasForeignKey(m => m.LearnerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LearnerProfile>()
            .HasMany(l => l.SessionHistory)
            .WithOne()
            .HasForeignKey(s => s.LearnerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.LearnerProfile)
            .WithOne()
            .HasForeignKey<ApplicationUser>(u => u.LearnerProfileId);
            
        // Configure unique constraint for LearnerMastery (one score per node per learner)
        modelBuilder.Entity<LearnerMastery>()
            .HasIndex(m => new { m.LearnerProfileId, m.NodeId })
            .IsUnique();
    }
}
