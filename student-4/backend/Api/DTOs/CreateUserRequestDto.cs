using Api.Models;

namespace Api.DTOs;

// Wrapper DTO for POST /api/users that bundles the base user fields with
// the optional role-specific student/teacher DTOs. This is needed because
// ASP.NET minimal API parameter binding cannot reliably split a single
// JSON body across multiple optional parameters in the endpoint signature.
public record CreateUserRequestDto(
    string Email,
    string PasswordHash,
    string FirstName,
    string? MiddleNames,
    string LastName,
    Gender Gender,
    DateTime DateOfBirth,
    UserType UserType,
    StudentDto? StudentDto,
    TeacherDto? TeacherDto
);
