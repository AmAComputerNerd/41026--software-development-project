using Api.Models;

namespace Api.DTOs;

public record TeacherDto(
    Guid UserId,
    EmploymentStatus EmploymentStatus,
    string CanvasApiKey
);