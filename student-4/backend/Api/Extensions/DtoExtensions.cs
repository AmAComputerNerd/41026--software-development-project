using Api.DTOs;
using Student4.Contracts;

namespace Api.Extensions;

// Converts contract records (returned by IDatabaseClient) to the
// public DTOs the API exposes. This is the boundary between the
// internal persistence shape and the public contract — anything
// crossing this line gets a fresh DTO with [JsonIgnore]s applied.
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
