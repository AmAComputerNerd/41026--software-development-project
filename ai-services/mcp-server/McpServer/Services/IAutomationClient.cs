namespace McpServer.Services;

public interface IAutomationClient
{
    Task<AutomationHealthMetrics> GetAutomationHealthAsync(
        int days,
        CancellationToken cancellationToken);
}

public sealed record AutomationHealthMetrics(
    int WindowDays,
    int EnabledAutomations,
    int TotalRuns,
    int SuccessfulRuns,
    int FailedRuns,
    int RunningRuns,
    double? SuccessRatePercent,
    DateTime? LastRunAt,
    DateTime GeneratedAt);