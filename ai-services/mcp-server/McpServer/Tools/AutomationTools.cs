using System.ComponentModel;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public sealed class AutomationTools(IAutomationClient automationClient)
{
    [McpServerTool(Name = "automations_review_health", UseStructuredContent = true)]
    [Description("Reviews recent Student 2 automation executions and returns an actionable health summary.")]
    public async Task<AutomationToolResult> ReviewHealthAsync(
        [Description("Number of recent days to review, from 1 to 90.")] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days is < 1 or > 90)
        {
            return AutomationToolResult.Failure(
                "invalid_days",
                "days must be between 1 and 90.");
        }

        try
        {
            var metrics = await automationClient.GetAutomationHealthAsync(
                days,
                cancellationToken);
            return AutomationToolResult.Success(CreateReport(metrics));
        }
        catch (HttpRequestException)
        {
            return AutomationToolResult.Failure(
                "student2_unavailable",
                "The Student 2 automation service could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AutomationToolResult.Failure(
                "student2_timeout",
                "The Student 2 automation service timed out.");
        }
        catch (AutomationClientException)
        {
            return AutomationToolResult.Failure(
                "student2_invalid_response",
                "The Student 2 automation service returned an invalid response.");
        }
    }

    private static AutomationHealthReport CreateReport(AutomationHealthMetrics metrics)
    {
        if (metrics.TotalRuns == 0)
        {
            return new AutomationHealthReport(
                "no_activity",
                $"{metrics.EnabledAutomations} automation(s) are enabled, but none ran in the last {metrics.WindowDays} days.",
                metrics);
        }

        if (metrics.FailedRuns > 0)
        {
            return new AutomationHealthReport(
                "attention",
                $"{metrics.FailedRuns} of {metrics.SuccessfulRuns + metrics.FailedRuns} completed run(s) failed in the last {metrics.WindowDays} days. Review run history before relying on the affected automations.",
                metrics);
        }

        if (metrics.RunningRuns > 0)
        {
            return new AutomationHealthReport(
                "in_progress",
                $"All completed runs succeeded, with {metrics.RunningRuns} run(s) still in progress.",
                metrics);
        }

        return new AutomationHealthReport(
            "healthy",
            $"All {metrics.SuccessfulRuns} run(s) completed successfully in the last {metrics.WindowDays} days.",
            metrics);
    }
}

public sealed record AutomationToolResult(
    string Status,
    string Tool,
    AutomationHealthReport? Data,
    AutomationToolError? Error)
{
    public static AutomationToolResult Success(AutomationHealthReport data) =>
        new("success", "automations_review_health", data, null);

    public static AutomationToolResult Failure(string code, string message) =>
        new("error", "automations_review_health", null, new(code, message));
}

public sealed record AutomationHealthReport(
    string Health,
    string Summary,
    AutomationHealthMetrics Metrics);

public sealed record AutomationToolError(string Code, string Message);