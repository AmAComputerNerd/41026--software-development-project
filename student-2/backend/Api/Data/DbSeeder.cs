using System.Text.Json;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DbSeeder
{
    private static readonly Guid StudentId = Guid.Parse("25341708-0000-0000-0000-000000000002");

    public static async Task SeedDataAsync(AppDbContext db)
    {
        if (await db.Automations.AnyAsync())
        {
            return;
        }

        var extensionAutomations = Enumerable.Range(1, 10)
            .Select(index => new AssignmentExtensionAutomation
            {
                Id = CreateGuid(100 + index),
                StudentId = StudentId,
                Enabled = index <= 3,
                BufferMinutes = index * 30,
                Reason = $"Extension request reason {index}",
                FurtherDetails = $"Supporting details for extension automation {index}."
            })
            .ToList();

        var postAutomations = Enumerable.Range(1, 10)
            .Select(index => new ScheduledPostAutomation
            {
                Id = CreateGuid(200 + index),
                StudentId = StudentId,
                Enabled = index <= 3,
                PostTime = DateTime.UtcNow.Date.AddDays(index).AddHours(9),
                Recipients = JsonSerializer.Serialize(new[] { $"student{index}@example.edu.au" }),
                Subject = $"Scheduled post {index}",
                Body = $"Body for scheduled post automation {index}."
            })
            .ToList();

        var extensionRuns = extensionAutomations.Select((automation, index) =>
            new AssignmentExtensionAutomationRun
            {
                Id = CreateGuid(300 + index),
                AutomationId = automation.Id,
                ExecutionTimeStamp = DateTime.UtcNow.AddDays(-(index + 1)),
                Result = index % 4 == 0 ? "FAI" : "SUC",
                AssignmentId = $"assignment-{index + 1}"
            });

        var postRuns = postAutomations.Select((automation, index) =>
            new ScheduledPostAutomationRun
            {
                Id = CreateGuid(400 + index),
                AutomationId = automation.Id,
                ExecutionTimeStamp = DateTime.UtcNow.AddDays(-(index + 1)),
                Result = index % 5 == 0 ? "FAI" : "SUC",
                Recipients = automation.Recipients,
                Subject = automation.Subject,
                Body = automation.Body
            });

        db.Automations.AddRange(extensionAutomations);
        db.Automations.AddRange(postAutomations);
        db.AutomationRuns.AddRange(extensionRuns);
        db.AutomationRuns.AddRange(postRuns);
        await db.SaveChangesAsync();
    }

    private static Guid CreateGuid(int value)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
    }
}