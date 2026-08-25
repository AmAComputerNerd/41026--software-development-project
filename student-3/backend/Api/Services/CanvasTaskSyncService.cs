using Api.Data;
using Api.DTOs;
using Api.Models;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Api.Models.TaskStatus;

namespace Api.Services;

public sealed class CanvasTaskSyncService(
    ISharedCanvasClient canvasClient,
    AppDbContext db,
    TaskHierarchyService taskHierarchy)
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    public async Task<CanvasSyncResultDto> SyncAsync(CancellationToken cancellationToken)
    {
        await SyncLock.WaitAsync(cancellationToken);
        try
        {
            var remoteCourses = await canvasClient.GetCoursesAsync(cancellationToken);
            var snapshots = new List<CourseSnapshot>(remoteCourses.Count);

            foreach (var course in remoteCourses)
            {
                var assignments = await canvasClient.GetAssignmentsAsync(
                    course.Id,
                    cancellationToken);

                if (assignments.Any(assignment => assignment.CourseId != course.Id))
                {
                    throw new SharedServiceException(
                        $"The shared service returned an assignment for the wrong course ({course.Id}).");
                }

                snapshots.Add(new CourseSnapshot(course, assignments));
            }

            return await ApplySnapshotAsync(snapshots, cancellationToken);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task<CanvasSyncResultDto> ApplySnapshotAsync(
        IReadOnlyCollection<CourseSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        var duplicateCourseId = snapshots
            .GroupBy(snapshot => snapshot.Course.Id)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateCourseId is not null)
        {
            throw new SharedServiceException(
                $"The shared service returned duplicate Canvas course ID {duplicateCourseId}.");
        }

        var allAssignments = snapshots.SelectMany(snapshot => snapshot.Assignments).ToList();
        var duplicateAssignmentId = allAssignments
            .GroupBy(assignment => assignment.Id)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateAssignmentId is not null)
        {
            throw new SharedServiceException(
                $"The shared service returned duplicate Canvas assignment ID {duplicateAssignmentId}.");
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
            seenCourseIds.Add(snapshot.Course.Id);

            if (!existingCourses.TryGetValue(snapshot.Course.Id, out var course))
            {
                var matchingUnlinkedCourses = string.IsNullOrWhiteSpace(snapshot.Course.CourseCode)
                    ? []
                    : unlinkedCourses
                        .Where(candidate => string.Equals(
                            candidate.Code,
                            snapshot.Course.CourseCode,
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
                        Code = snapshot.Course.CourseCode ??
                            snapshot.Course.Id.ToString(CultureInfo.InvariantCulture),
                        Name = snapshot.Course.Name
                    };
                    db.Courses.Add(course);
                    coursesCreated++;
                }

                course.Code = snapshot.Course.CourseCode ??
                    snapshot.Course.Id.ToString(CultureInfo.InvariantCulture);
                course.Name = snapshot.Course.Name;
                course.CanvasCourseId = snapshot.Course.Id;
                course.CanvasWorkflowState = snapshot.Course.WorkflowState;
                course.CanvasIsActive = true;
                course.LastCanvasSyncAt = now;
                existingCourses.Add(snapshot.Course.Id, course);
            }
            else
            {
                course.Code = snapshot.Course.CourseCode ??
                    snapshot.Course.Id.ToString(CultureInfo.InvariantCulture);
                course.Name = snapshot.Course.Name;
                course.CanvasWorkflowState = snapshot.Course.WorkflowState;
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
                        Status = IsSubmitted(assignment.Submission)
                            ? TaskStatus.Completed
                            : TaskStatus.Todo,
                        Course = course,
                        CanvasAssignmentId = assignment.Id,
                        CanvasUpdatedAt = assignment.UpdatedAt,
                        CanvasWorkflowState = assignment.WorkflowState,
                        CanvasSubmissionState = assignment.Submission?.WorkflowState,
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
                    if (IsSubmitted(assignment.Submission))
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
                    task.CanvasSubmissionState = assignment.Submission?.WorkflowState;
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

        return new CanvasSyncResultDto(
            coursesCreated,
            coursesUpdated,
            coursesDeactivated,
            tasksCreated,
            tasksUpdated,
            tasksDeactivated);
    }

    private static bool IsSubmitted(SharedCanvasSubmissionDto? submission)
    {
        return submission?.WorkflowState is "submitted" or "graded";
    }

    private sealed record CourseSnapshot(
        SharedCanvasCourseDto Course,
        IReadOnlyList<SharedCanvasAssignmentDto> Assignments);
}
