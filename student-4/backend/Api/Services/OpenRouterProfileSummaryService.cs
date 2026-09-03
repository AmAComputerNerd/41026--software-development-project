using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Api.Models;

namespace Api.Services;

public class OpenRouterProfileSummaryService : IAiProfileSummaryService
{
    private const string Model = "nvidia/nemotron-3-ultra-550b-a55b:free";
    private const string DefaultBaseUrl = "http://ai-mode:8080";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public OpenRouterProfileSummaryService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> GenerateSummaryAsync(
        User user,
        Student? student,
        Teacher? teacher,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AiGateway:BaseUrl"] ?? DefaultBaseUrl;
        var endpoint = $"{baseUrl.TrimEnd('/')}/v1/chat/completions";

        var prompt = BuildPrompt(user, student, teacher);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = JsonContent.Create(new ChatCompletionRequest
        {
            Model = Model,
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = prompt }
            }
        });

        using var httpClient = _httpClientFactory.CreateClient(nameof(OpenRouterProfileSummaryService));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);

        return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim()
            ?? string.Empty;
    }

    private static string BuildPrompt(User user, Student? student, Teacher? teacher)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are an assistant that writes a short, friendly, third-person profile summary for a user of an education platform.");
        sb.AppendLine();
        sb.AppendLine("Here is the user's profile data:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Name: {user.FirstName} {user.LastName}");
        if (!string.IsNullOrWhiteSpace(user.MiddleNames))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Middle names: {user.MiddleNames}");
        }
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Account type: {user.UserType}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Gender: {user.Gender}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Date of birth: {user.DateOfBirth:yyyy-MM-dd}");

        if (student is not null)
        {
            sb.AppendLine("- Student details:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  - Course status: {student.CourseStatus}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  - International: {(student.IsInternational ? "yes" : "no")}");
        }
        else if (teacher is not null)
        {
            sb.AppendLine("- Teacher details:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  - Employment status: {teacher.EmploymentStatus}");
        }

        sb.AppendLine();
        sb.AppendLine("Write a 2-3 sentence profile summary that is friendly and useful, mentioning their role and any distinguishing details. Avoid making up facts that aren't in the data. Do not include the email or DOB verbatim.");
        sb.AppendLine();
        sb.AppendLine("Return ONLY the summary text, no preamble or labels.");

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
