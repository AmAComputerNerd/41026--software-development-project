using Api.Models;

namespace Api.Data;

public static class DbSeeder
{
    private static readonly Guid Student1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Student2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static void SeedData(AppDbContext db)
    {
        SeedNotifications(db);
        SeedNotificationPreferences(db);
        SeedAiDigests(db);
        SeedChatSessions(db);
    }

    private static void SeedNotifications(AppDbContext db)
    {
        var notifications = db.Notifications;
        if (!notifications.Any())
        {
            var now = DateTime.UtcNow;

            notifications.AddRange(
                new Notification { StudentId = Student1Id, Type = NotificationType.Deadline, SourceMicroservice = "deadline-tracker", Message = "Your assignment is due soon.", IsRead = false, CreatedAtUtc = now.AddHours(-1) },
                new Notification { StudentId = Student1Id, Type = NotificationType.Grade, SourceMicroservice = "grading-service", Message = "A new grade has been posted.", IsRead = false, CreatedAtUtc = now.AddHours(-2) },
                new Notification { StudentId = Student1Id, Type = NotificationType.Automation, SourceMicroservice = "automation-engine", Message = "Automation run completed successfully.", IsRead = true, CreatedAtUtc = now.AddHours(-3) },
                new Notification { StudentId = Student1Id, Type = NotificationType.Account, SourceMicroservice = "identity-service", Message = "Your account settings were updated.", IsRead = true, CreatedAtUtc = now.AddHours(-4) },
                new Notification { StudentId = Student1Id, Type = NotificationType.AI, SourceMicroservice = "ai-digest-service", Message = "Your weekly AI digest is ready.", IsRead = false, CreatedAtUtc = now.AddHours(-5) },
                new Notification { StudentId = Student1Id, Type = NotificationType.Deadline, SourceMicroservice = "deadline-tracker", Message = "A task deadline is approaching.", IsRead = false, CreatedAtUtc = now.AddHours(-6) },
                new Notification { StudentId = Student2Id, Type = NotificationType.Grade, SourceMicroservice = "grading-service", Message = "A new grade has been posted.", IsRead = false, CreatedAtUtc = now.AddHours(-1) },
                new Notification { StudentId = Student2Id, Type = NotificationType.Automation, SourceMicroservice = "automation-engine", Message = "Automation flagged a task for follow-up.", IsRead = false, CreatedAtUtc = now.AddHours(-2) },
                new Notification { StudentId = Student2Id, Type = NotificationType.Account, SourceMicroservice = "identity-service", Message = "Your account settings were updated.", IsRead = true, CreatedAtUtc = now.AddHours(-3) },
                new Notification { StudentId = Student2Id, Type = NotificationType.AI, SourceMicroservice = "ai-digest-service", Message = "Your weekly AI digest is ready.", IsRead = true, CreatedAtUtc = now.AddHours(-4) },
                new Notification { StudentId = Student2Id, Type = NotificationType.Deadline, SourceMicroservice = "deadline-tracker", Message = "A task deadline is approaching.", IsRead = false, CreatedAtUtc = now.AddHours(-5) }
            );

            db.SaveChanges();
        }
    }

    private static void SeedNotificationPreferences(AppDbContext db)
    {
        var preferences = db.NotificationPreferences;
        if (!preferences.Any())
        {
            var now = DateTime.UtcNow;

            preferences.AddRange(
                new NotificationPreference { StudentId = Student1Id, Type = NotificationType.Deadline, Channel = NotificationChannel.InApp, Enabled = true, UpdatedAtUtc = now },
                new NotificationPreference { StudentId = Student1Id, Type = NotificationType.Deadline, Channel = NotificationChannel.Email, Enabled = true, UpdatedAtUtc = now },
                new NotificationPreference { StudentId = Student1Id, Type = NotificationType.Grade, Channel = NotificationChannel.InApp, Enabled = true, UpdatedAtUtc = now },
                new NotificationPreference { StudentId = Student1Id, Type = NotificationType.Grade, Channel = NotificationChannel.Email, Enabled = false, UpdatedAtUtc = now },
                new NotificationPreference { StudentId = Student1Id, Type = NotificationType.Automation, Channel = NotificationChannel.InApp, Enabled = false, UpdatedAtUtc = now },
                new NotificationPreference { StudentId = Student1Id, Type = NotificationType.Account, Channel = NotificationChannel.Email, Enabled = true, UpdatedAtUtc = now },
                new NotificationPreference { StudentId = Student1Id, Type = NotificationType.AI, Channel = NotificationChannel.InApp, Enabled = true, UpdatedAtUtc = now },
                new NotificationPreference { StudentId = Student2Id, Type = NotificationType.Deadline, Channel = NotificationChannel.InApp, Enabled = true, UpdatedAtUtc = now },
                new NotificationPreference { StudentId = Student2Id, Type = NotificationType.Grade, Channel = NotificationChannel.Email, Enabled = true, UpdatedAtUtc = now },
                new NotificationPreference { StudentId = Student2Id, Type = NotificationType.Automation, Channel = NotificationChannel.Email, Enabled = false, UpdatedAtUtc = now },
                new NotificationPreference { StudentId = Student2Id, Type = NotificationType.AI, Channel = NotificationChannel.InApp, Enabled = false, UpdatedAtUtc = now }
            );

            db.SaveChanges();
        }
    }

