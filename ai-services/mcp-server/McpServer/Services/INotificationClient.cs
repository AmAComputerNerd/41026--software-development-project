namespace McpServer.Services;

public interface INotificationClient
{
    Task<BroadcastNotificationResultData> BroadcastNotificationAsync(
        Guid studentId,
        string title,
        string message,
        string urgency,
        CancellationToken cancellationToken = default);
}

public sealed record BroadcastNotificationResultData(
    Guid NotificationId,
    Guid StudentId,
    string Title,
    string Message,
    string Urgency,
    string Source,
    DateTime DeliveredAtUtc);
