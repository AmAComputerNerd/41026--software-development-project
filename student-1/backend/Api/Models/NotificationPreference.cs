namespace Api.Models;

public class NotificationPreference
{
    public Guid Id { get; }
    public Guid StudentId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public bool Enabled { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public enum NotificationChannel
{
    InApp,
    Email
}
