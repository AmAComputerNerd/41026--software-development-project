using System.ComponentModel;
using System.Text.Json;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public sealed class AccountTools(IAccountClient accountClient)
{
    [McpServerTool(Name = "accounts_check_readiness", UseStructuredContent = true)]
    [Description("Checks one Student 4 account's profile completeness, role setup, Canvas gateway connectivity and notification service readiness. Returns actionable findings, not personal details. No writes or credentials.")]
    public async Task<AccountToolResult> CheckReadinessAsync(
        [Description("The non-empty UUID of the account to read.")] string userId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var id) || id == Guid.Empty)
        {
            return AccountToolResult.Failure("invalid_user_id", "A non-empty account UUID is required.");
        }

        try
        {
            var readiness = await accountClient.CheckReadinessAsync(id, cancellationToken);
            return readiness is null
                ? AccountToolResult.Failure("account_not_found", "The account could not be found.")
                : new("success", "accounts_check_readiness", readiness, null);
        }
        catch (HttpRequestException)
        {
            return AccountToolResult.Failure("student4_unavailable", "The Student 4 account service could not be reached.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AccountToolResult.Failure("student4_timeout", "The Student 4 account service timed out.");
        }
        catch (Exception exception) when (exception is AccountClientException or JsonException)
        {
            return AccountToolResult.Failure("student4_invalid_response", "The Student 4 account service returned an invalid response.");
        }
    }
}

public sealed record AccountToolResult(
    string Status,
    string Tool,
    AccountReadinessData? Data,
    AccountToolError? Error)
{
    public static AccountToolResult Failure(string code, string message) =>
        new("error", "accounts_check_readiness", null, new(code, message));
}

public sealed record AccountToolError(string Code, string Message);
