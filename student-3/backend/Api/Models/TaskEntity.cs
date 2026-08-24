namespace Api.Models;

public class TaskEntity
{
    public Guid Id { get; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? CanvasAssignmentId { get; set; }
    public DateTime? CanvasUpdatedAt { get; set; }
    public string? CanvasWorkflowState { get; set; }
    public string? CanvasSubmissionState { get; set; }
    public bool? CanvasIsActive { get; set; }

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