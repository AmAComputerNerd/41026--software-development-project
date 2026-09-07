using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Student4.Contracts;

namespace Api.Services;

public class OpenRouterProfileSummaryService : IAiProfileSummaryService
{
    public const string HttpClientName = "AiGateway";

    private const string Model = "nvidia/nemotron-3-ultra-550b-a55b:free";
    private const string DefaultBaseUrl = "http://ai-mode:8080";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenRouterProfileSummaryService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientName);
        _configuration = configuration;
    }

    public async Task<string> GenerateSummaryAsync(
        UserRecord user,
        StudentRecord? student,
        TeacherRecord? teacher,
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

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new AiGatewayException(
                "Could not reach the AI gateway.",
                upstreamStatusCode: null,
                upstreamErrorBody: null,
                rateLimitReset: null,
                innerException: ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadBodyAsync(response, cancellationToken);
            var reset = ExtractRateLimitReset(response.Headers);
            throw new AiGatewayException(
                BuildUpstreamErrorMessage(response.StatusCode, body, reset),
                upstreamStatusCode: (int)response.StatusCode,
                upstreamErrorBody: body,
                rateLimitReset: reset);
        }

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);

        return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim()
            ?? string.Empty;
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static DateTimeOffset? ExtractRateLimitReset(HttpResponseHeaders headers)
    {
        if (headers.TryGetValues("X-RateLimit-Reset", out var values))
        {
            var raw = values.FirstOrDefault();
            if (long.TryParse(raw, out var unixMs))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(unixMs);
            }
        }
        return null;
    }

    private static string BuildUpstreamErrorMessage(HttpStatusCode statusCode, string body, DateTimeOffset? reset)
    {
        if (statusCode == HttpStatusCode.TooManyRequests && reset is not null)
        {
            return $"AI gateway rate limit reached. Try again after {reset:yyyy-MM-dd HH:mm} UTC.";
        }
        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            return "AI gateway rate limit reached. Try again later.";
        }
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"AI gateway returned {(int)statusCode} {statusCode}.";
        }
        return $"AI gateway returned {(int)statusCode} {statusCode}: {body}";
    }

    private static string BuildPrompt(UserRecord user, StudentRecord? student, TeacherRecord? teacher)
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
