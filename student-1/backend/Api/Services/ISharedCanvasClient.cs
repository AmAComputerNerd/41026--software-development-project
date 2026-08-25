using Api.DTOs;

namespace Api.Services;

public interface ISharedCanvasClient
{
    Task<IReadOnlyList<SharedCanvasCourseDto>> GetCoursesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SharedCanvasAssignmentDto>> GetAssignmentsAsync(
        long courseId,
        CancellationToken cancellationToken);
}
