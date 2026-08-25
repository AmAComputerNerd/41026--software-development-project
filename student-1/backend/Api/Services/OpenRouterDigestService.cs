using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Api.Models;

namespace Api.Services;

public class OpenRouterDigestService : IAiDigestService
{
    private const string Model = "nvidia/nemotron-3-ultra-550b-a55b:free";
    private const string DefaultBaseUrl = "http://ai-mode:8080";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public OpenRouterDigestService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> GenerateDigestAsync(Guid studentId, IReadOnlyList<Notification> unreadNotifications, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AiGateway:BaseUrl"] ?? DefaultBaseUrl;
        var endpoint = $"{baseUrl.TrimEnd('/')}/v1/chat/completions";

        var prompt = BuildPrompt(studentId, unreadNotifications);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = JsonContent.Create(new ChatCompletionRequest
        {
            Model = Model,
            Messages =
            [
                new ChatMessage { Role = "user", Content = prompt }
            ]
        });

        using var httpClient = _httpClientFactory.CreateClient(nameof(OpenRouterDigestService));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);

        return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim()
            ?? string.Empty;
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
        sb.AppendLine("Summarise what the student should know about today in a short, prioritised digest, most important first.");

        return sb.ToString();
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("messages")]
        public required List<ChatMessage> Messages { get; init; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public required string Role { get; init; }

        [JsonPropertyName("content")]
        public required string Content { get; init; }
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice>? Choices { get; init; }
    }

    private sealed class ChatCompletionChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; init; }
    }
}
