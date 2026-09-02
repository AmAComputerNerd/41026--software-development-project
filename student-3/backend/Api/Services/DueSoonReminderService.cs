using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Api.Models.TaskStatus;

namespace Api.Services;

public sealed class DueSoonReminderService(
    AppDbContext db,
    INotificationClient notificationClient,
    IConfiguration configuration)
{
    private static readonly Guid DemoStudentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<int> SendDueSoonRemindersAsync(CancellationToken cancellationToken)
    {
        var hoursBeforeDue = configuration.GetValue("DueSoonReminder:HoursBeforeDue", 24);
        var finalHoursBeforeDue = configuration.GetValue("DueSoonReminder:FinalHoursBeforeDue", 3);
        var now = DateTime.UtcNow;

        var candidates = await db.Tasks
            .Where(t => t.DueDate != null
                && t.Status != TaskStatus.Completed
                && t.CanvasIsActive != false
                && t.DueDate > now
                && t.DueDate <= now.AddHours(hoursBeforeDue))
            .ToListAsync(cancellationToken);

        var remindersSent = 0;

        foreach (var task in candidates)
        {
            var dueDate = task.DueDate!.Value;
            var isFirstReminder = task.DueSoonReminderSentAtUtc is null;
            var isFinalReminder = !isFirstReminder
                && dueDate <= now.AddHours(finalHoursBeforeDue)
                && task.DueSoonReminderSentAtUtc < dueDate.AddHours(-finalHoursBeforeDue);

            if (!isFirstReminder && !isFinalReminder)
            {
                continue;
            }

            await notificationClient.PushAsync(
                new PushNotificationDto(
                    StudentId: DemoStudentId,
                    Type: "Deadline",
                    SourceMicroservice: "deadlines",
                    Message: $"\"{task.Title}\" is due {FormatDate(dueDate)}.",
                    RelatedEntityType: "Task",
                    RelatedEntityId: task.Id),
                cancellationToken);

            task.DueSoonReminderSentAtUtc = now;
            remindersSent++;
        }

        if (remindersSent > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return remindersSent;
    }

    private static string FormatDate(DateTime value)
    {
        return value.ToString("MMM d, yyyy 'at' h:mm tt 'UTC'", System.Globalization.CultureInfo.InvariantCulture);
    }
}
