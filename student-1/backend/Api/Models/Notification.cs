namespace Api.Models;

public class Notification
{
    public Guid Id { get; }
    public Guid StudentId { get; set; }
    public NotificationType Type { get; set; }
    public required string SourceMicroservice { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public enum NotificationType
{
    Deadline,
    Grade,
    Automation,
    Account,
    AI
}
