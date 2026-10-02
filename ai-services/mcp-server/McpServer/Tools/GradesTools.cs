using System.ComponentModel;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public sealed class GradesTools(IGradesClient gradesClient)
{
    [McpServerTool(Name = "grades_list_weightings", UseStructuredContent = true)]
    [Description("Lists grade weightings for a student above specified number.")]
    public async Task<GradeWeightingsToolResult> ListWeightingsAsync(
        [Description("The weight to filter by.")] double weight = 0.4,
        [Description("The maximum number of weightings to return.")] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (weight is < 0.01 or > 1)
        {
            return GradeWeightingsToolResult.Invalid(
                "invalid_weight",
                "weight must be between 0.01 and 1.");
        }

        if (limit is < 1 or > 10)
        {
            return GradeWeightingsToolResult.Invalid(
                "invalid_limit",
                "limit must be between 1 and 10.");
        }

        try
        {
            var weightings = await gradesClient.GetGradesWeightingsAsync(
                weight,
                limit,
                cancellationToken);
            return GradeWeightingsToolResult.Success(weightings);
        }
        catch (HttpRequestException)
        {
            return GradeWeightingsToolResult.Failure(
                "student5_unavailable",
                "The Student 5 grade weightings service could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return GradeWeightingsToolResult.Failure(
                "student5_timeout",
                "The Student 5 grade weightings service timed out.");
        }
        catch (GradeClientException)
        {
            return GradeWeightingsToolResult.Failure(
                "student5_invalid_response",
                "The Student 5 grade weightings service returned an invalid response.");
        }
    }
}

public sealed record GradeWeightingsToolResult(
    string Status,
    string Tool,
    GradeWeightingsData? Data,
    GradeWeightingsToolError? Error)
{
    public static GradeWeightingsToolResult Success(GradeWeightingsData data) =>
        new("success", "grades_list_weightings", data, null);

    public static GradeWeightingsToolResult Invalid(string code, string message) =>
        new("error", "grades_list_weightings", null, new(code, message));

    public static GradeWeightingsToolResult Failure(string code, string message) =>
        new("error", "grades_list_weightings", null, new(code, message));
}

public sealed record GradeWeightingsToolError(string Code, string Message);
