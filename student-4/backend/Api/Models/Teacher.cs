namespace Api.Models;

public class Teacher
{
    public Guid UserId { get; }
    public required EmploymentStatus EmploymentStatus { get; set; }
    public required string CanvasApiKey { get; set; }

    public Teacher(Guid userId)
    {
        UserId = userId;
    }
}

public enum EmploymentStatus
{
    FullTime,
    PartTime,
    Inactive,
}