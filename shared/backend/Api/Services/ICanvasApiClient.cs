using Api.DTOs;

namespace Api.Services;

public interface ICanvasApiClient
{
    Task<IReadOnlyList<CanvasCourseDto>> GetCoursesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CanvasAssignmentDto>> GetAssignmentsAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CanvasUserDto>> GetUsersForCourseAsync(
        long courseId,
        CancellationToken cancellationToken);
}
