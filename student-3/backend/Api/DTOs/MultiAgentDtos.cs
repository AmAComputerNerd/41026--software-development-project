namespace Api.DTOs;

public sealed record MultiAgentReviewRequestDto(
    string Decision,
    string ReviewerName,
    string Comments,
    string? CorrectedOutput = null);

public sealed record MultiAgentPlannerDto(
    string Objective,
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<string> Steps,
    DateTime CompletedAtUtc);

public sealed record MultiAgentWorkerDto(
    string EvidenceSummary,
    string DraftOutput,
    IReadOnlyList<string> RecommendedActions,
    IReadOnlyList<string> Citations,
    DateTime CompletedAtUtc);

public sealed record MultiAgentReviewerDto(
    string Verdict,
    string Confidence,
    string Critique,
    IReadOnlyList<string> IdentifiedRisks,
    IReadOnlyList<string> Recommendations,
    DateTime CompletedAtUtc);

public sealed record MultiAgentHumanReviewDto(
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
    MultiAgentPlannerDto Planner,
    MultiAgentWorkerDto Worker,
    MultiAgentReviewerDto Reviewer,
    MultiAgentHumanReviewDto? HumanReview,
    IReadOnlyList<MultiAgentAuditEntryDto> AuditLog);
