namespace Api.DTOs;

public record CourseDto(
    Guid Id,
    string Code,
    string Name,
    long? CanvasCourseId,
    string? CanvasWorkflowState,
    bool? CanvasIsActive,
    DateTimeOffset? LastCanvasSyncAt
);