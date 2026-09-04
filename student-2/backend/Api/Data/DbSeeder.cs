using System.Text.Json;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DbSeeder
{
    private static readonly Guid StudentId = Guid.Parse("25341708-0000-0000-0000-000000000002");
    private static readonly string[] ScheduledPostRecipients = ["100001"];

    public static async Task SeedDataAsync(AppDbContext db)
    {
        var legacySeedIds = Enumerable.Range(102, 9)
            .Concat(Enumerable.Range(202, 9))
            .Select(CreateGuid)
            .ToArray();
        var legacySeeds = await db.Automations
            .Where(automation => legacySeedIds.Contains(automation.Id))
            .ToListAsync();
        db.Automations.RemoveRange(legacySeeds);

        var extensionId = CreateGuid(101);
        if (!await db.Automations.AnyAsync(automation => automation.Id == extensionId))
        {
            db.Automations.Add(new AssignmentExtensionAutomation
            {
                Id = extensionId,
                StudentId = StudentId,
                Enabled = true,
                SubjectId = null,
                BufferMinutes = 30,
                Reason = AssignmentExtensionReason.UNW,
                FurtherDetails = "Supporting details for the assignment extension automation."
            });
        }

        var postId = CreateGuid(201);
        if (!await db.Automations.AnyAsync(automation => automation.Id == postId))
        {
            db.Automations.Add(new ScheduledPostAutomation
            {
                Id = postId,
                StudentId = StudentId,
                Enabled = true,
                PostTime = DateTime.UtcNow.Date.AddDays(1).AddHours(9),
                ContextCode = "course_1001",
                Recipients = JsonSerializer.Serialize(ScheduledPostRecipients),
                Subject = "Scheduled post",
                Body = "Body for the scheduled post automation.",
                GroupConversation = true
            });
        }

        var quizFillerId = CreateGuid(501);
        if (!await db.Automations.AnyAsync(automation => automation.Id == quizFillerId))
        {
            db.Automations.Add(new QuizFillerAutomation
            {
                Id = quizFillerId,
                StudentId = StudentId,
                Enabled = true,
                SubjectId = null,
                MultipleChoice = true,
                ShortAnswer = true,
                NumberOfAttemptsRequired = 2,
                AllowForNoTimeLimit = true
            });
        }

        if (!await db.AutomationRuns.AnyAsync(run => run.Id == CreateGuid(300)))
        {
            db.AutomationRuns.Add(new AssignmentExtensionAutomationRun
            {
                Id = CreateGuid(300),
                AutomationId = extensionId,
                ExecutionKey = "seed",
                ExecutionTimeStamp = DateTime.UtcNow.AddDays(-1),
                Result = "SUC",
                AssignmentId = "assignment-1"
            });
        }

        if (!await db.AutomationRuns.AnyAsync(run => run.Id == CreateGuid(400)))
        {
            db.AutomationRuns.Add(new ScheduledPostAutomationRun
            {
                Id = CreateGuid(400),
                AutomationId = postId,
                ExecutionKey = "once",
                ExecutionTimeStamp = DateTime.UtcNow.AddDays(-1),
                Result = "SUC",
                PostTime = DateTime.UtcNow.Date.AddDays(1).AddHours(9),
                ContextCode = "course_1001",
                Recipients = JsonSerializer.Serialize(ScheduledPostRecipients),
                Subject = "Scheduled post",
                Body = "Body for the scheduled post automation.",
                GroupConversation = true
            });
        }

        if (!await db.AutomationRuns.AnyAsync(run => run.Id == CreateGuid(600)))
        {
            db.AutomationRuns.Add(new QuizFillerAutomationRun
            {
                Id = CreateGuid(600),
                AutomationId = quizFillerId,
                ExecutionKey = "quiz-filler:v1:2001",
                ExecutionTimeStamp = DateTime.UtcNow.AddDays(-1),
                Result = "SUC",
                CourseId = 1001,
                QuizId = 2001,
                QuizTitle = "Practice quiz",
                QuestionCount = 10
            });
        }

        await db.SaveChangesAsync();
    }

    private static Guid CreateGuid(int value)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
    }
}