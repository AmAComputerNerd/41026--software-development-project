using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Api.Tests;

// Post-commit CI endpoint function 1: /api/automations (create, read, update, delete).
public sealed class AutomationEndpointsTests(AutomationsApiFactory factory)
    : IClassFixture<AutomationsApiFactory>
{
    private const string StudentId = "25341708-0000-0000-0000-000000000002";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostQuizFiller_PersistsHumanReviewFlag_AndGetReturnsIt()
    {
        var created = await PostAsync(QuizFillerJson(requireHumanReview: true));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = body.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("quizFiller", body.RootElement.GetProperty("$type").GetString());
        Assert.True(body.RootElement.GetProperty("requireHumanReview").GetBoolean());

        var fetched = await _client.GetFromJsonAsync<JsonElement>($"/api/automations/{id}");
        Assert.True(fetched.GetProperty("requireHumanReview").GetBoolean());
        Assert.Equal(2, fetched.GetProperty("numberOfAttemptsRequired").GetInt32());
    }

    [Fact]
    public async Task GetAutomations_FiltersByStudent_AndExcludesDeleted()
    {
        var created = await PostAsync(QuizFillerJson(requireHumanReview: false));
        using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = body.RootElement.GetProperty("id").GetGuid();

        var delete = await _client.DeleteAsync($"/api/automations/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await _client.GetFromJsonAsync<JsonElement>($"/api/automations/?studentId={StudentId}");
        Assert.Equal(JsonValueKind.Array, list.ValueKind);
        Assert.NotEmpty(list.EnumerateArray());
        Assert.DoesNotContain(list.EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);
        Assert.All(list.EnumerateArray(), item => Assert.Equal(StudentId, item.GetProperty("studentId").GetString()));
    }

    [Fact]
    public async Task PutQuizFiller_UpdatesValues()
    {
        var created = await PostAsync(QuizFillerJson(requireHumanReview: false));
        using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = body.RootElement.GetProperty("id").GetGuid();

        var updated = await _client.PutAsync(
            $"/api/automations/{id}",
            Json(QuizFillerJson(requireHumanReview: true, attempts: 3)));

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var result = await updated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("requireHumanReview").GetBoolean());
        Assert.Equal(3, result.GetProperty("numberOfAttemptsRequired").GetInt32());
    }

    [Fact]
    public async Task PostQuizFiller_WithoutQuestionTypes_ReturnsBadRequest()
    {
        var response = await PostAsync(
            $$"""{"$type":"quizFiller","studentId":"{{StudentId}}","enabled":true,"subjectId":null,"multipleChoice":false,"shortAnswer":false,"numberOfAttemptsRequired":1,"allowForNoTimeLimit":false,"requireHumanReview":true}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAutomation_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/automations/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostAsync(string json) =>
        _client.PostAsync("/api/automations/", Json(json));

    private static StringContent Json(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private static string QuizFillerJson(bool requireHumanReview, int attempts = 2) =>
        $$"""{"$type":"quizFiller","studentId":"{{StudentId}}","enabled":false,"subjectId":null,"multipleChoice":true,"shortAnswer":true,"numberOfAttemptsRequired":{{attempts}},"allowForNoTimeLimit":true,"requireHumanReview":{{(requireHumanReview ? "true" : "false")}}}""";
}
