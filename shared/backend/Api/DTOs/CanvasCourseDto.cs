namespace Api.DTOs;

public record CanvasCourseDto(
    long Id,
    string Name,
    string? CourseCode,
    string WorkflowState
);
