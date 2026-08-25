using System.Globalization;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class CanvasNotificationSyncService(
    ISharedCanvasClient canvasClient,
    AppDbContext db)
{
    private static readonly Guid DemoStudentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<CanvasNotificationSyncResultDto> SyncAsync(CancellationToken cancellationToken)
    {
        var remoteCourses = await canvasClient.GetCoursesAsync(cancellationToken);
        var assignments = new List<SharedCanvasAssignmentDto>();

        foreach (var course in remoteCourses)
        {
            var courseAssignments = await canvasClient.GetAssignmentsAsync(
                course.Id,
                cancellationToken);
            assignments.AddRange(courseAssignments);
        }

        return await ApplyAsync(assignments, cancellationToken);
    }

    private async Task<CanvasNotificationSyncResultDto> ApplyAsync(
        IReadOnlyCollection<SharedCanvasAssignmentDto> assignments,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var watermarks = await db.CanvasAssignmentWatermarks
            .ToDictionaryAsync(w => w.CanvasAssignmentId, cancellationToken);

        var notificationsCreated = 0;

        foreach (var assignment in assignments)
        {
            var submissionState = assignment.Submission?.WorkflowState;

            if (!watermarks.TryGetValue(assignment.Id, out var watermark))
            {
                db.Notifications.Add(new Notification
                {
                    StudentId = DemoStudentId,
                    Type = NotificationType.Deadline,
                    SourceMicroservice = "canvas-sync",
                    Message = $"New assignment \"{assignment.Name}\" is due " +
                        $"{FormatDate(assignment.DueAt)}.",
                    IsRead = false,
                    CreatedAtUtc = now
                });
                notificationsCreated++;

                watermark = new CanvasAssignmentWatermark
                {
                    CanvasAssignmentId = assignment.Id
                };
                db.CanvasAssignmentWatermarks.Add(watermark);
                watermarks.Add(assignment.Id, watermark);
            }
            else
            {
                if (watermark.LastDueDate != assignment.DueAt)
                {
                    db.Notifications.Add(new Notification
                    {
                        StudentId = DemoStudentId,
                        Type = NotificationType.Deadline,
                        SourceMicroservice = "canvas-sync",
                        Message = $"Due date for \"{assignment.Name}\" changed from " +
                            $"{FormatDate(watermark.LastDueDate)} to {FormatDate(assignment.DueAt)}.",
                        IsRead = false,
                        CreatedAtUtc = now
                    });
                    notificationsCreated++;
                }

                if (IsSubmittedOrGraded(submissionState) &&
                    !IsSubmittedOrGraded(watermark.LastSubmissionState))
                {
                    db.Notifications.Add(new Notification
                    {
                        StudentId = DemoStudentId,
                        Type = NotificationType.Grade,
                        SourceMicroservice = "canvas-sync",
                        Message = $"\"{assignment.Name}\" is now {submissionState}.",
                        IsRead = false,
                        CreatedAtUtc = now
                    });
                    notificationsCreated++;
                }
            }

            watermark.LastDueDate = assignment.DueAt;
            watermark.LastWorkflowState = assignment.WorkflowState;
            watermark.LastSubmissionState = submissionState;
            watermark.LastSeenAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CanvasNotificationSyncResultDto(notificationsCreated);
    }

    private static bool IsSubmittedOrGraded(string? workflowState)
    {
        return workflowState is "submitted" or "graded";
    }

    private static string FormatDate(DateTime? date)
    {
        return date is null
            ? "an unknown date"
            : date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}

public sealed record CanvasNotificationSyncResultDto(int NotificationsCreated);
