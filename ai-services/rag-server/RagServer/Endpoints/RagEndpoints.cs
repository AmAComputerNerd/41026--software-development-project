using RagServer.Models;
using RagServer.Services;

namespace RagServer.Endpoints;

public static class RagEndpoints
{
    public static IEndpointRouteBuilder MapRagEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/answers", Answer);
        return endpoints;
    }

    private static async Task<IResult> Answer(
        RagAnswerRequest request,
        GroundedAnswerService answerService,
        CancellationToken cancellationToken)
    {
        var question = request.Question?.Trim() ?? string.Empty;
        if (question.Length is < 3 or > 500)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["question"] = ["Question must be between 3 and 500 characters."]
            });
        }

        if (!string.Equals(request.Scope, "student-3", StringComparison.Ordinal))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["scope"] = ["Only the student-3 knowledge scope is currently available."]
            });
        }

        try
        {
            return Results.Ok(await answerService.AnswerAsync(question, cancellationToken));
        }
        catch (RagGenerationException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "The grounded answer could not be generated.");
        }
        catch (HttpRequestException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "The AI gateway could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "The grounded answer request timed out.");
        }
    }
}
