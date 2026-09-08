using Api.DTOs;
using Student4.Contracts;

namespace Api.Extensions;

public static class DtoExtensions
{
    public static StudentDto ToDto(this StudentRecord record)
    {
        return new StudentDto(
            UserId: record.UserId,
            CourseStatus: record.CourseStatus,
            IsInternational: record.IsInternational,
            CanvasApiKey: record.CanvasApiKey
        );
    }

    public static TeacherDto ToDto(this TeacherRecord record)
    {
        return new TeacherDto(
            UserId: record.UserId,
            EmploymentStatus: record.EmploymentStatus,
            CanvasApiKey: record.CanvasApiKey
        );
    }

    public static UserDto ToDto(this UserRecord record)
    {
        return new UserDto
        {
            Id = record.Id,
            Email = record.Email,
            PasswordHash = record.PasswordHash,
            FirstName = record.FirstName,
            MiddleNames = record.MiddleNames,
            LastName = record.LastName,
            Gender = record.Gender,
            DateOfBirth = record.DateOfBirth,
            UserType = record.UserType,
            UserProfile = record.UserProfile,
        };
    }
}
