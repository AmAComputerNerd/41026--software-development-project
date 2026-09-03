using System.Text.Json.Serialization;

namespace Api.Services;

internal sealed record CanvasCourseResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("course_code")] string? CourseCode,
    [property: JsonPropertyName("workflow_state")] string WorkflowState
);

internal sealed record CanvasAssignmentResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("course_id")] long CourseId,
    [property: JsonPropertyName("assignment_group_id")] long AssignmentGroupId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("due_at")] DateTime? DueAt,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt,
    [property: JsonPropertyName("workflow_state")] string WorkflowState,
    [property: JsonPropertyName("published")] bool Published,
    [property: JsonPropertyName("points_possible")] double? MaxMarks,
    [property: JsonPropertyName("submission")] CanvasSubmissionResponse? Submission
);

internal sealed record CanvasAssignmentGroupResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("group_weight")] double Weight
);

internal sealed record CanvasSubmissionResponse(
    [property: JsonPropertyName("score")] double? FinalMark,
    [property: JsonPropertyName("workflow_state")] string WorkflowState,
    [property: JsonPropertyName("submitted_at")] DateTime? SubmittedAt,
    [property: JsonPropertyName("late")] bool Late,
    [property: JsonPropertyName("missing")] bool Missing
);

internal sealed record CanvasUserResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("sis_user_id")] string? SisUserId,
    [property: JsonPropertyName("login_id")] string? LoginId
);

// Canvas' /api/v1/users/self response only documents the subset we rely on.
internal sealed record CanvasSelfUserResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("sis_user_id")] string? SisUserId,
    [property: JsonPropertyName("login_id")] string? LoginId
);
