using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace Api.Endpoints;

public static partial class ChatEndpoints
{
    private const string DefaultModel = "minimax/minimax-m3:free";
    private const string OpenRouterEndpoint = "https://openrouter.ai/api/v1/chat/completions";

    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/chat/completions", PostChatCompletions);
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
            LogMissingApiKey(logger);
            return Results.Problem("OpenRouter:ApiKey configuration value is not set.", statusCode: StatusCodes.Status500InternalServerError);
        }

        JsonNode? parsedBody;
        try
        {
            parsedBody = await JsonNode.ParseAsync(
                httpRequest.Body,
                cancellationToken: cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            return Results.BadRequest(new { error = "Request body must be valid JSON." });
        }

        if (parsedBody is not JsonObject body)
        {
            return Results.BadRequest(new { error = "Request body must be a JSON object." });
        }

        var model = body["model"]?.GetValue<string>()?.Trim();
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
            LogUpstreamRequestFailure(logger, model, ex.Message);
            return Results.Problem("Failed to reach OpenRouter.", statusCode: StatusCodes.Status502BadGateway);
        }

        using (upstreamResponse)
        {
            var responseBody = await upstreamResponse.Content.ReadAsStringAsync(cancellationToken);
            var responseStatusCode = (int)upstreamResponse.StatusCode;
            if (upstreamResponse.IsSuccessStatusCode &&
                TryGetEmbeddedErrorStatusCode(responseBody, out var embeddedStatusCode))
            {
                responseStatusCode = embeddedStatusCode;
            }

            var success = responseStatusCode is >= 200 and < 300;

            LogUpstreamResponse(logger, model, responseStatusCode, success);

            if (upstreamResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                && upstreamResponse.Headers.RetryAfter is not null)
            {
                httpRequest.HttpContext.Response.Headers.RetryAfter = upstreamResponse.Headers.RetryAfter.ToString();
            }

            return Results.Content(
                responseBody,
                "application/json",
                statusCode: responseStatusCode);
        }
    }

    private static bool TryGetEmbeddedErrorStatusCode(
        string responseBody,
        out int statusCode)
    {
        statusCode = StatusCodes.Status502BadGateway;

        try
        {
            var body = JsonNode.Parse(responseBody);
            if (body?["error"] is not JsonObject error)
            {
                return false;
            }

            if (error["code"] is JsonValue code)
            {
                if (code.TryGetValue<int>(out var numericCode) &&
                    numericCode is >= 400 and <= 599)
                {
                    statusCode = numericCode;
                }
                else if (code.TryGetValue<string>(out var stringCode) &&
                         int.TryParse(stringCode, out numericCode) &&
                         numericCode is >= 400 and <= 599)
                {
                    statusCode = numericCode;
                }
            }

            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "OpenRouter:ApiKey is not configured.")]
    private static partial void LogMissingApiKey(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "model={Model} success=false error={Error}")]
    private static partial void LogUpstreamRequestFailure(
        ILogger logger,
        string model,
        string error);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "model={Model} status={Status} success={Success}")]
    private static partial void LogUpstreamResponse(
        ILogger logger,
        string model,
        int status,
        bool success);
}
