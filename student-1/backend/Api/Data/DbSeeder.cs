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
    }

    private static void SeedNotifications(AppDbContext db)
    {
        var notifications = db.Notifications;
        if (!notifications.Any())
        {
            var now = DateTime.UtcNow;

            notifications.AddRange(
                new Notification { StudentId = Student1Id, Type = NotificationType.Deadline, SourceMicroservice = "deadline-tracker", IsRead = false, CreatedAtUtc = now.AddHours(-1) },
                new Notification { StudentId = Student1Id, Type = NotificationType.Grade, SourceMicroservice = "grading-service", IsRead = false, CreatedAtUtc = now.AddHours(-2) },
                new Notification { StudentId = Student1Id, Type = NotificationType.Automation, SourceMicroservice = "automation-engine", IsRead = true, CreatedAtUtc = now.AddHours(-3) },
                new Notification { StudentId = Student1Id, Type = NotificationType.Account, SourceMicroservice = "identity-service", IsRead = true, CreatedAtUtc = now.AddHours(-4) },
                new Notification { StudentId = Student1Id, Type = NotificationType.AI, SourceMicroservice = "ai-digest-service", IsRead = false, CreatedAtUtc = now.AddHours(-5) },
                new Notification { StudentId = Student1Id, Type = NotificationType.Deadline, SourceMicroservice = "deadline-tracker", IsRead = false, CreatedAtUtc = now.AddHours(-6) },
                new Notification { StudentId = Student2Id, Type = NotificationType.Grade, SourceMicroservice = "grading-service", IsRead = false, CreatedAtUtc = now.AddHours(-1) },
                new Notification { StudentId = Student2Id, Type = NotificationType.Automation, SourceMicroservice = "automation-engine", IsRead = false, CreatedAtUtc = now.AddHours(-2) },
                new Notification { StudentId = Student2Id, Type = NotificationType.Account, SourceMicroservice = "identity-service", IsRead = true, CreatedAtUtc = now.AddHours(-3) },
                new Notification { StudentId = Student2Id, Type = NotificationType.AI, SourceMicroservice = "ai-digest-service", IsRead = true, CreatedAtUtc = now.AddHours(-4) },
                new Notification { StudentId = Student2Id, Type = NotificationType.Deadline, SourceMicroservice = "deadline-tracker", IsRead = false, CreatedAtUtc = now.AddHours(-5) }
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
}
