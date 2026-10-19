using System.Text.Json.Serialization;

namespace MultiAgentServer.Models;

public sealed record RunWorkflowRequest(
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("studentId")] Guid? StudentId,
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("evidenceSnapshot")] string? EvidenceSnapshot = null);

public sealed record HumanReviewRequest(
    [property: JsonPropertyName("decision")] string Decision,
    [property: JsonPropertyName("reviewerName")] string ReviewerName,
    [property: JsonPropertyName("comments")] string? Comments = null,
    [property: JsonPropertyName("correctedOutput")] string? CorrectedOutput = null);

public sealed record PlannerStageOutput(
    [property: JsonPropertyName("objective")] string Objective,
    [property: JsonPropertyName("requiredEvidence")] IReadOnlyList<string> RequiredEvidence,
    [property: JsonPropertyName("steps")] IReadOnlyList<string> Steps,
    [property: JsonPropertyName("completedAtUtc")] DateTime CompletedAtUtc);

public sealed record WorkerStageOutput(
    [property: JsonPropertyName("evidenceSummary")] string EvidenceSummary,
    [property: JsonPropertyName("draftOutput")] string DraftOutput,
    [property: JsonPropertyName("recommendedActions")] IReadOnlyList<string> RecommendedActions,
    [property: JsonPropertyName("citations")] IReadOnlyList<string> Citations,
    [property: JsonPropertyName("completedAtUtc")] DateTime CompletedAtUtc);

public sealed record ReviewerStageOutput(
    [property: JsonPropertyName("verdict")] string Verdict,
    [property: JsonPropertyName("confidence")] string Confidence,
    [property: JsonPropertyName("critique")] string Critique,
    [property: JsonPropertyName("identifiedRisks")] IReadOnlyList<string> IdentifiedRisks,
    [property: JsonPropertyName("recommendations")] IReadOnlyList<string> Recommendations,
    [property: JsonPropertyName("completedAtUtc")] DateTime CompletedAtUtc);

public sealed record HumanReviewStageOutput(
    [property: JsonPropertyName("decision")] string Decision,
    [property: JsonPropertyName("reviewerName")] string ReviewerName,
    [property: JsonPropertyName("comments")] string Comments,
    [property: JsonPropertyName("finalOutput")] string FinalOutput,
    [property: JsonPropertyName("reviewedAtUtc")] DateTime ReviewedAtUtc);

public sealed record CoordinationAuditEntry(
    [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
    [property: JsonPropertyName("agent")] string Agent,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("summary")] string Summary);

public sealed record MultiAgentWorkflowRecord(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("studentId")] Guid StudentId,
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
    [property: JsonPropertyName("updatedAtUtc")] DateTime UpdatedAtUtc,
    [property: JsonPropertyName("planner")] PlannerStageOutput Planner,
    [property: JsonPropertyName("worker")] WorkerStageOutput Worker,
    [property: JsonPropertyName("reviewer")] ReviewerStageOutput Reviewer,
    [property: JsonPropertyName("humanReview")] HumanReviewStageOutput? HumanReview,
    [property: JsonPropertyName("auditLog")] IReadOnlyList<CoordinationAuditEntry> AuditLog);
