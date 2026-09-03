using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Services;

public sealed class CanvasFacade(
    ICanvasApiClient canvasApiClient,
    AppDbContext db,
    IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(3);

    public Task<IReadOnlyList<CanvasCourseDto>> GetCoursesAsync(
        CancellationToken cancellationToken)
    {
        return GetOrAddAsync(
            "canvas:courses",
            () => ExecuteLoggedAsync(
                "GetCourses",
                canvasApiClient.GetCoursesAsync,
                cancellationToken));
    }

    public Task<IReadOnlyList<CanvasAssignmentDto>> GetAssignmentsAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return GetOrAddAsync(
            $"canvas:assignments:{courseId}",
            () => ExecuteLoggedAsync(
                $"GetAssignments:{courseId}",
                token => canvasApiClient.GetAssignmentsAsync(courseId, token),
                cancellationToken));
    }

    public Task<IReadOnlyList<CanvasAssignmentGroupDto>> GetAssignmentGroupAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return GetOrAddAsync(
            $"canvas:assignment-groups:{courseId}",
            () => ExecuteLoggedAsync(
                $"GetAssignmentGroups:{courseId}",
                token => canvasApiClient.GetAssignmentGroupAsync(courseId, token),
                cancellationToken));
    }

    public Task<IReadOnlyList<CanvasUserDto>> GetUsersForCourseAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return GetOrAddAsync(
            $"canvas:users:{courseId}",
            () => ExecuteLoggedAsync(
                $"GetUsersForCourse:{courseId}",
                token => canvasApiClient.GetUsersForCourseAsync(courseId, token),
                cancellationToken));
    }

    public async Task<CanvasUserDto> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        // Not cached: the active user can change between requests (token rotation,
        // re-auth) and a stale value would silently pin the UI to the wrong
        // student.
        var log = new CanvasRequestLog
        {
            Operation = "GetCurrentUser",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            var user = await canvasApiClient.GetCurrentUserAsync(cancellationToken);
            log.Succeeded = true;
            log.ItemCount = 1;
            return user;
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

    private async Task<IReadOnlyList<T>> GetOrAddAsync<T>(
        string cacheKey,
        Func<Task<IReadOnlyList<T>>> action)
    {
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<T>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await action();
        cache.Set(cacheKey, result, CacheTtl);
        return result;
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
