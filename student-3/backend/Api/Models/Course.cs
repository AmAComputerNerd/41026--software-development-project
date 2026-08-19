namespace Api.Models;

public class Course
{
    public Guid Id { get; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
}