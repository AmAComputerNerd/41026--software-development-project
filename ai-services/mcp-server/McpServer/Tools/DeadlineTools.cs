using System.ComponentModel;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public sealed class DeadlineTools(IDeadlineClient deadlineClient)
{
    [McpServerTool(Name = "deadlines_list_upcoming", UseStructuredContent = true)]
    [Description("Lists incomplete Student 3 tasks due within a bounded number of days.")]
    public async Task<DeadlineToolResult> ListUpcomingAsync(
        [Description("Number of days to look ahead, from 1 to 90.")] int days = 7,
        [Description("Maximum number of tasks to return, from 1 to 50.")] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (days is < 1 or > 90)
        {
            return DeadlineToolResult.Invalid(
                "invalid_days",
                "days must be between 1 and 90.");
        }

        if (limit is < 1 or > 50)
        {
            return DeadlineToolResult.Invalid(
                "invalid_limit",
                "limit must be between 1 and 50.");
        }

        try
        {
            var deadlines = await deadlineClient.GetUpcomingDeadlinesAsync(
                days,
                limit,
                cancellationToken);
            return DeadlineToolResult.Success(deadlines);
        }
        catch (HttpRequestException)
        {
            return DeadlineToolResult.Failure(
                "student3_unavailable",
                "The Student 3 deadline service could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DeadlineToolResult.Failure(
                "student3_timeout",
                "The Student 3 deadline service timed out.");
        }
        catch (DeadlineClientException)
        {
            return DeadlineToolResult.Failure(
                "student3_invalid_response",
                "The Student 3 deadline service returned an invalid response.");
        }
    }
}

public sealed record DeadlineToolResult(
    string Status,
    string Tool,
    UpcomingDeadlinesData? Data,
    DeadlineToolError? Error)
{
    public static DeadlineToolResult Success(UpcomingDeadlinesData data) =>
        new("success", "deadlines_list_upcoming", data, null);

    public static DeadlineToolResult Invalid(string code, string message) =>
        new("error", "deadlines_list_upcoming", null, new(code, message));

    public static DeadlineToolResult Failure(string code, string message) =>
        new("error", "deadlines_list_upcoming", null, new(code, message));
}

public sealed record DeadlineToolError(string Code, string Message);
