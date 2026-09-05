using Api.DTOs;
using Student3.Contracts;

namespace Api.Services;

public sealed class CanvasSyncOrchestrator(
    ISharedCanvasClient canvasClient,
    IStudent3DatabaseClient database)
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    public async Task<CanvasSyncResultDto> SyncAsync(CancellationToken cancellationToken)
    {
        await SyncLock.WaitAsync(cancellationToken);
        try
        {
            var remoteCourses = await canvasClient.GetCoursesAsync(cancellationToken);
            var snapshots = new List<CanvasCourseSnapshot>(remoteCourses.Count);

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

                snapshots.Add(new CanvasCourseSnapshot(
                    course.Id,
                    course.Name,
                    course.CourseCode,
                    course.WorkflowState,
                    assignments.Select(assignment => new CanvasAssignmentSnapshot(
                        assignment.Id,
                        assignment.CourseId,
                        assignment.Name,
                        assignment.Description,
                        assignment.DueAt,
                        assignment.UpdatedAt,
                        assignment.WorkflowState,
                        assignment.Submission?.WorkflowState,
                        IsSubmitted(assignment.Submission))).ToList()));
            }

            ValidateSnapshot(snapshots);
            var result = await database.ApplyCanvasSnapshotAsync(
                new CanvasSnapshotCommand(snapshots),
                cancellationToken);

            return new CanvasSyncResultDto(
                result.CoursesCreated,
                result.CoursesUpdated,
                result.CoursesDeactivated,
                result.TasksCreated,
                result.TasksUpdated,
                result.TasksDeactivated);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private static void ValidateSnapshot(IReadOnlyCollection<CanvasCourseSnapshot> snapshots)
    {
        var duplicateCourseId = snapshots
            .GroupBy(snapshot => snapshot.Id)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateCourseId is not null)
        {
            throw new SharedServiceException(
                $"The shared service returned duplicate Canvas course ID {duplicateCourseId}.");
        }

        var duplicateAssignmentId = snapshots
            .SelectMany(snapshot => snapshot.Assignments)
            .GroupBy(assignment => assignment.Id)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateAssignmentId is not null)
        {
            throw new SharedServiceException(
                $"The shared service returned duplicate Canvas assignment ID {duplicateAssignmentId}.");
        }
    }

    private static bool IsSubmitted(SharedCanvasSubmissionDto? submission)
    {
        return submission?.WorkflowState is "submitted" or "graded";
    }
}
