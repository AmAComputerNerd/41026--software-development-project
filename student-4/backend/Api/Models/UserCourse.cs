namespace Api.Models;

public class UserCourse
{
    public Guid UserId { get; }
    public Guid CourseId { get; }

    public UserCourse(Guid userId, Guid courseId)
    {
        UserId = userId;
        CourseId = courseId;
    }
}
