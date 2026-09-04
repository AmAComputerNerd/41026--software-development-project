using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class AssignmentExtensionAutomationConfiguration
    : IEntityTypeConfiguration<AssignmentExtensionAutomation>
{
    public void Configure(EntityTypeBuilder<AssignmentExtensionAutomation> builder)
    {
        builder.ToTable("AssignmentExtensionAutomations");
        builder.HasIndex(automation => automation.SubjectId);
        builder.Property(automation => automation.Reason).HasConversion<string>().HasMaxLength(3);
        builder.Property(automation => automation.FurtherDetails).HasMaxLength(2000);
    }
}

public sealed class AssignmentExtensionAutomationRunConfiguration
    : IEntityTypeConfiguration<AssignmentExtensionAutomationRun>
{
    public void Configure(EntityTypeBuilder<AssignmentExtensionAutomationRun> builder)
    {
        builder.ToTable("AssignmentExtensionAutomationRuns");
    }
}