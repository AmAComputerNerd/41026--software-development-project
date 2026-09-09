using Api.Services;
using Student4.Contracts;

namespace Api.Endpoints;

public static class ProfileSummaryEndpoints
{
    public static IEndpointRouteBuilder MapProfileSummaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users");

        group.MapPost("/{userId:guid}/profile-summary", GenerateProfileSummary);

        return endpoints;
    }

    private static async Task<IResult> GenerateProfileSummary(
        Guid userId,
        IAccountDatabaseClient db,
        IAiProfileSummaryService aiService,
        INotificationClient notificationClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await db.GetUserAsync(userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound();
            }

            StudentRecord? student = null;
            TeacherRecord? teacher = null;
            if (user.UserType == "Student")
            {
                student = await db.GetStudentAsync(userId, cancellationToken);
            }
            else if (user.UserType == "Teacher")
            {
                teacher = await db.GetTeacherAsync(userId, cancellationToken);
            }

            var summary = await aiService.GenerateSummaryAsync(
                user,
                student,
                teacher,
                cancellationToken);

            try
            {
                await notificationClient.PushAsync(new PushNotificationDto(
                    StudentId: userId,
                    Type: "AI",
                    SourceMicroservice: "account",
                    Message: "New AI profile summary generated.",
                    RelatedEntityType: "User",
                    RelatedEntityId: userId), cancellationToken);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger("Api.Endpoints.ProfileSummaryEndpoints");
                ProfileSummaryEndpointsLog.PushNotificationFailed(logger, userId, ex);
            }

            return Results.Ok(new
            {
                oldSummary = user.UserProfile,
                newSummary = summary,
            });
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
        catch (AiGatewayException ex)
        {
            return Results.Json(
                new
                {
                    error = ex.Message,
                    upstreamStatusCode = ex.UpstreamStatusCode,
                    rateLimitReset = ex.RateLimitReset,
                },
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}

internal static partial class ProfileSummaryEndpointsLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to push notification for profile summary of user {UserId}.")]
    public static partial void PushNotificationFailed(ILogger logger, Guid userId, Exception ex);
}
