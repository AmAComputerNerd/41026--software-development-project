namespace Api.DTOs;

public record StudentDto(
    Guid UserId,
    string CourseStatus,
    bool IsInternational,
    string CanvasApiKey
);