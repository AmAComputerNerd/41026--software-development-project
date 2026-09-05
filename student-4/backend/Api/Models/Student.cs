namespace Api.Models;

public class Student
{
    public Guid UserId { get; }
    public required CourseStatus CourseStatus { get; set; }
    public bool IsInternational { get; set; }
    public required string CanvasApiKey { get; set; }

    public Student(Guid userId)
    {
        UserId = userId;
    }
}

public enum CourseStatus
{
    FullTime,
    PartTime,
    Inactive,
}