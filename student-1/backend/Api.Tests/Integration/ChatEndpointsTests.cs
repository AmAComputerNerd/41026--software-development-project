using System.Net;
using System.Net.Http.Json;
using Api.DTOs;
using Api.Models;
using Moq;
using Xunit;

namespace Api.Tests.Integration;

public class ChatEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly Guid StudentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ChatEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSessions_ReturnsOkWithList()
    {
        await _factory.ResetDatabaseAsync();

        var response = await _client.GetAsync($"/api/chat/sessions?studentId={StudentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sessions = await response.Content.ReadFromJsonAsync<List<ChatSessionHeaderDto>>();
        Assert.NotNull(sessions);
    }

    [Fact]
    public async Task CreateSession_ReturnsCreatedWithSession()
    {
        await _factory.ResetDatabaseAsync();

        var payload = new CreateChatSessionRequestDto(StudentId, "New Sprint Chat");
        var response = await _client.PostAsJsonAsync("/api/chat/sessions", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ChatSessionHeaderDto>();
        Assert.NotNull(created);
        Assert.Equal("New Sprint Chat", created.Title);
        Assert.Equal(StudentId, created.StudentId);
    }

    [Fact]
    public async Task GetSessionDetail_ReturnsOkWithMessages()
    {
        await _factory.ResetDatabaseAsync();

        // Create a session
        var createResponse = await _client.PostAsJsonAsync("/api/chat/sessions", new CreateChatSessionRequestDto(StudentId, "Detail Test Session"));
        var created = await createResponse.Content.ReadFromJsonAsync<ChatSessionHeaderDto>();
        Assert.NotNull(created);

        var response = await _client.GetAsync($"/api/chat/sessions/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<ChatSessionDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail.Id);
    }

    [Fact]
    public async Task InitialDigest_GeneratesOpeningMessage()
    {
        await _factory.ResetDatabaseAsync();

        _factory.AiDigestServiceMock
            .Setup(s => s.GenerateDigestAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("You have 1 approaching deadline and no unread grades.");

        var createResponse = await _client.PostAsJsonAsync("/api/chat/sessions", new CreateChatSessionRequestDto(StudentId, "Digest Opening Test"));
        var created = await createResponse.Content.ReadFromJsonAsync<ChatSessionHeaderDto>();
        Assert.NotNull(created);

        var digestResponse = await _client.PostAsync($"/api/chat/sessions/{created.Id}/initial-digest?studentId={StudentId}", null);

        Assert.Equal(HttpStatusCode.OK, digestResponse.StatusCode);
        var message = await digestResponse.Content.ReadFromJsonAsync<ChatMessageDetailDto>();
        Assert.NotNull(message);
        Assert.Equal("assistant", message.Role);
        Assert.Contains("Notification Summary", message.Content);
        Assert.Contains("approaching deadline", message.Content);
    }

    [Fact]
    public async Task SendMessage_ReturnsAssistantReply()
    {
        await _factory.ResetDatabaseAsync();

        _factory.AiDigestServiceMock
            .Setup(s => s.AskAssistantAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ChatMessageDto>?>(),
                It.IsAny<IReadOnlyList<Notification>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("I suggest starting with task 1 first.");

        _factory.RagClientMock
            .Setup(r => r.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagQueryResponseDto("study plan", "Relevant syllabus context", ["course_policies.md"], "HIGH", true));

        var createResponse = await _client.PostAsJsonAsync("/api/chat/sessions", new CreateChatSessionRequestDto(StudentId, "Message Exchange Test"));
        var created = await createResponse.Content.ReadFromJsonAsync<ChatSessionHeaderDto>();
        Assert.NotNull(created);

        var sendResponse = await _client.PostAsJsonAsync(
            $"/api/chat/sessions/{created.Id}/messages?studentId={StudentId}",
            new SendChatMessageRequestDto("What should I study?"));

        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);
        var reply = await sendResponse.Content.ReadFromJsonAsync<ChatMessageDetailDto>();
        Assert.NotNull(reply);
        Assert.Equal("assistant", reply.Role);
        Assert.Equal("I suggest starting with task 1 first.", reply.Content);
    }

    [Fact]
    public async Task DeleteSession_ReturnsNoContent()
    {
        await _factory.ResetDatabaseAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/chat/sessions", new CreateChatSessionRequestDto(StudentId, "To Delete"));
        var created = await createResponse.Content.ReadFromJsonAsync<ChatSessionHeaderDto>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/chat/sessions/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/chat/sessions/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
