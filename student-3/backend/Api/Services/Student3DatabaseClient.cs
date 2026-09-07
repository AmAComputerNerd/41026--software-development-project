using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.DTOs;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Student3.Contracts;

namespace Api.Services;

public sealed class Student3DatabaseClient(HttpClient httpClient) : IStudent3DatabaseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<TaskRecord>> GetTasksAsync(
        TaskFilterDto filter,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["status"] = filter.Status,
            ["priority"] = filter.Priority,
            ["courseId"] = filter.CourseId?.ToString(),
            ["parentTaskId"] = filter.ParentTaskId?.ToString(),
            ["overdue"] = filter.Overdue?.ToString(CultureInfo.InvariantCulture),
            ["includeInactiveCanvas"] =
                filter.IncludeInactiveCanvas?.ToString(CultureInfo.InvariantCulture)
        };

        return GetListAsync<TaskRecord>(
            BuildUri("internal/tasks/", parameters),
            cancellationToken);
    }

    public Task<TaskRecord?> GetTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetOptionalAsync<TaskRecord>($"internal/tasks/{id}", cancellationToken);
    }

    public Task<TaskRecord> CreateTaskAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken)
    {
        return SendRequiredAsync<TaskRecord>(
            HttpMethod.Post,
            "internal/tasks/",
            command,
            cancellationToken);
    }

    public Task<TaskRecord?> UpdateTaskAsync(
        Guid id,
        UpdateTaskCommand command,
        CancellationToken cancellationToken)
    {
        return SendOptionalAsync<TaskRecord>(
            HttpMethod.Put,
            $"internal/tasks/{id}",
            command,
            cancellationToken);
    }

    public async Task<bool> DeleteTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"internal/tasks/{id}"),
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    public Task<IReadOnlyList<TaskRecord>?> CreateSubtasksAsync(
        Guid parentId,
        CreateSubtasksCommand command,
        CancellationToken cancellationToken)
    {
        return SendOptionalAsync<IReadOnlyList<TaskRecord>>(
            HttpMethod.Post,
            $"internal/tasks/{parentId}/subtasks",
            command,
            cancellationToken);
    }

    public Task<IReadOnlyList<CourseRecord>> GetCoursesAsync(
        bool includeInactiveCanvas,
        CancellationToken cancellationToken)
    {
        var path = $"internal/courses/?includeInactiveCanvas={includeInactiveCanvas.ToString(CultureInfo.InvariantCulture)}";
        return GetListAsync<CourseRecord>(path, cancellationToken);
    }

    public Task<CourseRecord?> GetCourseAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetOptionalAsync<CourseRecord>($"internal/courses/{id}", cancellationToken);
    }

    public Task<CanvasSyncResultRecord> ApplyCanvasSnapshotAsync(
        CanvasSnapshotCommand command,
        CancellationToken cancellationToken)
    {
        return SendRequiredAsync<CanvasSyncResultRecord>(
            HttpMethod.Post,
            "internal/canvas-snapshots",
            command,
            cancellationToken);
    }

    public Task<IReadOnlyList<TaskRecord>> GetDueRemindersAsync(
        int hoursBeforeDue,
        int finalHoursBeforeDue,
        CancellationToken cancellationToken)
    {
        var path = "internal/reminders/due" +
            $"?hoursBeforeDue={hoursBeforeDue.ToString(CultureInfo.InvariantCulture)}" +
            $"&finalHoursBeforeDue={finalHoursBeforeDue.ToString(CultureInfo.InvariantCulture)}";
        return GetListAsync<TaskRecord>(path, cancellationToken);
    }

    public async Task MarkReminderSentAsync(
        Guid id,
        DateTime sentAtUtc,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"internal/reminders/{id}/sent")
        {
            Content = JsonContent.Create(new MarkReminderSentCommand(sentAtUtc), options: JsonOptions)
        };
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Get, path),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<T>>(
            JsonOptions,
            cancellationToken) ?? [];
    }

    private async Task<T?> GetOptionalAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Get, path),
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<T>(response, cancellationToken);
    }

    private async Task<T> SendRequiredAsync<T>(
        HttpMethod method,
        string path,
        object body,
        CancellationToken cancellationToken)
    {
        return await SendOptionalAsync<T>(method, path, body, cancellationToken)
            ?? throw new DatabaseServiceException("The database service returned no response body.");
    }

    private async Task<T?> SendOptionalAsync<T>(
        HttpMethod method,
        string path,
        object body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<T>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new DatabaseServiceException(
                "The database service could not be reached.",
                innerException: exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DatabaseServiceException(
                "The database service request timed out.",
                innerException: exception);
        }
        catch (TimeoutRejectedException exception)
        {
            throw new DatabaseServiceException(
                "The database service request timed out.",
                innerException: exception);
        }
        catch (BrokenCircuitException exception)
        {
            throw new DatabaseServiceException(
                "The database service is temporarily unavailable.",
                innerException: exception);
        }
    }

    private static async Task<T> ReadRequiredAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(
                JsonOptions,
                cancellationToken)
                ?? throw new DatabaseServiceException(
                    "The database service returned an empty response.");
        }
        catch (JsonException exception)
        {
            throw new DatabaseServiceException(
                "The database service returned an unreadable response.",
                response.StatusCode,
                exception);
        }
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = $"The database service returned HTTP {(int)response.StatusCode}.";
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                message = JsonSerializer.Deserialize<string>(content, JsonOptions) ?? message;
            }
            catch (JsonException)
            {
                message = content;
            }
        }

        throw new DatabaseServiceException(message, response.StatusCode);
    }

    private static string BuildUri(
        string path,
        IReadOnlyDictionary<string, string?> parameters)
    {
        var query = string.Join(
            "&",
            parameters
                .Where(item => item.Value is not null)
                .Select(item =>
                    $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}"));
        return query.Length == 0 ? path : $"{path}?{query}";
    }
}
