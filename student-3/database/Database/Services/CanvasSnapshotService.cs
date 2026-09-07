using System.Globalization;
using Database.Data;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Student3.Contracts;
using TaskStatus = Database.Models.TaskStatus;

namespace Database.Services;

public sealed class CanvasSnapshotService(
    AppDbContext db,
    TaskHierarchyService taskHierarchy)
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    public async Task<CanvasSyncResultRecord> ApplyAsync(
        CanvasSnapshotCommand command,
        CancellationToken cancellationToken)
    {
        await SyncLock.WaitAsync(cancellationToken);
        try
        {
            return await ApplySnapshotAsync(command.Courses, cancellationToken);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task<CanvasSyncResultRecord> ApplySnapshotAsync(
        IReadOnlyCollection<CanvasCourseSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        var duplicateCourseId = snapshots
            .GroupBy(snapshot => snapshot.Id)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateCourseId is not null)
        {
            throw new InvalidOperationException(
                $"The snapshot contains duplicate Canvas course ID {duplicateCourseId}.");
        }

        var allAssignments = snapshots.SelectMany(snapshot => snapshot.Assignments).ToList();
        var duplicateAssignmentId = allAssignments
            .GroupBy(assignment => assignment.Id)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateAssignmentId is not null)
        {
            throw new InvalidOperationException(
                $"The snapshot contains duplicate Canvas assignment ID {duplicateAssignmentId}.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var existingCourses = await db.Courses
            .Where(course => course.CanvasCourseId != null)
            .ToDictionaryAsync(
                course => course.CanvasCourseId!.Value,
                cancellationToken);
        var unlinkedCourses = await db.Courses
            .Where(course => course.CanvasCourseId == null)
            .ToListAsync(cancellationToken);
        var existingTasks = await db.Tasks
            .Where(task => task.CanvasAssignmentId != null)
            .ToDictionaryAsync(
                task => task.CanvasAssignmentId!.Value,
                cancellationToken);

        var seenCourseIds = new HashSet<long>();
        var seenAssignmentIds = new HashSet<long>();
        var coursesCreated = 0;
        var coursesUpdated = 0;
        var coursesDeactivated = 0;
        var tasksCreated = 0;
        var tasksUpdated = 0;
        var tasksDeactivated = 0;

        foreach (var snapshot in snapshots)
        {
            seenCourseIds.Add(snapshot.Id);

            if (!existingCourses.TryGetValue(snapshot.Id, out var course))
            {
                var matchingUnlinkedCourses = string.IsNullOrWhiteSpace(snapshot.CourseCode)
                    ? []
                    : unlinkedCourses
                        .Where(candidate => string.Equals(
                            candidate.Code,
                            snapshot.CourseCode,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (matchingUnlinkedCourses.Count == 1)
                {
                    course = matchingUnlinkedCourses[0];
                    unlinkedCourses.Remove(course);
                    coursesUpdated++;
                }
                else
                {
                    course = new Course
                    {
                        Code = snapshot.CourseCode ??
                            snapshot.Id.ToString(CultureInfo.InvariantCulture),
                        Name = snapshot.Name
                    };
                    db.Courses.Add(course);
                    coursesCreated++;
                }

                course.Code = snapshot.CourseCode ??
                    snapshot.Id.ToString(CultureInfo.InvariantCulture);
                course.Name = snapshot.Name;
                course.CanvasCourseId = snapshot.Id;
                course.CanvasWorkflowState = snapshot.WorkflowState;
                course.CanvasIsActive = true;
                course.LastCanvasSyncAt = now;
                existingCourses.Add(snapshot.Id, course);
            }
            else
            {
                course.Code = snapshot.CourseCode ??
                    snapshot.Id.ToString(CultureInfo.InvariantCulture);
                course.Name = snapshot.Name;
                course.CanvasWorkflowState = snapshot.WorkflowState;
                course.CanvasIsActive = true;
                course.LastCanvasSyncAt = now;
                coursesUpdated++;
            }

            foreach (var assignment in snapshot.Assignments)
            {
                seenAssignmentIds.Add(assignment.Id);

                if (!existingTasks.TryGetValue(assignment.Id, out var task))
                {
                    task = new TaskEntity
                    {
                        Title = assignment.Name,
                        Description = assignment.Description,
                        DueDate = assignment.DueAt,
                        Priority = TaskPriority.Medium,
                        Status = assignment.IsSubmitted
                            ? TaskStatus.Completed
                            : TaskStatus.Todo,
                        Course = course,
                        CanvasAssignmentId = assignment.Id,
                        CanvasUpdatedAt = assignment.UpdatedAt,
                        CanvasWorkflowState = assignment.WorkflowState,
                        CanvasSubmissionState = assignment.SubmissionState,
                        CanvasIsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    db.Tasks.Add(task);
                    existingTasks.Add(assignment.Id, task);
                    tasksCreated++;
                }
                else
                {
                    if (assignment.IsSubmitted)
                    {
                        task.Status = TaskStatus.Completed;
                        await taskHierarchy.CompleteDescendantsAsync(
                            task.Id,
                            now,
                            cancellationToken);
                    }

                    task.Title = assignment.Name;
                    task.Description = assignment.Description;
                    task.DueDate = assignment.DueAt;
                    task.Course = course;
                    task.CanvasUpdatedAt = assignment.UpdatedAt;
                    task.CanvasWorkflowState = assignment.WorkflowState;
                    task.CanvasSubmissionState = assignment.SubmissionState;
                    task.CanvasIsActive = true;
                    task.UpdatedAt = now;
                    tasksUpdated++;
                }
            }
        }

        foreach (var (canvasCourseId, course) in existingCourses)
        {
            if (!seenCourseIds.Contains(canvasCourseId) && course.CanvasIsActive != false)
            {
                course.CanvasIsActive = false;
                course.LastCanvasSyncAt = now;
                coursesDeactivated++;
            }
        }

        foreach (var (canvasAssignmentId, task) in existingTasks)
        {
            if (!seenAssignmentIds.Contains(canvasAssignmentId) && task.CanvasIsActive != false)
            {
                task.CanvasIsActive = false;
                task.UpdatedAt = now;
                tasksDeactivated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CanvasSyncResultRecord(
            coursesCreated,
            coursesUpdated,
            coursesDeactivated,
            tasksCreated,
            tasksUpdated,
            tasksDeactivated);
    }
}
