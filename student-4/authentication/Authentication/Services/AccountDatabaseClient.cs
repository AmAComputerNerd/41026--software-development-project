using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Student4.Contracts;

namespace Authentication.Services;

// HTTP implementation of IAccountDatabaseClient scoped to the auth surface.
public sealed class AccountDatabaseClient(HttpClient httpClient) : IAccountDatabaseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<UserRecord>> GetUsersAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "internal/users/"),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<UserRecord>>(JsonOptions, cancellationToken)
            ?? new List<UserRecord>();
    }

    public Task<UserRecord?> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        return SendOptionalAsync<UserRecord>(HttpMethod.Post, "internal/auth/login", command, cancellationToken);
    }

    public async Task<UserRecord?> LoginWithStatusAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "internal/auth/login")
            {
                Content = JsonContent.Create(command, options: JsonOptions)
            },
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRequiredAsync<UserRecord>(response, cancellationToken);
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "internal/auth/change-password")
            {
                Content = JsonContent.Create(command, options: JsonOptions)
            },
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return false;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAccountAsync(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, "internal/auth/delete-account")
            {
                Content = JsonContent.Create(command, options: JsonOptions)
            },
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return false;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    public Task<PasswordResetTokenRecord?> CreatePasswordResetTokenAsync(
        CreatePasswordResetTokenCommand command,
        CancellationToken cancellationToken)
    {
        return SendRequiredAsync<PasswordResetTokenRecord>(
            HttpMethod.Post,
            "internal/password-reset-tokens",
            command,
            cancellationToken);
    }

    public Task<UserRecord?> ResetPasswordWithTokenAsync(
        ResetPasswordWithTokenCommand command,
        CancellationToken cancellationToken)
    {
        return SendOptionalAsync<UserRecord>(
            HttpMethod.Post,
            "internal/password-reset-tokens/redeem",
            command,
            cancellationToken);
    }

    #region Helpers
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

    private static async Task<T> ReadRequiredAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
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

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
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
