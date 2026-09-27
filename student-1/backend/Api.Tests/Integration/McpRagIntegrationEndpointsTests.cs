using System.Net;
using System.Net.Http.Json;
using Api.DTOs;
using Moq;
using Xunit;

namespace Api.Tests.Integration;

public class McpRagIntegrationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly Guid StudentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public McpRagIntegrationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BroadcastMcpAlert_ValidRequest_ReturnsOkWithResult()
    {
        _factory.McpClientMock
            .Setup(m => m.BroadcastAlertAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new McpBroadcastResponseDto(
                Success: true,
                Status: "success",
                Tool: "notifications_broadcast_alert",
                Data: new { title = "System Update", urgency = "High" }));

        var payload = new McpBroadcastRequestDto(
            StudentId: StudentId,
            Title: "System Maintenance",
            Message: "Scheduled maintenance in 30 minutes.",
            Urgency: "High");

        var response = await _client.PostAsJsonAsync("/api/notifications/mcp/broadcast", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<McpBroadcastResponseDto>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("notifications_broadcast_alert", result.Tool);
    }

    [Fact]
    public async Task BroadcastMcpAlert_InvalidRequest_ReturnsBadRequest()
    {
        var payload = new McpBroadcastRequestDto(
            StudentId: Guid.Empty,
            Title: "Hi", // too short
            Message: "No", // too short
            Urgency: "High");

        var response = await _client.PostAsJsonAsync("/api/notifications/mcp/broadcast", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task QueryRagKnowledge_ValidQuery_ReturnsGroundedAnswer()
    {
        _factory.RagClientMock
            .Setup(r => r.QueryAsync(
                "late submission penalty",
                "student-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagQueryResponseDto(
                Question: "late submission penalty",
                Answer: "Late submissions incur a 5% penalty per 24 hours.",
                Citations: ["course_policies.md (Section 2)"],
                Confidence: "HIGH",
                HasSufficientContext: true));

        var payload = new RagQueryRequestDto("late submission penalty", "student-1");
        var response = await _client.PostAsJsonAsync("/api/notifications/rag/query", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RagQueryResponseDto>();
        Assert.NotNull(result);
        Assert.True(result.HasSufficientContext);
        Assert.Equal("HIGH", result.Confidence);
        Assert.Contains("5% penalty", result.Answer);
    }

    [Fact]
    public async Task QueryRagKnowledge_QueryTooShort_ReturnsBadRequest()
    {
        var payload = new RagQueryRequestDto("ab");
        var response = await _client.PostAsJsonAsync("/api/notifications/rag/query", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
