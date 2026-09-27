using System.Net.Http.Json;
using System.Text.Json;

namespace McpServer.Services;

public sealed class NotificationClient(HttpClient httpClient) : INotificationClient
{
    private sealed record PushNotificationPayload(
        Guid StudentId,
        string Type,
        string SourceMicroservice,
        string Message,
        string? RelatedEntityType = null,
        Guid? RelatedEntityId = null,
        string? ActionPayload = null);

    private sealed record CreatedNotificationResponse(
        Guid Id,
        Guid StudentId,
        string Type,
        string SourceMicroservice,
        string Message,
        bool IsRead,
        DateTime CreatedAtUtc);

    public async Task<BroadcastNotificationResultData> BroadcastNotificationAsync(
        Guid studentId,
        string title,
        string message,
        string urgency,
        CancellationToken cancellationToken = default)
    {
        var formattedMessage = $"[{urgency.ToUpperInvariant()}] {title}: {message}";
        var payload = new PushNotificationPayload(
            StudentId: studentId,
            Type: "AI",
            SourceMicroservice: "mcp-server",
            Message: formattedMessage,
            RelatedEntityType: "McpBroadcast");

        using var response = await httpClient.PostAsJsonAsync(
            "notifications/push",
            payload,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NotificationClientException(
                $"Student 1 returned HTTP {(int)response.StatusCode}: {errorBody}");
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedNotificationResponse>(
            cancellationToken: cancellationToken)
            ?? throw new NotificationClientException("Student 1 returned an empty notification payload.");

        return new BroadcastNotificationResultData(
            NotificationId: created.Id,
            StudentId: created.StudentId,
            Title: title,
            Message: message,
            Urgency: urgency,
            Source: "mcp-server",
            DeliveredAtUtc: created.CreatedAtUtc);
    }
}

public sealed class NotificationClientException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);
