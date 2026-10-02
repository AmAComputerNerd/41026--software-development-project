using System.Text.Json;
using Api.DTOs;
using Student4.Contracts;

namespace Api.Services;

public sealed class AccountReadinessService(
    IAccountDatabaseClient database,
    IHttpClientFactory httpClientFactory,
    ILogger<AccountReadinessService> logger)
{
    public const string CanvasClientName = "account-readiness-canvas";
    public const string NotificationsClientName = "account-readiness-notifications";

    public async Task<AccountReadinessDto?> CheckAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await database.GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var role = await CheckRoleAsync(user, cancellationToken);
        var canvas = CheckDependencyAsync(
            CanvasClientName, "api/canvas/courses", "canvas", "Canvas connectivity",
            "The Canvas gateway successfully retrieved courses. This does not verify your account's enrolments.",
            "Canvas features are unavailable. Check the Canvas gateway configuration and retry.",
            cancellationToken);
        var notifications = CheckDependencyAsync(
            NotificationsClientName, "health/ready", "notifications", "Account notifications",
            "The notification service is ready. This does not verify delivery preferences or email delivery.",
            "Account alerts may not be delivered. Start the notification service and retry.",
            cancellationToken);
        var checks = new List<AccountReadinessCheckDto>
        {
            CheckProfile(user),
            role,
            await canvas,
            await notifications
        };
        var overallStatus = checks.Any(check => check.Status == "unavailable")
            ? "unavailable"
            : checks.Any(check => check.Status == "needs_attention")
                ? "needs_attention"
                : "ready";
        return new AccountReadinessDto(user.Id, overallStatus, DateTime.UtcNow, checks);
    }

    private static AccountReadinessCheckDto CheckProfile(UserRecord user)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(user.FirstName)) missing.Add("first name");
        if (string.IsNullOrWhiteSpace(user.LastName)) missing.Add("last name");
        if (string.IsNullOrWhiteSpace(user.Email)) missing.Add("email address");
        if (string.IsNullOrWhiteSpace(user.UserProfile)) missing.Add("profile summary");

        return missing.Count == 0
            ? new("profile", "Profile completeness", "ready", "Your basic profile details and summary are filled in.", null)
            : new("profile", "Profile completeness", "needs_attention",
                $"Your profile is missing: {string.Join(", ", missing)}. Review your profile to complete it.", "edit_profile");
    }

    private async Task<AccountReadinessCheckDto> CheckRoleAsync(
        UserRecord user,
        CancellationToken cancellationToken)
    {
        string? roleStatus;
        switch (user.UserType)
        {
            case "Student":
                roleStatus = (await database.GetStudentAsync(user.Id, cancellationToken))?.CourseStatus;
                break;
            case "Teacher":
                roleStatus = (await database.GetTeacherAsync(user.Id, cancellationToken))?.EmploymentStatus;
                break;
            case "Admin":
                return new("role", "Role configuration", "ready", "Admin accounts do not require a student or teacher record.", null);
            default:
                return new("role", "Role configuration", "needs_attention", "Your account role is not recognised. Contact an administrator.", null);
        }

        return roleStatus switch
        {
            null => new("role", "Role configuration", "needs_attention",
                $"Your {user.UserType.ToLowerInvariant()} record is missing. Contact an administrator to complete account setup.", null),
            "Inactive" => new("role", "Role configuration", "needs_attention",
                $"Your {user.UserType.ToLowerInvariant()} status is inactive. Review your role settings if this is no longer correct.", "edit_profile"),
            "FullTime" or "PartTime" => new("role", "Role configuration", "ready",
                $"Your {user.UserType.ToLowerInvariant()} record has an active status.", null),
            _ => new("role", "Role configuration", "needs_attention",
                "Your role status is not recognised. Contact an administrator.", null)
        };
    }

    private async Task<AccountReadinessCheckDto> CheckDependencyAsync(
        string clientName,
        string path,
        string code,
        string label,
        string readyMessage,
        string unavailableMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = httpClientFactory.CreateClient(clientName);
            using var response = await client.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                AccountReadinessLog.DependencyFailed(logger, code, (int)response.StatusCode);
                return new(code, label, "unavailable", unavailableMessage, null);
            }

            if (code == "canvas")
            {
                var courses = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                if (courses.ValueKind != JsonValueKind.Array)
                {
                    AccountReadinessLog.InvalidCanvasResponse(logger);
                    return new(code, label, "unavailable", unavailableMessage, null);
                }
            }

            return new(code, label, "ready", readyMessage, null);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            AccountReadinessLog.DependencyUnavailable(logger, code, exception);
            return new(code, label, "unavailable", unavailableMessage, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or IOException)
        {
            AccountReadinessLog.DependencyUnavailable(logger, code, exception);
            return new(code, label, "unavailable", unavailableMessage, null);
        }
    }
}

internal static partial class AccountReadinessLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Account readiness dependency {Dependency} returned HTTP {StatusCode}.")]
    public static partial void DependencyFailed(ILogger logger, string dependency, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account readiness dependency {Dependency} could not be checked.")]
    public static partial void DependencyUnavailable(ILogger logger, string dependency, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account readiness Canvas gateway returned an invalid course list.")]
    public static partial void InvalidCanvasResponse(ILogger logger);
}
