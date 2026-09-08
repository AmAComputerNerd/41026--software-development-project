namespace Api.DTOs;

public record UpdateStudentRequestDto(
    string? CourseStatus,
    bool? IsInternational,
    string? CanvasApiKey
);
