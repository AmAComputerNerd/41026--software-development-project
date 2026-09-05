namespace Api.Configuration;

public sealed class AutomationExecutionOptions
{
    public const string SectionName = "AutomationExecution";

    public int IntervalSeconds { get; init; } = 30;
}