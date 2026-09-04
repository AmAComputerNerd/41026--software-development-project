using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Automation> Automations { get; set; } = null!;
    public DbSet<AutomationRun> AutomationRuns { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Automation>().ToTable("Automations");
        modelBuilder.Entity<AutomationRun>().ToTable("AutomationRuns");

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
        modelBuilder.Entity<AutomationRun>()
            .HasIndex(run => new { run.AutomationId, run.ExecutionKey })
            .IsUnique();

        modelBuilder.Entity<AutomationRun>()
            .Property(run => run.ExecutionKey)
            .HasMaxLength(100);
        modelBuilder.Entity<AutomationRun>()
            .Property(run => run.ExecutionTimeStamp)
            .HasConversion<UtcDateTimeConverter>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}