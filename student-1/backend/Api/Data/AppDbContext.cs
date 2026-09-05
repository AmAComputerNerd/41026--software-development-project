using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<NotificationPreference> NotificationPreferences { get; set; } = null!;
    public DbSet<AiDigest> AiDigests { get; set; } = null!;
    public DbSet<CanvasAssignmentWatermark> CanvasAssignmentWatermarks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>()
            .HasKey(n => n.Id);

        modelBuilder.Entity<NotificationPreference>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<AiDigest>()
            .HasKey(d => d.Id);

        modelBuilder.Entity<CanvasAssignmentWatermark>()
            .HasKey(w => w.Id);

        modelBuilder.Entity<CanvasAssignmentWatermark>()
            .HasIndex(w => w.CanvasAssignmentId)
            .IsUnique();

        modelBuilder.Entity<Notification>()
            .Property(n => n.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.RelatedEntityType, n.RelatedEntityId });

        modelBuilder.Entity<NotificationPreference>()
            .Property(p => p.Type)
            .HasConversion<string>();

        modelBuilder.Entity<NotificationPreference>()
            .Property(p => p.Channel)
            .HasConversion<string>();
    }
}
