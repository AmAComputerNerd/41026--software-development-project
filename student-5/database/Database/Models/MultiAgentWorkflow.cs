namespace GradesManager.Models;

public sealed class MultiAgentWorkflow
{
    public Guid WorkflowId { get; set; }
    public Guid StudentId { get; set; }
    public required string Goal { get; set; }
    public required string ResultJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Revision { get; set; }
}