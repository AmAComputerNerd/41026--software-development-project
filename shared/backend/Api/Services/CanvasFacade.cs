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

    public Task<IReadOnlyList<CanvasRecipientDto>> FindRecipientsAsync(
        long courseId,
        string? search,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = search?.Trim() ?? string.Empty;
        return GetOrAddAsync(
            $"canvas:recipients:{courseId}:{normalizedSearch}",
            () => ExecuteLoggedAsync(
                $"FindRecipients:{courseId}",
                token => canvasApiClient.FindRecipientsAsync(
                    courseId,
                    normalizedSearch,
                    token),
                cancellationToken));
    }

    public Task<IReadOnlyList<CanvasConversationDto>> CreateConversationAsync(
        CreateCanvasConversationDto request,
        CancellationToken cancellationToken)
    {
        return ExecuteLoggedAsync(
            "CreateConversation",
            token => canvasApiClient.CreateConversationAsync(request, token),
            cancellationToken);
    }

    public Task<IReadOnlyList<CanvasQuizDto>> GetQuizzesAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return GetOrAddAsync(
            $"canvas:quizzes:{courseId}",
            () => ExecuteLoggedAsync(
                $"GetQuizzes:{courseId}",
                token => canvasApiClient.GetQuizzesAsync(courseId, token),
                cancellationToken));
    }

    public Task<CanvasQuizSubmissionDto> StartQuizSubmissionAsync(
        long courseId,
        long quizId,
        CancellationToken cancellationToken)
    {
        return ExecuteLoggedScalarAsync(
            $"StartQuizSubmission:{quizId}",
            token => canvasApiClient.StartQuizSubmissionAsync(courseId, quizId, token),
            cancellationToken);
    }

    public Task<IReadOnlyList<CanvasQuizQuestionDto>> GetQuizSubmissionQuestionsAsync(
        long quizSubmissionId,
        CancellationToken cancellationToken)
    {
        return ExecuteLoggedAsync(
            $"GetQuizSubmissionQuestions:{quizSubmissionId}",
            token => canvasApiClient.GetQuizSubmissionQuestionsAsync(quizSubmissionId, token),
            cancellationToken);
    }

    public Task AnswerQuizSubmissionQuestionsAsync(
        long quizSubmissionId,
        AnswerCanvasQuizQuestionsDto request,
        CancellationToken cancellationToken)
    {
        return ExecuteLoggedScalarAsync(
            $"AnswerQuizSubmissionQuestions:{quizSubmissionId}",
            async token =>
            {
                await canvasApiClient.AnswerQuizSubmissionQuestionsAsync(
                    quizSubmissionId,
                    request,
                    token);
                return true;
            },
            cancellationToken);
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

    private async Task<T> ExecuteLoggedScalarAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> action,
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
            log.ItemCount = 1;
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
