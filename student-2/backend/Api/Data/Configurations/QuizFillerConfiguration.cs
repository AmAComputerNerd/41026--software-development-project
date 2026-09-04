using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class QuizFillerAutomationConfiguration
    : IEntityTypeConfiguration<QuizFillerAutomation>
{
    public void Configure(EntityTypeBuilder<QuizFillerAutomation> builder)
    {
        builder.ToTable("QuizFillerAutomations");
        builder.HasIndex(automation => automation.SubjectId);
    }
}

public sealed class QuizFillerAutomationRunConfiguration
    : IEntityTypeConfiguration<QuizFillerAutomationRun>
{
    public void Configure(EntityTypeBuilder<QuizFillerAutomationRun> builder)
    {
        builder.ToTable("QuizFillerAutomationRuns");
        builder.Property(run => run.QuizTitle).HasMaxLength(255);
    }
}
