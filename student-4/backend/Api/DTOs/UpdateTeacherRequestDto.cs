namespace Api.DTOs;

public record UpdateTeacherRequestDto(
    string? EmploymentStatus,
    string? CanvasApiKey
);
