using Api.Models;

namespace Api.DTOs;

// Body for PUT /api/students/{userId}. All fields are optional in the
// profile-edit UI, so we accept a partial DTO. The endpoint will only
// overwrite fields that are non-null in the request.
public record UpdateStudentRequestDto(
    CourseStatus? CourseStatus,
    bool? IsInternational,
    string? CanvasApiKey
);
