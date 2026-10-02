namespace Api.DTOs;

public sealed record AutomationHealthRequestDto(int Days = 30);

public sealed record AutomationHealthMetricsDto(
    int WindowDays,
    int EnabledAutomations,
    int TotalRuns,
    int SuccessfulRuns,
    int FailedRuns,
    int RunningRuns,
    double? SuccessRatePercent,
    DateTime? LastRunAt,
    DateTime GeneratedAt);

public sealed record AutomationHealthReportDto(
    string Health,
    string Summary,
    AutomationHealthMetricsDto Metrics);

public sealed record McpToolErrorDto(string Code, string Message);

public sealed record McpAutomationResultDto(
    string Status,
    string Tool,
    AutomationHealthReportDto? Data,
    McpToolErrorDto? Error);

public sealed record RagQuestionRequestDto(string Question);

public sealed record RagAnswerRequestDto(string Question, string Scope);

public sealed class RagAnswerResponseDto
{
    public string Status { get; init; } = "insufficient_context";
    public string Answer { get; init; } = string.Empty;
    public string Confidence { get; init; } = "insufficient";
    public IReadOnlyList<RagCitationDto> Citations { get; init; } = [];
    public RagRetrievalSummaryDto Retrieval { get; init; } = new();
}

public sealed class RagCitationDto
{
    public string SourceId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Heading { get; init; } = string.Empty;
    public double Score { get; init; }
}

public sealed class RagRetrievalSummaryDto
{
    public int MatchedChunks { get; init; }
    public int ConsideredChunks { get; init; }
}