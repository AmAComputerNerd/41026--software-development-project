namespace Api.DTOs;

// Wrapper DTO for POST /api/users that bundles the base user fields with
// the optional role-specific student/teacher DTOs.
public record CreateUserRequestDto(
    string Email,
    string PasswordHash,
    string FirstName,
    string? MiddleNames,
    string LastName,
    string Gender,
    DateTime DateOfBirth,
    string UserType,
    StudentDto? StudentDto,
    TeacherDto? TeacherDto
);
