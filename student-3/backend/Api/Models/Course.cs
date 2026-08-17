namespace Api.Models;

public class Course
{
    public Guid Id { get; }
    public string Code { get; set; }
    public string Name { get; set; }
    public ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
}