namespace Api.DTOs;

public sealed record StartMultiAgentWorkflowRequestDto(
    Guid? StudentId,
    string Goal);

public sealed record SubmitHumanReviewRequestDto(
    string Decision,
    string ReviewerName,
    string? Comments = null,
    string? CorrectedOutput = null);

public sealed record MultiAgentPlannerStageDto(
    string Objective,
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<string> Steps,
    DateTime CompletedAtUtc);

public sealed record MultiAgentWorkerStageDto(
    string EvidenceSummary,
    string DraftOutput,
    IReadOnlyList<string> RecommendedActions,
    IReadOnlyList<string> Citations,
    DateTime CompletedAtUtc);

public sealed record MultiAgentReviewerStageDto(
    string Verdict,
    string Confidence,
    string Critique,
    IReadOnlyList<string> IdentifiedRisks,
    IReadOnlyList<string> Recommendations,
    DateTime CompletedAtUtc);

public sealed record MultiAgentHumanReviewStageDto(
    string Decision,
    string ReviewerName,
    string Comments,
    string FinalOutput,
    DateTime ReviewedAtUtc);

public sealed record MultiAgentAuditEntryDto(
    DateTime TimestampUtc,
    string Agent,
    string Action,
    string Summary);

public sealed record MultiAgentWorkflowDto(
    Guid Id,
    string Owner,
    Guid StudentId,
    string Goal,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    MultiAgentPlannerStageDto Planner,
    MultiAgentWorkerStageDto Worker,
    MultiAgentReviewerStageDto Reviewer,
    MultiAgentHumanReviewStageDto? HumanReview,
    IReadOnlyList<MultiAgentAuditEntryDto> AuditLog);
