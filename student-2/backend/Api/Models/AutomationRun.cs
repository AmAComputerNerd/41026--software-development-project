namespace Api.Models;

public abstract class AutomationRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AutomationId { get; set; }
    public DateTime ExecutionTimeStamp { get; set; }
    public string Result { get; set; } = string.Empty;
    public Automation Automation { get; set; } = null!;
}

public class AssignmentExtensionAutomationRun : AutomationRun
{
    public string AssignmentId { get; set; } = string.Empty;
}

public class ScheduledPostAutomationRun : AutomationRun
{
    public string Recipients { get; set; } = "[]";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}