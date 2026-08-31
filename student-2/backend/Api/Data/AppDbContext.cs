using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter =
        new(value => ToUtc(value), value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public DbSet<Automation> Automations { get; set; } = null!;
    public DbSet<AssignmentExtensionAutomation> AssignmentExtensionAutomations { get; set; } = null!;
    public DbSet<ScheduledPostAutomation> ScheduledPostAutomations { get; set; } = null!;
    public DbSet<AutomationRun> AutomationRuns { get; set; } = null!;
    public DbSet<AssignmentExtensionAutomationRun> AssignmentExtensionAutomationRuns { get; set; } = null!;
    public DbSet<ScheduledPostAutomationRun> ScheduledPostAutomationRuns { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Automation>().ToTable("Automations");
        modelBuilder.Entity<AssignmentExtensionAutomation>().ToTable("AssignmentExtensionAutomations");
        modelBuilder.Entity<ScheduledPostAutomation>().ToTable("ScheduledPostAutomations");
        modelBuilder.Entity<AutomationRun>().ToTable("AutomationRuns");
        modelBuilder.Entity<AssignmentExtensionAutomationRun>().ToTable("AssignmentExtensionAutomationRuns");
        modelBuilder.Entity<ScheduledPostAutomationRun>().ToTable("ScheduledPostAutomationRuns");

        modelBuilder.Entity<Automation>().HasKey(automation => automation.Id);
        modelBuilder.Entity<AutomationRun>().HasKey(run => run.Id);

        modelBuilder.Entity<AutomationRun>()
            .HasOne(run => run.Automation)
            .WithMany(automation => automation.Runs)
            .HasForeignKey(run => run.AutomationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Automation>()
            .HasIndex(automation => new { automation.StudentId, automation.Deleted });
        modelBuilder.Entity<AutomationRun>()
            .HasIndex(run => new { run.AutomationId, run.ExecutionTimeStamp });

        modelBuilder.Entity<AssignmentExtensionAutomation>()
            .Property(automation => automation.Reason)
            .HasMaxLength(500);
        modelBuilder.Entity<AssignmentExtensionAutomation>()
            .Property(automation => automation.FurtherDetails)
            .HasMaxLength(2000);
        modelBuilder.Entity<ScheduledPostAutomation>()
            .Property(automation => automation.Subject)
            .HasMaxLength(200);
        modelBuilder.Entity<ScheduledPostAutomation>()
            .Property(automation => automation.Body)
            .HasMaxLength(10000);

        modelBuilder.Entity<ScheduledPostAutomation>()
            .Property(automation => automation.PostTime)
            .HasConversion(UtcDateTimeConverter);
        modelBuilder.Entity<AutomationRun>()
            .Property(run => run.ExecutionTimeStamp)
            .HasConversion(UtcDateTimeConverter);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}