namespace Api.Models;

public abstract class Automation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public bool Enabled { get; set; }
    public bool Deleted { get; set; }
    public ICollection<AutomationRun> Runs { get; set; } = [];
}

public class AssignmentExtensionAutomation : Automation
{
    public int BufferMinutes { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string FurtherDetails { get; set; } = string.Empty;
}

public class ScheduledPostAutomation : Automation
{
    public DateTime PostTime { get; set; }
    public string Recipients { get; set; } = "[]";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}