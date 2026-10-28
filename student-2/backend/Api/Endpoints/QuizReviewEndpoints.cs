using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class QuizReviewEndpoints
{
    public static IEndpointRouteBuilder MapQuizReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/automation-runs/{id:guid}/quiz-review");
        group.MapGet("/", GetReview);
        group.MapPost("/", SubmitReview);
        group.MapPost("/revise", ReviseReview);
        return endpoints;
    }

    private static async Task<IResult> GetReview(
        [FromRoute] Guid id,
        QuizAnswerReviewService review,
        CancellationToken cancellationToken)
    {
        var result = await review.GetReviewAsync(id, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static Task<IResult> SubmitReview(
        [FromRoute] Guid id,
        [FromBody] SubmitQuizReviewRequestDto request,
        QuizAnswerReviewService review,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => review.SubmitReviewAsync(id, request, cancellationToken));

    private static Task<IResult> ReviseReview(
        [FromRoute] Guid id,
        [FromBody] ReviseQuizReviewRequestDto request,
        QuizAnswerReviewService review,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => review.ReviseAsync(id, request, cancellationToken));

    private static async Task<IResult> ExecuteAsync(Func<Task<QuizReviewDto?>> action)
    {
        try
        {
            var result = await action();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (QuizReviewValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["review"] = [exception.Message]
            });
        }
        catch (Exception exception) when (exception is QuizReviewConflictException or MultiAgentConflictException)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: exception.Message);
        }
        catch (MultiAgentIntegrationDisabledException exception)
        {
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: exception.Message);
        }
        catch (Exception exception) when (exception is MultiAgentServiceException or QuizSubmissionFailedException)
        {
            return Results.Problem(statusCode: StatusCodes.Status502BadGateway, title: exception.Message);
        }
    }
}
