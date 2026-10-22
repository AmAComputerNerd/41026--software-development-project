using Api.DTOs;
using Api.Services;
using Student4.Contracts;

namespace Api.Endpoints;

public static class ProfileSummaryEndpoints
{
    public static IEndpointRouteBuilder MapProfileSummaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users");

        group.MapPost("/{userId:guid}/profile-summary", GenerateProfileSummary);
        group.MapPost("/{userId:guid}/profile-summary/{workflowId:guid}/review", ReviewProfileSummary);
        group.MapPost("/{userId:guid}/profile-summary/{workflowId:guid}/revise", ReviseProfileSummary);

        return endpoints;
    }

    private static async Task<IResult> GenerateProfileSummary(
        Guid userId,
        IAccountDatabaseClient db,
        ProfileSummaryWorkflowService workflowService,
        IMultiAgentClient multiAgent,
        INotificationClient notificationClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!multiAgent.IsEnabled)
        {
            return Results.Problem(
                title: "Multi-Agent Service Disabled",
                detail: "Multi-Agent profile review is disabled for this environment.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

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

            var proposal = await workflowService.GenerateAsync(user, student, teacher, cancellationToken);

            try
            {
                await notificationClient.PushAsync(new PushNotificationDto(
                    StudentId: userId,
                    Type: "AI",
                    SourceMicroservice: "account",
                    Message: "New AI profile summary proposal generated.",
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
                newSummary = proposal.NewSummary,
                workflow = proposal.Workflow,
            });
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
        catch (MultiAgentServiceException ex)
        {
            return Results.Problem(detail: ex.Message,
                statusCode: ex.StatusCode == System.Net.HttpStatusCode.Conflict
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> ReviewProfileSummary(
        Guid userId,
        Guid workflowId,
        MultiAgentReviewRequestDto request,
        ProfileSummaryWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        if (request.Decision is not ("Approve" or "Reject") ||
            string.IsNullOrWhiteSpace(request.ReviewerName) || request.ReviewerName.Length > 100 ||
            string.IsNullOrWhiteSpace(request.Comments) || request.Comments.Length > 2000 ||
            request.ExpectedRevision is null or < 0)
        {
            return Results.BadRequest("Supply Approve or Reject, a reviewer name, comments, and the current revision.");
        }

        try
        {
            var result = await workflowService.ReviewAsync(userId, workflowId, request, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (MultiAgentServiceException exception)
        {
            return Results.Problem(detail: exception.Message,
                statusCode: exception.StatusCode == System.Net.HttpStatusCode.Conflict
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> ReviseProfileSummary(
        Guid userId,
        Guid workflowId,
        MultiAgentRevisionRequestDto request,
        ProfileSummaryWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        if (request.RequestId == Guid.Empty || request.ExpectedRevision < 0 ||
            string.IsNullOrWhiteSpace(request.ReviewerName) || request.ReviewerName.Length > 100 ||
            string.IsNullOrWhiteSpace(request.Feedback) || request.Feedback.Length > 2000)
        {
            return Results.BadRequest("Supply a request ID, current revision, reviewer name, and feedback.");
        }

        try
        {
            var result = await workflowService.ReviseAsync(userId, workflowId, request, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (MultiAgentServiceException exception)
        {
            return Results.Problem(detail: exception.Message,
                statusCode: exception.StatusCode == System.Net.HttpStatusCode.Conflict
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status502BadGateway);
        }
    }
}

internal static partial class ProfileSummaryEndpointsLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to push notification for profile summary of user {UserId}.")]
    public static partial void PushNotificationFailed(ILogger logger, Guid userId, Exception ex);
}
