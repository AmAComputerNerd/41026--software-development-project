using System.Globalization;
using GradesManager.Data;
using GradesManager.DTOs;
using GradesManager.Models;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Services;

public sealed class CanvasTaskSyncService(
    ISharedCanvasClient canvasClient,
    AppDbContext db)
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
                var assignmentGroups = await canvasClient.GetAssignmentGroupsAsync(
                    course.Id,
                    cancellationToken);

                if (assignments.Any(assignment => assignment.CourseId != course.Id))
                {
                    throw new SharedServiceException(
                        $"The shared service returned an assignment for the wrong course ({course.Id}).");
                }

                snapshots.Add(new CourseSnapshot(course, assignments, assignmentGroups));
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
        var existingAssignmentGroups = await db.AssignmentGroups
            .Where(group => group.CanvasAssignmentGroupId != null)
            .ToDictionaryAsync(
                group => (group.CourseId, group.CanvasAssignmentGroupId!.Value),
                cancellationToken);
        var existingAssignments = await db.Assignments
            .Where(assignment => assignment.CanvasAssignmentId != null)
            .ToDictionaryAsync(
                assignment => assignment.CanvasAssignmentId!.Value,
                cancellationToken);

        var seenCourseIds = new HashSet<long>();
        var seenAssignmentGroupIds = new HashSet<(Guid CourseId, long CanvasAssignmentGroupId)>();
        var seenAssignmentIds = new HashSet<long>();

        var coursesCreated = 0;
        var coursesUpdated = 0;
        var coursesDeactivated = 0;
        var assignmentGroupsCreated = 0;
        var assignmentGroupsUpdated = 0;
        var assignmentGroupsDeactivated = 0;
        var assignmentsCreated = 0;
        var assignmentsUpdated = 0;
        var assignmentsDeactivated = 0;

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
            }
            else
            {
                coursesUpdated++;
            }

            course.Code = snapshot.Course.CourseCode ??
                snapshot.Course.Id.ToString(CultureInfo.InvariantCulture);
            course.Name = snapshot.Course.Name;
            course.CanvasCourseId = snapshot.Course.Id;
            course.CanvasWorkflowState = snapshot.Course.WorkflowState;
            course.CanvasIsActive = true;
            course.LastCanvasSyncAt = now;
            existingCourses[snapshot.Course.Id] = course;

            var groupsByCanvasId = new Dictionary<long, AssignmentGroup>();

            foreach (var remoteGroup in snapshot.AssignmentGroups)
            {
                seenAssignmentGroupIds.Add((course.CourseId, remoteGroup.Id));

                if (!existingAssignmentGroups.TryGetValue(
                    (course.CourseId, remoteGroup.Id), out var group))
                {
                    group = new AssignmentGroup
                    {
                        CourseId = course.CourseId,
                        CanvasAssignmentGroupId = remoteGroup.Id,
                        Name = remoteGroup.Name,
                        Weight = remoteGroup.Weight
                    };
                    db.AssignmentGroups.Add(group);
                    existingAssignmentGroups[(course.CourseId, remoteGroup.Id)] = group;
                    assignmentGroupsCreated++;
                }
                else
                {
                    group.CourseId = course.CourseId;
                    group.Name = remoteGroup.Name;
                    group.Weight = remoteGroup.Weight;
                    assignmentGroupsUpdated++;
                }

                groupsByCanvasId[remoteGroup.Id] = group;
            }

            foreach (var remoteAssignment in snapshot.Assignments)
            {
                seenAssignmentIds.Add(remoteAssignment.Id);

                if (!groupsByCanvasId.TryGetValue(remoteAssignment.AssignmentGroupId, out var group))
                {
                    throw new SharedServiceException(
                        $"Assignment {remoteAssignment.Id} references unknown assignment group {remoteAssignment.AssignmentGroupId}.");
                }

                // Canvas returns null points_possible for assignments that
                // aren't graded (e.g. attendance, participation). Coerce null
                // to 0 so the row still imports but contributes nothing to a
                // weighted rollup. Non-null values are rounded to the nearest
                // whole mark because Assignment.MaxMark is stored as int?.
                var maxMark = (int)Math.Round(remoteAssignment.MaxMarks ?? 0d);

                if (!existingAssignments.TryGetValue(remoteAssignment.Id, out var assignment))
                {
                    assignment = new Assignment
                    {
                        Name = remoteAssignment.Name,
                        DueAt = remoteAssignment.DueAt,
                        UpdatedAt = remoteAssignment.UpdatedAt,
                        MaxMark = maxMark,
                        CanvasAssignmentId = remoteAssignment.Id,
                        CanvasWorkflowState = remoteAssignment.WorkflowState,
                        CanvasSubmissionState = remoteAssignment.Submission?.WorkflowState,
                        CanvasIsActive = true,
                        CourseId = course.CourseId,
                        GroupId = group.GroupId
                    };
                    db.Assignments.Add(assignment);
                    existingAssignments[remoteAssignment.Id] = assignment;
                    assignmentsCreated++;
                }
                else
                {
                    assignment.Name = remoteAssignment.Name;
                    assignment.DueAt = remoteAssignment.DueAt;
                    assignment.UpdatedAt = remoteAssignment.UpdatedAt;
                    assignment.MaxMark = maxMark;
                    assignment.CanvasWorkflowState = remoteAssignment.WorkflowState;
                    assignment.CanvasSubmissionState = remoteAssignment.Submission?.WorkflowState;
                    assignment.CanvasIsActive = true;
                    assignment.CourseId = course.CourseId;
                    assignment.GroupId = group.GroupId;
                    assignmentsUpdated++;
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

        foreach (var (canvasAssignmentId, assignment) in existingAssignments)
        {
            if (!seenAssignmentIds.Contains(canvasAssignmentId) && assignment.CanvasIsActive != false)
            {
                assignment.CanvasIsActive = false;
                assignmentsDeactivated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CanvasSyncResultDto(
            coursesCreated,
            coursesUpdated,
            coursesDeactivated,
            assignmentGroupsCreated,
            assignmentGroupsUpdated,
            assignmentGroupsDeactivated,
            assignmentsCreated,
            assignmentsUpdated,
            assignmentsDeactivated);
    }

    private sealed record CourseSnapshot(
        SharedCanvasCourseDto Course,
        IReadOnlyList<SharedCanvasAssignmentDto> Assignments,
        IReadOnlyList<SharedCanvasAssignmentGroupDto> AssignmentGroups);
}
