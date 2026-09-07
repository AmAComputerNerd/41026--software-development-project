using Database.Models;
using Student4.Contracts;

namespace Database.Extensions;

// Converts internal EF entities to the contract records shared with
// the public API. The contract records are what the API sees; the
// database service is the only place that touches the entity types.
public static class DtoExtensions
{
    public static UserRecord ToRecord(this User user)
    {
        return new UserRecord(
            Id: user.Id,
            Email: user.Email,
            PasswordHash: user.PasswordHash,
            FirstName: user.FirstName,
            MiddleNames: user.MiddleNames,
            LastName: user.LastName,
            Gender: user.Gender.ToString(),
            DateOfBirth: user.DateOfBirth,
            UserType: user.UserType.ToString(),
            UserProfile: user.UserProfile
        );
    }

    public static StudentRecord ToRecord(this Student student)
    {
        return new StudentRecord(
            UserId: student.UserId,
            CourseStatus: student.CourseStatus.ToString(),
            IsInternational: student.IsInternational,
            CanvasApiKey: student.CanvasApiKey
        );
    }

    public static TeacherRecord ToRecord(this Teacher teacher)
    {
        return new TeacherRecord(
            UserId: teacher.UserId,
            EmploymentStatus: teacher.EmploymentStatus.ToString(),
            CanvasApiKey: teacher.CanvasApiKey
        );
    }
}
