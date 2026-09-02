using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Models;

namespace Api.Services;

public sealed partial class OpenRouterDigestService(
    HttpClient httpClient,
    ILogger<OpenRouterDigestService> logger) : IAiDigestService
{
    private const string Model = "nvidia/nemotron-3-ultra-550b-a55b:free";
    private const int MaxAttempts = 3;
    private const int MaxDigestCharacters = 4000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> GenerateDigestAsync(
        Guid studentId,
        IReadOnlyList<Notification> unreadNotifications,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(studentId, unreadNotifications);
        var request = new ChatCompletionRequest
        {
            Model = Model,
            Messages =
            [
                new ChatMessage { Role = "user", Content = prompt }
            ],
            MaxTokens = 800,
            Temperature = 0.2,
            Reasoning = new ReasoningOptions("low")
        };

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var response = await SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                LogGatewayHttpError(logger, statusCode);
                if (IsTransientStatusCode(statusCode) && attempt < MaxAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, statusCode.ToString(CultureInfo.InvariantCulture), cancellationToken);
                    continue;
                }

                throw new AiGatewayException($"The AI gateway returned HTTP {statusCode} after retrying.");
            }

            ChatCompletionResponse? completion;
            try
            {
                completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
            }
            catch (JsonException exception)
            {
                LogInvalidDigestJson(logger, exception);
                throw new AiGatewayException("The AI gateway returned an unreadable response.", exception);
            }

            var choice = completion?.Choices?.FirstOrDefault();
            var content = choice?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                LogEmptyDigestContent(
                    logger,
                    choice?.FinishReason ?? "missing",
                    !string.IsNullOrWhiteSpace(choice?.Message?.Reasoning));
                if (attempt < MaxAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, "empty response", cancellationToken);
                    continue;
                }

                throw new AiGatewayException("The AI gateway returned an empty response after retrying.");
            }

            return content.Trim();
        }

        throw new AiGatewayException("The AI gateway did not return a response.");
    }

    private async Task<HttpResponseMessage> SendAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var endpoint = new Uri(httpClient.BaseAddress!, "v1/chat/completions");
        try
        {
            return await httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogGatewayTransportError(logger, exception);
            throw new AiGatewayException("The AI gateway could not be reached.", exception);
        }
    }

    private async Task DelayBeforeRetryAsync(
        int attempt,
        string reason,
        CancellationToken cancellationToken)
    {
        var delayMilliseconds = attempt * 500;
        LogDigestRetry(logger, attempt + 1, reason, delayMilliseconds);
        await Task.Delay(delayMilliseconds, cancellationToken);
    }

    private static bool IsTransientStatusCode(int statusCode)
    {
        return statusCode is StatusCodes.Status408RequestTimeout
            or StatusCodes.Status429TooManyRequests
            or StatusCodes.Status502BadGateway
            or StatusCodes.Status503ServiceUnavailable
            or StatusCodes.Status504GatewayTimeout;
    }

    private static string BuildPrompt(Guid studentId, IReadOnlyList<Notification> unreadNotifications)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an assistant that writes a short, prioritised, natural-language digest for a student.");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Student ID: {studentId}");
        sb.AppendLine("Here are the student's unread notifications:");

        foreach (var notification in unreadNotifications)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"- [{notification.Type}] {notification.Message} (from {notification.SourceMicroservice} at {notification.CreatedAtUtc:O})");
        }

        sb.AppendLine();
        sb.AppendLine("Summarise what the student should know about today in a short, prioritised digest, most important first. Keep it under 500 words.");
        sb.AppendLine("If the list is empty, reply exactly: \"No new notifications.\"");

        var prompt = sb.ToString();
        return prompt.Length > MaxDigestCharacters
            ? prompt[..MaxDigestCharacters]
            : prompt;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "AI gateway returned HTTP {StatusCode}.")]
    private static partial void LogGatewayHttpError(ILogger logger, int statusCode);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "AI gateway returned an unreadable response.")]
    private static partial void LogInvalidDigestJson(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "The AI gateway could not be reached.")]
    private static partial void LogGatewayTransportError(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "AI gateway returned no digest content (finish reason: {FinishReason}; reasoning present: {HasReasoning}).")]
    private static partial void LogEmptyDigestContent(ILogger logger, string finishReason, bool hasReasoning);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Retrying AI digest request (attempt {Attempt}) after {Reason}; waiting {DelayMilliseconds} ms.")]
    private static partial void LogDigestRetry(ILogger logger, int attempt, string reason, int delayMilliseconds);
}

internal sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required List<ChatMessage> Messages { get; init; }

    [JsonPropertyName("max_tokens")]
    public required int MaxTokens { get; init; }

    [JsonPropertyName("temperature")]
    public required double Temperature { get; init; }

    [JsonPropertyName("reasoning")]
    public required ReasoningOptions Reasoning { get; init; }
}

internal sealed class ChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

internal sealed class ReasoningOptions
{
    [JsonPropertyName("effort")]
    public string Effort { get; init; }

    public ReasoningOptions(string effort)
    {
        Effort = effort;
    }
}

internal sealed class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<ChatCompletionChoice>? Choices { get; init; }
}

internal sealed class ChatCompletionChoice
{
    [JsonPropertyName("message")]
    public ChatCompletionMessage? Message { get; init; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
}

internal sealed class ChatCompletionMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; init; }
}
