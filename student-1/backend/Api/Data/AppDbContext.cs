using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<NotificationPreference> NotificationPreferences { get; set; } = null!;
    public DbSet<AiDigest> AiDigests { get; set; } = null!;
    public DbSet<CanvasAssignmentWatermark> CanvasAssignmentWatermarks { get; set; } = null!;
    public DbSet<ChatSession> ChatSessions { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>()
            .HasKey(n => n.Id);

        modelBuilder.Entity<NotificationPreference>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<AiDigest>()
            .HasKey(d => d.Id);

        modelBuilder.Entity<ChatSession>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<ChatSession>()
            .HasIndex(s => new { s.StudentId, s.UpdatedAtUtc });

        modelBuilder.Entity<ChatMessage>()
            .HasKey(m => m.Id);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.ChatSession)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => new { m.ChatSessionId, m.CreatedAtUtc });

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
