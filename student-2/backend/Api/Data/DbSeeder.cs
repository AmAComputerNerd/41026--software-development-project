using System.Text.Json;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DbSeeder
{
    private static readonly Guid StudentId = Guid.Parse("25341708-0000-0000-0000-000000000002");
    private const long SeedCourseId = 39716;
    private const string SeedCourseContext = "course_39716";
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
        var seededPost = await db.Automations
            .OfType<ScheduledPostAutomation>()
            .SingleOrDefaultAsync(automation => automation.Id == postId);
        if (seededPost is null)
        {
            db.Automations.Add(new ScheduledPostAutomation
            {
                Id = postId,
                StudentId = StudentId,
                Enabled = true,
                PostTime = DateTime.UtcNow.Date.AddDays(1).AddHours(9),
                ContextCode = SeedCourseContext,
                Recipients = JsonSerializer.Serialize(ScheduledPostRecipients),
                Subject = "Course announcement",
                Body = "Body for the scheduled post automation.",
                GroupConversation = true
            });
        }
        else
        {
            if (seededPost.Subject == "Scheduled post")
            {
                seededPost.Subject = "Course announcement";
            }

            if (seededPost.ContextCode == "course_1001")
            {
                seededPost.ContextCode = SeedCourseContext;
            }
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

        var seededAssignmentRunId = CreateGuid(300);
        var seededAssignmentRun = await db.AutomationRuns
            .OfType<AssignmentExtensionAutomationRun>()
            .SingleOrDefaultAsync(run => run.Id == seededAssignmentRunId);
        if (seededAssignmentRun is null)
        {
            db.AutomationRuns.Add(new AssignmentExtensionAutomationRun
            {
                Id = seededAssignmentRunId,
                AutomationId = extensionId,
                ExecutionKey = "seed",
                ExecutionTimeStamp = DateTime.UtcNow.AddDays(-1),
                Result = "SUC",
                AssignmentId = "assignment-1",
                AssignmentTitle = "Assessment 1",
                CourseId = SeedCourseId
            });
        }
        else if (seededAssignmentRun.CourseId is null)
        {
            seededAssignmentRun.CourseId = SeedCourseId;
        }

        var seededPostRunId = CreateGuid(400);
        var seededPostRun = await db.AutomationRuns
            .OfType<ScheduledPostAutomationRun>()
            .SingleOrDefaultAsync(run => run.Id == seededPostRunId);
        if (seededPostRun is null)
        {
            db.AutomationRuns.Add(new ScheduledPostAutomationRun
            {
                Id = seededPostRunId,
                AutomationId = postId,
                ExecutionKey = "once",
                ExecutionTimeStamp = DateTime.UtcNow.AddDays(-1),
                Result = "SUC",
                PostTime = DateTime.UtcNow.Date.AddDays(1).AddHours(9),
                ContextCode = SeedCourseContext,
                Recipients = JsonSerializer.Serialize(ScheduledPostRecipients),
                Subject = "Course announcement",
                Body = "Body for the scheduled post automation.",
                GroupConversation = true
            });
        }
        else
        {
            if (seededPostRun.Subject == "Scheduled post")
            {
                seededPostRun.Subject = "Course announcement";
            }

            if (seededPostRun.ContextCode == "course_1001")
            {
                seededPostRun.ContextCode = SeedCourseContext;
            }
        }

        var seededQuizRunId = CreateGuid(600);
        var seededQuizRun = await db.AutomationRuns
            .OfType<QuizFillerAutomationRun>()
            .SingleOrDefaultAsync(run => run.Id == seededQuizRunId);
        if (seededQuizRun is null)
        {
            db.AutomationRuns.Add(new QuizFillerAutomationRun
            {
                Id = seededQuizRunId,
                AutomationId = quizFillerId,
                ExecutionKey = "quiz-filler:v1:2001",
                ExecutionTimeStamp = DateTime.UtcNow.AddDays(-1),
                Result = "SUC",
                CourseId = SeedCourseId,
                QuizId = 2001,
                QuizTitle = "Practice quiz",
                QuestionCount = 10
            });
        }
        else if (seededQuizRun.CourseId == 1001)
        {
            seededQuizRun.CourseId = SeedCourseId;
        }

        await db.SaveChangesAsync();
    }

    private static Guid CreateGuid(int value)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
    }
}