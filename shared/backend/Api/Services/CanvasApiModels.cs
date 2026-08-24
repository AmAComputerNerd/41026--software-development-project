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
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("due_at")] DateTimeOffset? DueAt,
    [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt,
    [property: JsonPropertyName("workflow_state")] string WorkflowState,
    [property: JsonPropertyName("published")] bool Published,
    [property: JsonPropertyName("submission")] CanvasSubmissionResponse? Submission
);

internal sealed record CanvasSubmissionResponse(
    [property: JsonPropertyName("workflow_state")] string WorkflowState,
    [property: JsonPropertyName("submitted_at")] DateTimeOffset? SubmittedAt,
    [property: JsonPropertyName("late")] bool Late,
    [property: JsonPropertyName("missing")] bool Missing
);
