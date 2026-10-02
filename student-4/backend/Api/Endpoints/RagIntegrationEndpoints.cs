using Api.DTOs;
using Api.Services;

namespace Api.Endpoints;

public static class RagIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapRagIntegrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/users/help/answers", Answer);
        return endpoints;
    }

    private static async Task<IResult> Answer(
        RagQuestionRequestDto request,
        RagClient ragClient,
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

        return Results.Ok(await ragClient.AnswerAsync(question, cancellationToken));
    }
}
