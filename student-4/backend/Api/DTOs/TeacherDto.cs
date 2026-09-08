namespace Api.DTOs;

public record TeacherDto(
    Guid UserId,
    string EmploymentStatus,
    string CanvasApiKey
);