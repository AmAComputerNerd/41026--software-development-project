using Api.Data;
using Api.DTOs;
using Api.Models;

namespace Api.Services;

public sealed class CanvasFacade(
    ICanvasApiClient canvasApiClient,
    AppDbContext db)
{
    public Task<IReadOnlyList<CanvasCourseDto>> GetCoursesAsync(
        CancellationToken cancellationToken)
    {
        return ExecuteLoggedAsync(
            "GetCourses",
            canvasApiClient.GetCoursesAsync,
            cancellationToken);
    }

    public Task<IReadOnlyList<CanvasAssignmentDto>> GetAssignmentsAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return ExecuteLoggedAsync(
            $"GetAssignments:{courseId}",
            token => canvasApiClient.GetAssignmentsAsync(courseId, token),
            cancellationToken);
    }

    public Task<IReadOnlyList<CanvasUserDto>> GetUsersForCourseAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return ExecuteLoggedAsync(
            $"GetUsersForCourse:{courseId}",
            token => canvasApiClient.GetUsersForCourseAsync(courseId, token),
            cancellationToken);
    }

    private async Task<IReadOnlyList<T>> ExecuteLoggedAsync<T>(
        string operation,
        Func<CancellationToken, Task<IReadOnlyList<T>>> action,
        CancellationToken cancellationToken)
    {
        var log = new CanvasRequestLog
        {
            Operation = operation,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            var result = await action(cancellationToken);
            log.Succeeded = true;
            log.ItemCount = result.Count;
            return result;
        }
        catch (CanvasApiException exception)
        {
            log.UpstreamStatusCode = (int)exception.StatusCode;
            throw;
        }
        catch (HttpRequestException exception)
        {
            log.UpstreamStatusCode = exception.StatusCode is null
                ? null
                : (int)exception.StatusCode.Value;
            throw;
        }
        finally
        {
            log.CompletedAt = DateTime.UtcNow;
            db.CanvasRequestLogs.Add(log);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
