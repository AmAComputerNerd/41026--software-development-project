namespace Database.Models;

public sealed class BreakdownApplication
{
    public Guid WorkflowId { get; init; }
    public Guid ParentTaskId { get; init; }
    public required string Fingerprint { get; init; }
    public required string ResultJson { get; init; }
}
