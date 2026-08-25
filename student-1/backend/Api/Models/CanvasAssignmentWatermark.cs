namespace Api.Models;

public class CanvasAssignmentWatermark
{
    public Guid Id { get; }
    public long CanvasAssignmentId { get; set; }
    public DateTime? LastDueDate { get; set; }
    public string? LastWorkflowState { get; set; }
    public string? LastSubmissionState { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
}
