using System.ComponentModel;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public sealed class NotificationTools(INotificationClient notificationClient)
{
    private static readonly HashSet<string> ValidUrgencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low", "Medium", "High", "Critical"
    };

    [McpServerTool(Name = "notifications_broadcast_alert", UseStructuredContent = true)]
    [Description("Broadcasts a validated course or system alert to a target student via Student 1 NotificationService.")]
    public async Task<NotificationToolResult> BroadcastAlertAsync(
        [Description("Target student GUID.")] string studentId,
        [Description("Alert title (3-100 characters).")] string title,
        [Description("Alert message content (5-500 characters).")] string message,
        [Description("Urgency level: Low, Medium, High, or Critical.")] string urgency = "Medium",
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(studentId, out var parsedStudentId) || parsedStudentId == Guid.Empty)
        {
            return NotificationToolResult.Invalid(
                "invalid_student_id",
                "studentId must be a valid, non-empty GUID.");
        }

        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length is < 3 or > 100)
        {
            return NotificationToolResult.Invalid(
                "invalid_title",
                "title must be between 3 and 100 characters.");
        }

        var trimmedMessage = message?.Trim() ?? string.Empty;
        if (trimmedMessage.Length is < 5 or > 500)
        {
            return NotificationToolResult.Invalid(
                "invalid_message",
                "message must be between 5 and 500 characters.");
        }

        var normalizedUrgency = urgency is not null ? urgency.Trim() : string.Empty;
        if (!ValidUrgencies.Contains(normalizedUrgency))
        {
            return NotificationToolResult.Invalid(
                "invalid_urgency",
                "urgency must be one of: Low, Medium, High, Critical.");
        }

        try
        {
            var data = await notificationClient.BroadcastNotificationAsync(
                parsedStudentId,
                trimmedTitle,
                trimmedMessage,
                normalizedUrgency,
                cancellationToken);

            return NotificationToolResult.Success(data);
        }
        catch (HttpRequestException)
        {
            return NotificationToolResult.Failure(
                "student1_unavailable",
                "The Student 1 notification service could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return NotificationToolResult.Failure(
                "student1_timeout",
                "The Student 1 notification service timed out.");
        }
        catch (NotificationClientException ex)
        {
            return NotificationToolResult.Failure(
                "student1_error",
                ex.Message);
        }
    }
}

public sealed record NotificationToolResult(
    string Status,
    string Tool,
    BroadcastNotificationResultData? Data,
    NotificationToolError? Error)
{
    public static NotificationToolResult Success(BroadcastNotificationResultData data) =>
        new("success", "notifications_broadcast_alert", data, null);

    public static NotificationToolResult Invalid(string code, string message) =>
        new("error", "notifications_broadcast_alert", null, new(code, message));

    public static NotificationToolResult Failure(string code, string message) =>
        new("error", "notifications_broadcast_alert", null, new(code, message));
}

public sealed record NotificationToolError(string Code, string Message);
