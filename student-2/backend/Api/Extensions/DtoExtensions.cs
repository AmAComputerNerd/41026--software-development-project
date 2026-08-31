using System.Text.Json;
using Api.DTOs;
using Api.Models;

namespace Api.Extensions;

public static class DtoExtensions
{
    public static AutomationDto ToDto(this Automation automation)
    {
        return automation switch
        {
            AssignmentExtensionAutomation extension => new AutomationDto(
                extension.Id,
                extension.StudentId,
                "AssignmentExtension",
                extension.Enabled,
                extension.BufferMinutes,
                extension.Reason,
                extension.FurtherDetails,
                null,
                null,
                null,
                null),
            ScheduledPostAutomation post => new AutomationDto(
                post.Id,
                post.StudentId,
                "ScheduledPost",
                post.Enabled,
                null,
                null,
                null,
                post.PostTime,
                DeserializeRecipients(post.Recipients),
                post.Subject,
                post.Body),
            _ => throw new InvalidOperationException("Unsupported automation type.")
        };
    }

    public static AutomationRunDto ToDto(this AutomationRun run)
    {
        return run switch
        {
            AssignmentExtensionAutomationRun extension => new AutomationRunDto(
                extension.Id,
                extension.AutomationId,
                "AssignmentExtension",
                extension.ExecutionTimeStamp,
                extension.Result,
                extension.AssignmentId,
                null,
                null,
                null),
            ScheduledPostAutomationRun post => new AutomationRunDto(
                post.Id,
                post.AutomationId,
                "ScheduledPost",
                post.ExecutionTimeStamp,
                post.Result,
                null,
                DeserializeRecipients(post.Recipients),
                post.Subject,
                post.Body),
            _ => throw new InvalidOperationException("Unsupported automation run type.")
        };
    }

    public static string SerializeRecipients(IReadOnlyList<string> recipients)
    {
        return JsonSerializer.Serialize(recipients);
    }

    private static string[] DeserializeRecipients(string recipients)
    {
        return JsonSerializer.Deserialize<string[]>(recipients) ?? [];
    }
}