namespace Api.DTOs;

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