    private static void SeedAiDigests(AppDbContext db)
    {
        var digests = db.AiDigests;
        if (!digests.Any())
        {
            var now = DateTime.UtcNow;

            digests.AddRange(
                new AiDigest { StudentId = Student1Id, Summary = "You have 2 deadlines due this week and 1 unread grade.", GeneratedAtUtc = now.AddDays(-1) },
                new AiDigest { StudentId = Student1Id, Summary = "Automation run completed successfully for your enrolled courses.", GeneratedAtUtc = now.AddDays(-2) },
                new AiDigest { StudentId = Student1Id, Summary = "Account settings were updated; review recent activity.", GeneratedAtUtc = now.AddDays(-3) },
                new AiDigest { StudentId = Student1Id, Summary = "3 tasks completed this week, on track for the sprint.", GeneratedAtUtc = now.AddDays(-4) },
                new AiDigest { StudentId = Student1Id, Summary = "New grade posted for Advanced Software Development.", GeneratedAtUtc = now.AddDays(-5) },
                new AiDigest { StudentId = Student2Id, Summary = "1 deadline overdue, consider rescheduling your study plan.", GeneratedAtUtc = now.AddDays(-1) },
                new AiDigest { StudentId = Student2Id, Summary = "Weekly summary: 5 notifications, 2 unread.", GeneratedAtUtc = now.AddDays(-2) },
                new AiDigest { StudentId = Student2Id, Summary = "Automation engine flagged a task for follow-up.", GeneratedAtUtc = now.AddDays(-3) },
                new AiDigest { StudentId = Student2Id, Summary = "No new grades this week.", GeneratedAtUtc = now.AddDays(-4) },
                new AiDigest { StudentId = Student2Id, Summary = "Digest generated: all systems normal.", GeneratedAtUtc = now.AddDays(-5) }
            );

            db.SaveChanges();
        }
    }

    private static void SeedChatSessions(AppDbContext db)
    {
        var chatSessions = db.ChatSessions;
        if (!chatSessions.Any())
        {
            var now = DateTime.UtcNow;

            var session1 = new ChatSession
            {
                Id = Guid.NewGuid(),
                StudentId = Student1Id,
                Title = "Study Plan & Assessment 1 Priority",
                CreatedAtUtc = now.AddDays(-2),
                UpdatedAtUtc = now.AddDays(-2).AddMinutes(15)
            };

            var session1Msg1 = new ChatMessage
            {
                ChatSessionId = session1.Id,
                Role = "assistant",
                Content = "Hello Bryan! Here is your quick notification digest: You have 2 approaching deadlines (Assignment due soon and task deadline), plus 1 unread grade posted. I recommend prioritizing your upcoming assignment due tomorrow.",
                CreatedAtUtc = now.AddDays(-2),
                Confidence = "HIGH"
            };

            var session1Msg2 = new ChatMessage
            {
                ChatSessionId = session1.Id,
                Role = "user",
                Content = "What is the penalty if I submit 1 day late?",
                CreatedAtUtc = now.AddDays(-2).AddMinutes(5)
            };

            var session1Msg3 = new ChatMessage
            {
                ChatSessionId = session1.Id,
                Role = "assistant",
                Content = "According to course policies, a **5% penalty per 24-hour period** applies to late submissions. Submissions more than 5 days late receive 0% unless an extension is granted.",
                CreatedAtUtc = now.AddDays(-2).AddMinutes(6),
                Confidence = "HIGH",
                CitationsJson = "[\"docs/knowledge-base/course_policies.md\"]"
            };

            var session2 = new ChatSession
            {
                Id = Guid.NewGuid(),
                StudentId = Student1Id,
                Title = "Grade Query & Special Consideration",
                CreatedAtUtc = now.AddDays(-1),
                UpdatedAtUtc = now.AddDays(-1).AddMinutes(8)
            };

            var session2Msg1 = new ChatMessage
            {
                ChatSessionId = session2.Id,
                Role = "assistant",
                Content = "Notification digest: 1 new grade posted in Advanced Software Development. Check your Grade Impact in the notifications feed!",
                CreatedAtUtc = now.AddDays(-1),
                Confidence = "HIGH"
            };

            var session2Msg2 = new ChatMessage
            {
                ChatSessionId = session2.Id,
                Role = "user",
                Content = "Can I request special consideration?",
                CreatedAtUtc = now.AddDays(-1).AddMinutes(3)
            };

            var session2Msg3 = new ChatMessage
            {
                ChatSessionId = session2.Id,
                Role = "assistant",
                Content = "Yes. Extensions up to 48 hours must be requested through the Student Portal at least 24 hours prior to the due date, accompanied by relevant documentation.",
                CreatedAtUtc = now.AddDays(-1).AddMinutes(4),
                Confidence = "HIGH",
                CitationsJson = "[\"docs/knowledge-base/course_policies.md\"]"
            };

            var session3 = new ChatSession
            {
                Id = Guid.NewGuid(),
                StudentId = Student2Id,
                Title = "Weekly Planning Overview",
                CreatedAtUtc = now.AddDays(-1),
                UpdatedAtUtc = now.AddDays(-1).AddMinutes(10)
            };

            var session3Msg1 = new ChatMessage
            {
                ChatSessionId = session3.Id,
                Role = "assistant",
                Content = "Good day! You have 1 unread grade and 1 pending automation task requiring follow-up.",
                CreatedAtUtc = now.AddDays(-1),
                Confidence = "HIGH"
            };

            chatSessions.AddRange(session1, session2, session3);
            db.ChatMessages.AddRange(
                session1Msg1, session1Msg2, session1Msg3,
                session2Msg1, session2Msg2, session2Msg3,
                session3Msg1);

            db.SaveChanges();
        }
    }
}
