using GradesManager.DTOs;

namespace GradesManager.Services;

public interface ISharedCanvasClient
{
    Task<IReadOnlyList<SharedCanvasCourseDto>> GetCoursesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SharedCanvasAssignmentDto>> GetAssignmentsAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SharedCanvasAssignmentGroupDto>> GetAssignmentGroupsAsync(
        long courseId,
        CancellationToken cancellationToken);
}
