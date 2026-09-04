using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class ScheduledPostAutomationConfiguration
    : IEntityTypeConfiguration<ScheduledPostAutomation>
{
    public void Configure(EntityTypeBuilder<ScheduledPostAutomation> builder)
    {
        builder.ToTable("ScheduledPostAutomations");
        builder.Property(automation => automation.ContextCode).HasMaxLength(100);
        builder.Property(automation => automation.Subject).HasMaxLength(255);
        builder.Property(automation => automation.Body).HasMaxLength(10000);
        builder.Property(automation => automation.PostTime).HasConversion<UtcDateTimeConverter>();
    }
}

public sealed class ScheduledPostAutomationRunConfiguration
    : IEntityTypeConfiguration<ScheduledPostAutomationRun>
{
    public void Configure(EntityTypeBuilder<ScheduledPostAutomationRun> builder)
    {
        builder.ToTable("ScheduledPostAutomationRuns");
        builder.Property(run => run.PostTime).HasConversion<UtcDateTimeConverter>();
        builder.Property(run => run.ContextCode).HasMaxLength(100);
        builder.Property(run => run.Subject).HasMaxLength(255);
        builder.Property(run => run.Body).HasMaxLength(10000);
    }
}