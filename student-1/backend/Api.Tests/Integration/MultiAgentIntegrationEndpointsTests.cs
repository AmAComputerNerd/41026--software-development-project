using System.Net;
using System.Net.Http.Json;
using Api.DTOs;
using Moq;
using Xunit;

namespace Api.Tests.Integration;

public class MultiAgentIntegrationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly Guid StudentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public MultiAgentIntegrationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static MultiAgentWorkflowDto CreateSampleWorkflow(Guid id, string status = "AwaitingHumanReview") =>
        new(
            Id: id,
            Owner: "student-1",
            StudentId: StudentId,
            Goal: "Triage unread notifications and draft study plan",
            Status: status,
            CreatedAtUtc: DateTime.UtcNow,
            UpdatedAtUtc: DateTime.UtcNow,
            Planner: new MultiAgentPlannerStageDto(
                Objective: "Triage notifications",
                RequiredEvidence: ["PostgreSQL notifications_db", "RAG policies"],
                Steps: ["Step 1: Check unread", "Step 2: Prioritise deadlines"],
                CompletedAtUtc: DateTime.UtcNow),
            Worker: new MultiAgentWorkerStageDto(
                EvidenceSummary: "3 unread deadline alerts",
                DraftOutput: "Focus on Assessment 3 due in 48 hours.",
                RecommendedActions: ["Complete subtask breakdown", "Check grade impact"],
                Citations: ["student-1-backend:/notifications", "rag-server:/api/answers"],
                CompletedAtUtc: DateTime.UtcNow),
            Reviewer: new MultiAgentReviewerStageDto(
                Verdict: "ReadyForHumanReview",
                Confidence: "high",
                Critique: "Plan aligns with urgency and course policies.",
                IdentifiedRisks: ["Confirm Canvas sync timestamp"],
                Recommendations: ["Approve plan"],
                CompletedAtUtc: DateTime.UtcNow),
            HumanReview: status == "Approved"
                ? new MultiAgentHumanReviewStageDto(
                    Decision: "Approve",
                    ReviewerName: "Bryan Lee",
                    Comments: "Looks great",
                    FinalOutput: "Focus on Assessment 3 due in 48 hours.",
                    ReviewedAtUtc: DateTime.UtcNow)
                : null,
            AuditLog:
            [
                new MultiAgentAuditEntryDto(DateTime.UtcNow, "Planner", "PlanCreated", "Created 2-step plan"),
                new MultiAgentAuditEntryDto(DateTime.UtcNow, "Worker", "TaskExecuted", "Drafted triage plan"),
                new MultiAgentAuditEntryDto(DateTime.UtcNow, "Reviewer", "OutputReviewed", "Verified against evidence")
            ]);

    [Fact]
    public async Task RunMultiAgentWorkflow_ValidRequest_ReturnsAwaitingHumanReview()
    {
        var workflowId = Guid.NewGuid();
        var sample = CreateSampleWorkflow(workflowId);

        _factory.MultiAgentClientMock
            .SetupGet(m => m.IsEnabled)
            .Returns(true);
        _factory.MultiAgentClientMock
            .Setup(m => m.RunWorkflowAsync(
                StudentId,
                "Triage unread notifications and draft study plan",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sample);

        var response = await _client.PostAsJsonAsync(
            "/api/notifications/multi-agent/run",
            new StartMultiAgentWorkflowRequestDto(StudentId, "Triage unread notifications and draft study plan"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MultiAgentWorkflowDto>();
        Assert.NotNull(result);
        Assert.Equal(workflowId, result.Id);
        Assert.Equal("AwaitingHumanReview", result.Status);
        Assert.Equal("ReadyForHumanReview", result.Reviewer.Verdict);
    }

    [Fact]
    public async Task SubmitHumanReview_ValidDecision_ReturnsApprovedWorkflow()
    {
        var workflowId = Guid.NewGuid();
        var approved = CreateSampleWorkflow(workflowId, "Approved");

        _factory.MultiAgentClientMock
            .SetupGet(m => m.IsEnabled)
            .Returns(true);
        _factory.MultiAgentClientMock
            .Setup(m => m.SubmitHumanReviewAsync(
                workflowId,
                It.IsAny<SubmitHumanReviewRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(approved);

        var response = await _client.PostAsJsonAsync(
            $"/api/notifications/multi-agent/workflows/{workflowId}/review",
            new SubmitHumanReviewRequestDto("Approve", "Bryan Lee", "Approved after checking deadlines"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MultiAgentWorkflowDto>();
        Assert.NotNull(result);
        Assert.Equal("Approved", result.Status);
        Assert.NotNull(result.HumanReview);
        Assert.Equal("Approve", result.HumanReview.Decision);
    }

    [Fact]
    public async Task RunMultiAgentWorkflow_WhenDisabled_ReturnsServiceUnavailable()
    {
        _factory.MultiAgentClientMock
            .SetupGet(m => m.IsEnabled)
            .Returns(false);

        var response = await _client.PostAsJsonAsync(
            "/api/notifications/multi-agent/run",
            new StartMultiAgentWorkflowRequestDto(StudentId, "Triage unread notifications and draft study plan"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
