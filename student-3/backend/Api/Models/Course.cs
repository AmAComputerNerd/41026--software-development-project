namespace Api.Models;

public class Course
{
    public Guid Id { get; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long? CanvasCourseId { get; set; }
    public string? CanvasWorkflowState { get; set; }
    public bool? CanvasIsActive { get; set; }
    public DateTime? LastCanvasSyncAt { get; set; }
    public ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
}