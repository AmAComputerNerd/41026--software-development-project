namespace Api.DTOs;

public record SharedCanvasCourseDto(
    long Id,
    string Name,
    string? CourseCode,
    string WorkflowState
);
