namespace Api.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    string Priority,
    string Status,
    Guid? CourseId,
    string? CourseName,
    Guid? ParentTaskId,
    string? ParentTaskTitle,
    long? CanvasAssignmentId,
    DateTimeOffset? CanvasUpdatedAt,
    string? CanvasWorkflowState,
    string? CanvasSubmissionState,
    bool? CanvasIsActive
);