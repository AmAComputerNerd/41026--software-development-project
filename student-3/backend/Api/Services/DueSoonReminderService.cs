using Api.DTOs;

namespace Api.Services;

public sealed class DueSoonReminderService(
    IStudent3DatabaseClient database,
    INotificationClient notificationClient,
    IConfiguration configuration)
{
    private static readonly Guid DemoStudentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<int> SendDueSoonRemindersAsync(CancellationToken cancellationToken)
    {
        var hoursBeforeDue = configuration.GetValue("DueSoonReminder:HoursBeforeDue", 24);
        var finalHoursBeforeDue = configuration.GetValue("DueSoonReminder:FinalHoursBeforeDue", 3);
        var candidates = await database.GetDueRemindersAsync(
            hoursBeforeDue,
            finalHoursBeforeDue,
            cancellationToken);

        var remindersSent = 0;

        foreach (var task in candidates)
        {
            var dueDate = task.DueDate!.Value;
            await notificationClient.PushAsync(
                new PushNotificationDto(
                    StudentId: DemoStudentId,
                    Type: "Deadline",
                    SourceMicroservice: "deadlines",
                    Message: $"\"{task.Title}\" is due {FormatDate(dueDate)}.",
                    RelatedEntityType: "Task",
                    RelatedEntityId: task.Id),
                cancellationToken);

            await database.MarkReminderSentAsync(
                task.Id,
                DateTime.UtcNow,
                cancellationToken);
            remindersSent++;
        }

        return remindersSent;
    }

    private static string FormatDate(DateTime value)
    {
        return value.ToString("MMM d, yyyy 'at' h:mm tt 'UTC'", System.Globalization.CultureInfo.InvariantCulture);
    }
}
