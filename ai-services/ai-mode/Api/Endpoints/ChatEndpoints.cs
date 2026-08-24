using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace Api.Endpoints;

public static class ChatEndpoints
{
    private const string DefaultModel = "nvidia/nemotron-3-ultra-550b-a55b:free";
    private const string OpenRouterEndpoint = "https://openrouter.ai/api/v1/chat/completions";

    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/chat/completions", PostChatCompletions);
        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        return endpoints;
    }

    private static async Task<IResult> PostChatCompletions(
        HttpRequest httpRequest,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("ChatEndpoints");

        var apiKey = configuration["OpenRouter:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogError("{Timestamp:O} OpenRouter:ApiKey is not configured", DateTime.UtcNow);
            return Results.Problem("OpenRouter:ApiKey configuration value is not set.", statusCode: StatusCodes.Status500InternalServerError);
        }

        JsonNode? body;
        try
        {
            body = await JsonNode.ParseAsync(httpRequest.Body, cancellationToken: cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            return Results.BadRequest(new { error = "Request body must be valid JSON." });
        }

        body ??= new JsonObject();
        var model = body["model"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(model))
        {
            model = DefaultModel;
            body["model"] = model;
        }

        using var upstreamRequest = new HttpRequestMessage(HttpMethod.Post, OpenRouterEndpoint)
        {
            Content = JsonContent.Create(body)
        };
        upstreamRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var httpClient = httpClientFactory.CreateClient(nameof(ChatEndpoints));

        HttpResponseMessage upstreamResponse;
        try
        {
            upstreamResponse = await httpClient.SendAsync(upstreamRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("{Timestamp:O} model={Model} success=false error={Error}", DateTime.UtcNow, model, ex.Message);
            return Results.Problem("Failed to reach OpenRouter.", statusCode: StatusCodes.Status502BadGateway);
        }

        using (upstreamResponse)
        {
            var responseBody = await upstreamResponse.Content.ReadAsStringAsync(cancellationToken);
            var success = upstreamResponse.IsSuccessStatusCode;

            logger.LogInformation(
                "{Timestamp:O} model={Model} status={Status} success={Success}",
                DateTime.UtcNow, model, (int)upstreamResponse.StatusCode, success);

            if (upstreamResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                && upstreamResponse.Headers.RetryAfter is not null)
            {
                httpRequest.HttpContext.Response.Headers.RetryAfter = upstreamResponse.Headers.RetryAfter.ToString();
            }

            return Results.Content(responseBody, "application/json", statusCode: (int)upstreamResponse.StatusCode);
        }
    }
}
