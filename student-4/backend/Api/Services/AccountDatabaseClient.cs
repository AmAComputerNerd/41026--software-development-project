using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Student4.Contracts;

namespace Api.Services;

// HTTP client for the student-4-database service, scoped to the profile-CRUD surface.
public sealed class AccountDatabaseClient(HttpClient httpClient) : IAccountDatabaseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<UserRecord>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return GetListAsync<UserRecord>("internal/users/", cancellationToken);
    }

    public Task<UserRecord?> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetOptionalAsync<UserRecord>($"internal/users/{id}", cancellationToken);
    }

    public Task<UserRecord> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        return SendRequiredAsync<UserRecord>(HttpMethod.Post, "internal/users/", command, cancellationToken);
    }

    public Task<UserRecord?> UpdateUserAsync(Guid id, UpdateUserCommand command, CancellationToken cancellationToken)
    {
        return SendOptionalAsync<UserRecord>(HttpMethod.Put, $"internal/users/{id}", command, cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"internal/users/{id}"),
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    public Task<StudentRecord?> GetStudentAsync(Guid userId, CancellationToken cancellationToken)
    {
        return GetOptionalAsync<StudentRecord>($"internal/students/{userId}", cancellationToken);
    }

    public Task<StudentRecord> UpdateStudentAsync(Guid userId, UpdateStudentCommand command, CancellationToken cancellationToken)
    {
        return SendRequiredAsync<StudentRecord>(HttpMethod.Put, $"internal/students/{userId}", command, cancellationToken);
    }

    public Task<TeacherRecord?> GetTeacherAsync(Guid userId, CancellationToken cancellationToken)
    {
        return GetOptionalAsync<TeacherRecord>($"internal/teachers/{userId}", cancellationToken);
    }

    public Task<TeacherRecord> UpdateTeacherAsync(Guid userId, UpdateTeacherCommand command, CancellationToken cancellationToken)
    {
        return SendRequiredAsync<TeacherRecord>(HttpMethod.Put, $"internal/teachers/{userId}", command, cancellationToken);
    }

    public Task<UserRecord?> UpdateProfileSummaryAsync(Guid userId, ProfileSummaryCommand command, CancellationToken cancellationToken)
    {
        return SendOptionalAsync<UserRecord>(HttpMethod.Post, $"internal/users/{userId}/profile-summary", command, cancellationToken);
    }

    #region Helpers
    private async Task<IReadOnlyList<T>> GetListAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Get, path),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<T>>(JsonOptions, cancellationToken) ?? [];
    }

    private async Task<T?> GetOptionalAsync<T>(string path, CancellationToken cancellationToken)
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

    private async Task<T> SendRequiredAsync<T>(HttpMethod method, string path, object body, CancellationToken cancellationToken)
    {
        return await SendOptionalAsync<T>(method, path, body, cancellationToken)
            ?? throw new DatabaseServiceException("The database service returned no response body.");
    }

    private async Task<T?> SendOptionalAsync<T>(HttpMethod method, string path, object body, CancellationToken cancellationToken)
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

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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
            throw new DatabaseServiceException("The database service could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DatabaseServiceException("The database service request timed out.", exception);
        }
        catch (TimeoutRejectedException exception)
        {
            throw new DatabaseServiceException("The database service request timed out.", exception);
        }
        catch (BrokenCircuitException exception)
        {
            throw new DatabaseServiceException("The database service is temporarily unavailable.", exception);
        }
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                ?? throw new DatabaseServiceException("The database service returned an empty response.");
        }
        catch (JsonException exception)
        {
            throw new DatabaseServiceException("The database service returned a malformed response.", exception);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new DatabaseServiceException(
            $"The database service returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }
    #endregion
}
