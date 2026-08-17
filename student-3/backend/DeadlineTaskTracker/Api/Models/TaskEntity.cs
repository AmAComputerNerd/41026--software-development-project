namespace Api.Models;

public class TaskEntity
{
    public Guid Id { get; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }
    
    public Guid? ParentTaskId { get; set; }
    public TaskEntity? ParentTask { get; set; }

    public ICollection<TaskEntity> ChildrenTasks { get; set; } = new List<TaskEntity>();
}

public enum TaskPriority
{
    Low,
    Medium,
    High
}

public enum TaskStatus
{
    Todo,
    InProgress,
    Completed
}