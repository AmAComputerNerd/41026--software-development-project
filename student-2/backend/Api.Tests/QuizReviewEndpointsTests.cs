using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Api.Tests;

// Post-commit CI endpoint function 2: /api/automation-runs/{id}/quiz-review (Multi-Agent human review).
public sealed class QuizReviewEndpointsTests(AutomationsApiFactory factory)
    : IClassFixture<AutomationsApiFactory>
{
    private static readonly Guid SeededQuizFillerId = Guid.Parse("00000000-0000-0000-0000-000000000501");
    private static readonly Guid StudentId = Guid.Parse("25341708-0000-0000-0000-000000000002");
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetReview_ReturnsProposalAndLiveWorkflowStages()
    {
        var (runId, workflowId) = await SeedPendingRunAsync();
        ResetMocks();
        factory.MultiAgent.Setup(client => client.GetAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Workflow(workflowId, "AwaitingHumanReview"));

        var review = await _client.GetFromJsonAsync<JsonElement>($"/api/automation-runs/{runId}/quiz-review");

        Assert.Equal("REV", review.GetProperty("result").GetString());
        Assert.Equal(QuizAnswerReviewService.WorkerSource, review.GetProperty("proposalSource").GetString());
        var questions = review.GetProperty("questions").EnumerateArray().ToList();
        Assert.Equal(2, questions.Count);
        Assert.Equal(11, questions[0].GetProperty("proposedAnswerId").GetInt64());
        Assert.Equal("Planner", review.GetProperty("workflow").GetProperty("planner").GetProperty("objective").GetString());
        Assert.DoesNotContain("token-1", review.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Approve_RecordsHumanDecision_AndSubmitsProposedAnswersToCanvas()
    {
        var (runId, workflowId) = await SeedPendingRunAsync();
        ResetMocks();
        SetupReview(workflowId);
        AnswerSharedCanvasQuizQuestionsDto? submitted = null;
        factory.Canvas.Setup(canvas => canvas.AnswerQuizSubmissionQuestionsAsync(
                9001, It.IsAny<AnswerSharedCanvasQuizQuestionsDto>(), It.IsAny<CancellationToken>()))
            .Callback<long, AnswerSharedCanvasQuizQuestionsDto, CancellationToken>((_, dto, _) => submitted = dto)
            .Returns(Task.CompletedTask);

        var response = await _client.PostAsJsonAsync(
            $"/api/automation-runs/{runId}/quiz-review",
            new { decision = "Approve", reviewerName = "Isaac Thomas", comments = "Checked every answer." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SUC", result.GetProperty("result").GetString());
        Assert.Equal("Approve", result.GetProperty("reviewDecision").GetString());
        Assert.NotNull(submitted);
        Assert.Equal(1, submitted!.Attempt);
        Assert.Equal("token-1", submitted.ValidationToken);
        Assert.Contains(submitted.Answers, answer => answer.QuestionId == 1 && answer.AnswerId == 11);
        Assert.Contains(submitted.Answers, answer => answer.QuestionId == 2 && answer.Text == "Photosynthesis");
        factory.MultiAgent.Verify(client => client.ReviewAsync(
            workflowId,
            It.Is<MultiAgentReviewRequestDto>(dto => dto.Decision == "Approve" && dto.CorrectedOutput == null),
            It.IsAny<CancellationToken>()), Times.Once);

        await using var scope = factory.Services.CreateAsyncScope();
        var run = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .AutomationRuns.OfType<QuizFillerAutomationRun>().SingleAsync(item => item.Id == runId);
        Assert.Null(run.ValidationToken);
    }

    [Fact]
    public async Task Correct_SubmitsHumanEditedAnswers()
    {
        var (runId, workflowId) = await SeedPendingRunAsync();
        ResetMocks();
        SetupReview(workflowId);
        AnswerSharedCanvasQuizQuestionsDto? submitted = null;
        factory.Canvas.Setup(canvas => canvas.AnswerQuizSubmissionQuestionsAsync(
                9001, It.IsAny<AnswerSharedCanvasQuizQuestionsDto>(), It.IsAny<CancellationToken>()))
            .Callback<long, AnswerSharedCanvasQuizQuestionsDto, CancellationToken>((_, dto, _) => submitted = dto)
            .Returns(Task.CompletedTask);

        var response = await _client.PostAsJsonAsync(
            $"/api/automation-runs/{runId}/quiz-review",
            new
            {
                decision = "Correct",
                reviewerName = "Isaac Thomas",
                comments = "Question 1 should be option B.",
                answers = new object[]
                {
                    new { questionId = 1, answerId = 12, text = (string?)null },
                    new { questionId = 2, answerId = (long?)null, text = "Light-driven sugar synthesis" }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(submitted!.Answers, answer => answer.QuestionId == 1 && answer.AnswerId == 12);
        factory.MultiAgent.Verify(client => client.ReviewAsync(
            workflowId,
            It.Is<MultiAgentReviewRequestDto>(dto => dto.Decision == "Correct" && dto.CorrectedOutput!.Contains("12")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_DoesNotSubmitToCanvas()
    {
        var (runId, workflowId) = await SeedPendingRunAsync();
        ResetMocks();
        SetupReview(workflowId);

        var response = await _client.PostAsJsonAsync(
            $"/api/automation-runs/{runId}/quiz-review",
            new { decision = "Reject", reviewerName = "Isaac Thomas", comments = "I will sit this quiz myself." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("REJ", result.GetProperty("result").GetString());
        factory.Canvas.Verify(canvas => canvas.AnswerQuizSubmissionQuestionsAsync(
            It.IsAny<long>(), It.IsAny<AnswerSharedCanvasQuizQuestionsDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Correct_WithOptionNotOffered_ReturnsBadRequest_AndKeepsRunAwaitingReview()
    {
        var (runId, _) = await SeedPendingRunAsync();
        ResetMocks();

        var response = await _client.PostAsJsonAsync(
            $"/api/automation-runs/{runId}/quiz-review",
            new
            {
                decision = "Correct",
                reviewerName = "Isaac Thomas",
                answers = new object[]
                {
                    new { questionId = 1, answerId = 999 },
                    new { questionId = 2, text = "Answer" }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("REV", await GetResultAsync(runId));
        factory.MultiAgent.Verify(client => client.ReviewAsync(
            It.IsAny<Guid>(), It.IsAny<MultiAgentReviewRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SecondReview_ReturnsConflict()
    {
        var (runId, workflowId) = await SeedPendingRunAsync();
        ResetMocks();
        SetupReview(workflowId);
        var request = new { decision = "Reject", reviewerName = "Isaac Thomas" };

        var first = await _client.PostAsJsonAsync($"/api/automation-runs/{runId}/quiz-review", request);
        var second = await _client.PostAsJsonAsync($"/api/automation-runs/{runId}/quiz-review", request);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task MultiAgentDisabled_ReturnsServiceUnavailable_AndRestoresPendingRun()
    {
        var (runId, workflowId) = await SeedPendingRunAsync();
        ResetMocks();
        factory.MultiAgent.Setup(client => client.ReviewAsync(
                workflowId, It.IsAny<MultiAgentReviewRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MultiAgentIntegrationDisabledException());

        var response = await _client.PostAsJsonAsync(
            $"/api/automation-runs/{runId}/quiz-review",
            new { decision = "Approve", reviewerName = "Isaac Thomas" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("REV", await GetResultAsync(runId));
    }

    [Fact]
    public async Task GetReview_UnknownRun_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/automation-runs/{Guid.NewGuid()}/quiz-review");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Revise_RerunsAgentsWithFeedback_AndKeepsRunAwaitingReview()
    {
        var (runId, workflowId) = await SeedPendingRunAsync();
        ResetMocks();
        MultiAgentRevisionRequestDto? sent = null;
        factory.MultiAgent.Setup(client => client.ReviseAsync(
                workflowId, It.IsAny<MultiAgentRevisionRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, MultiAgentRevisionRequestDto, CancellationToken>((_, dto, _) => sent = dto)
            .ReturnsAsync(Workflow(
                workflowId,
                "AwaitingHumanReview",
                draftOutput: "{\"answers\":[{\"questionId\":1,\"answerId\":2,\"text\":null},{\"questionId\":2,\"answerId\":null,\"text\":\"Revised\"}]}",
                revision: 1));
        SetupReview(workflowId);
        factory.Canvas.Setup(canvas => canvas.AnswerQuizSubmissionQuestionsAsync(
                9001, It.IsAny<AnswerSharedCanvasQuizQuestionsDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _client.PostAsJsonAsync(
            $"/api/automation-runs/{runId}/quiz-review/revise",
            new { requestId = Guid.NewGuid(), reviewerName = "Isaac Thomas", feedback = "Q1 is a chloroplast question." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("REV", result.GetProperty("result").GetString());
        Assert.Equal(1, result.GetProperty("workflowRevision").GetInt32());
        var questions = result.GetProperty("questions").EnumerateArray().ToList();
        Assert.Equal(12, questions[0].GetProperty("proposedAnswerId").GetInt64());
        Assert.Equal("Revised", questions[1].GetProperty("proposedText").GetString());
        Assert.Equal(0, sent!.ExpectedRevision);
        Assert.Equal("Q1 is a chloroplast question.", sent.Feedback);

        var approve = await _client.PostAsJsonAsync(
            $"/api/automation-runs/{runId}/quiz-review",
            new { decision = "Approve", reviewerName = "Isaac Thomas" });
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        factory.MultiAgent.Verify(client => client.ReviewAsync(
            workflowId,
            It.Is<MultiAgentReviewRequestDto>(dto => dto.ExpectedRevision == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Revise_WithUnusableWorkerOutput_KeepsPreviousAnswers()
    {
        var (runId, workflowId) = await SeedPendingRunAsync();
        ResetMocks();
        factory.MultiAgent.Setup(client => client.ReviseAsync(
                workflowId, It.IsAny<MultiAgentRevisionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Workflow(workflowId, "AwaitingHumanReview", draftOutput: "Plain text only.", revision: 1));

        var response = await _client.PostAsJsonAsync(
            $"/api/automation-runs/{runId}/quiz-review/revise",
            new { requestId = Guid.NewGuid(), reviewerName = "Isaac Thomas", feedback = "Try again." });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var review = await _client.GetFromJsonAsync<JsonElement>($"/api/automation-runs/{runId}/quiz-review");
        Assert.Equal("REV", review.GetProperty("result").GetString());
        Assert.Equal(1, review.GetProperty("workflowRevision").GetInt32());
        Assert.Equal(11, review.GetProperty("questions")[0].GetProperty("proposedAnswerId").GetInt64());
    }

    [Fact]
    public async Task Revise_WithoutFeedback_ReturnsBadRequest()
    {
        var (runId, _) = await SeedPendingRunAsync();
        ResetMocks();

        var response = await _client.PostAsJsonAsync(
            $"/api/automation-runs/{runId}/quiz-review/revise",
            new { requestId = Guid.NewGuid(), reviewerName = "Isaac Thomas", feedback = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("REV", await GetResultAsync(runId));
    }

    [Fact]
    public async Task ProposeAsync_MapsWorkerOptionNumbersBackToCanvasIds()
    {
        ResetMocks();
        var workflowId = Guid.NewGuid();
        factory.Ai.Setup(ai => ai.AnswerQuestionsAsync(It.IsAny<AiQuizContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GeneratedQuizAnswer(1, 11, null), new GeneratedQuizAnswer(2, null, "Baseline")]);
        factory.MultiAgent.Setup(client => client.RunAsync(
                StudentId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Workflow(
                workflowId,
                "AwaitingHumanReview",
                draftOutput: "```json\n{\"answers\":[{\"questionId\":1,\"answerId\":2,\"text\":null},{\"questionId\":2,\"answerId\":null,\"text\":\"Worker\"}]}\n```"));

        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<QuizAnswerReviewService>();
        var draft = await service.ProposeAsync(StudentId, Guid.NewGuid(), "Biology quiz", CanvasQuestions(), CancellationToken.None);

        Assert.Equal(workflowId, draft.WorkflowId);
        Assert.Equal(QuizAnswerReviewService.WorkerSource, draft.Proposal.Source);
        Assert.Contains(draft.Proposal.Answers, answer => answer.QuestionId == 1 && answer.AnswerId == 12);
        Assert.Contains(draft.Proposal.Answers, answer => answer.QuestionId == 2 && answer.Text == "Worker");
    }

    [Fact]
    public async Task ProposeAsync_FallsBackToAiModeBaseline_WhenWorkerOutputIsNotAnswerJson()
    {
        ResetMocks();
        factory.Ai.Setup(ai => ai.AnswerQuestionsAsync(It.IsAny<AiQuizContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GeneratedQuizAnswer(1, 11, null), new GeneratedQuizAnswer(2, null, "Baseline")]);
        factory.MultiAgent.Setup(client => client.RunAsync(
                StudentId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Workflow(Guid.NewGuid(), "AwaitingHumanReview", draftOutput: "Free-text plan only."));

        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<QuizAnswerReviewService>();
        var draft = await service.ProposeAsync(StudentId, Guid.NewGuid(), "Biology quiz", CanvasQuestions(), CancellationToken.None);

        Assert.Equal(QuizAnswerReviewService.BaselineSource, draft.Proposal.Source);
        Assert.Contains(draft.Proposal.Answers, answer => answer.QuestionId == 1 && answer.AnswerId == 11);
    }

    private void ResetMocks()
    {
        factory.Canvas.Reset();
        factory.Ai.Reset();
        factory.MultiAgent.Reset();
        factory.MultiAgent.SetupGet(client => client.IsEnabled).Returns(true);
    }

    private void SetupReview(Guid workflowId)
    {
        factory.MultiAgent.Setup(client => client.ReviewAsync(
                workflowId, It.IsAny<MultiAgentReviewRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, MultiAgentReviewRequestDto request, CancellationToken _) =>
                Workflow(id, "Reviewed", new MultiAgentHumanReviewDto(
                    request.Decision,
                    request.ReviewerName,
                    request.Comments,
                    request.CorrectedOutput ?? "approved",
                    DateTime.UtcNow)));
    }

    private async Task<string> GetResultAsync(Guid runId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.AutomationRuns.Where(run => run.Id == runId).Select(run => run.Result).SingleAsync();
    }

    private async Task<(Guid RunId, Guid WorkflowId)> SeedPendingRunAsync()
    {
        _ = factory.Server;
        var workflowId = Guid.NewGuid();
        var questions = CanvasQuestions()
            .Select(question => new QuizReviewQuestion(
                question.Id,
                question.QuestionType,
                question.QuestionText,
                [.. question.Answers.Select(answer => new AiQuizAnswerOption(answer.Id, answer.Text))]))
            .ToList();
        var run = new QuizFillerAutomationRun
        {
            AutomationId = SeededQuizFillerId,
            ExecutionKey = $"quiz-filler:test:{Guid.NewGuid():N}",
            ExecutionTimeStamp = DateTime.UtcNow,
            Result = AutomationRunResult.AwaitingReview,
            CourseId = 1001,
            QuizId = Random.Shared.NextInt64(10_000, 1_000_000),
            QuizTitle = "Biology quiz",
            QuestionCount = 2,
            SubmissionId = 9001,
            SubmissionAttempt = 1,
            ValidationToken = "token-1",
            WorkflowId = workflowId,
            ProposalJson = QuizAnswerReviewService.SerializeProposal(new QuizReviewProposal(
                "Biology quiz",
                QuizAnswerReviewService.WorkerSource,
                questions,
                [new GeneratedQuizAnswer(1, 11, null), new GeneratedQuizAnswer(2, null, "Photosynthesis")]))
        };

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AutomationRuns.Add(run);
        await db.SaveChangesAsync();
        return (run.Id, workflowId);
    }

    private static List<SharedCanvasQuizQuestionDto> CanvasQuestions() =>
    [
        new(1, "multiple_choice_question", "Which organelle performs photosynthesis?",
            [new SharedCanvasQuizAnswerOptionDto(11, "Mitochondria"), new SharedCanvasQuizAnswerOptionDto(12, "Chloroplast")]),
        new(2, "short_answer_question", "Name the process plants use to make sugar.", [])
    ];

    private static MultiAgentWorkflowDto Workflow(
        Guid id,
        string status,
        MultiAgentHumanReviewDto? humanReview = null,
        string draftOutput = "{}",
        int revision = 0) =>
        new(
            id,
            MultiAgentClient.Owner,
            StudentId,
            "{\"feature\":\"student-2-quiz-filler\"}",
            status,
            DateTime.UtcNow,
            DateTime.UtcNow,
            new MultiAgentPlannerDto("Planner", ["Quiz questions"], ["Answer", "Review"], DateTime.UtcNow),
            new MultiAgentWorkerDto("Quiz evidence", draftOutput, [], ["student-2/backend/supplied-context"], DateTime.UtcNow),
            new MultiAgentReviewerDto("ReadyForHumanReview", "high", "Looks correct.", [], [], DateTime.UtcNow),
            humanReview,
            [],
            revision);
}
