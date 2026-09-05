using Api.DTOs;
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Extensions;

public static class DtoExtensions
{
    public static StudentDto ToDto(this Student student)
    {
        return new StudentDto(
            UserId: student.UserId,
            CourseStatus: student.CourseStatus,
            IsInternational: student.IsInternational,
            CanvasApiKey: student.CanvasApiKey
        );
    }

    public static TeacherDto ToDto(this Teacher teacher)
    {
        return new TeacherDto(
            UserId: teacher.UserId,
            EmploymentStatus: teacher.EmploymentStatus,
            CanvasApiKey: teacher.CanvasApiKey
        );
    }

    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            FirstName = user.FirstName,
            MiddleNames = user.MiddleNames,
            LastName = user.LastName,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            UserType = user.UserType,
            UserProfile = user.UserProfile,
        };
    }

    public static UserCourseDto ToDto(this UserCourse userCourse)
    {
        return new UserCourseDto(
            UserId: userCourse.UserId,
            CourseId: userCourse.CourseId
        );
    }
}
