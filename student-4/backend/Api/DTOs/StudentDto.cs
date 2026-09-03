using Api.Models;

namespace Api.DTOs;

public record StudentDto(
    Guid UserId,
    CourseStatus CourseStatus,
    bool IsInternational,
    string CanvasApiKey
);