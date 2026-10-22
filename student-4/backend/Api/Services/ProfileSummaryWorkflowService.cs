using System.Text.Json;
using Api.DTOs;
using Student4.Contracts;

namespace Api.Services;

public sealed class ProfileSummaryWorkflowService(IMultiAgentClient client)
{
    private const string Owner = "student-4";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ProfileSummaryProposalDto> GenerateAsync(
        UserRecord user,
        StudentRecord? student,
        TeacherRecord? teacher,
        CancellationToken cancellationToken)
    {
        var goal = "Create a concise professional profile summary for the account. " +
            "Use only the supplied profile evidence. Do not infer or invent personal traits, " +
            "qualifications, or interests. Return only the summary text, with no labels or commentary.";
        var evidence = JsonSerializer.Serialize(new
        {
            accountId = user.Id,
            user.FirstName,
            user.MiddleNames,
            user.LastName,
            user.UserType,
            user.Gender,
            student = student is null ? null : new
            {
                student.CourseStatus,
                student.IsInternational
            },
            teacher = teacher is null ? null : new
            {
                teacher.EmploymentStatus
            },
            existingSummary = user.UserProfile
        }, JsonOptions);

        var workflow = await client.RunAsync(user.Id, goal, evidence, cancellationToken);
        EnsureWorkflow(workflow, user.Id, "AwaitingHumanReview");
        return new ProfileSummaryProposalDto(workflow, GetDraftSummary(workflow));
    }

    public async Task<ProfileSummaryReviewResultDto?> ReviewAsync(
        Guid userId,
        Guid workflowId,
        MultiAgentReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        var current = await GetOwnedWorkflowAsync(userId, workflowId, cancellationToken);
        if (current is null) return null;
        if (current.Status != "AwaitingHumanReview" || current.HumanReview is not null ||
            request.ExpectedRevision != current.Revision)
        {
            throw new MultiAgentServiceException("This proposal is no longer awaiting review. Generate or review the current revision.",
                statusCode: System.Net.HttpStatusCode.Conflict);
        }

        var reviewed = await client.ReviewAsync(workflowId, request, cancellationToken);
        EnsureWorkflow(reviewed, userId, request.Decision == "Approve" ? "Approved" : "Rejected");
        if (reviewed.HumanReview is null || reviewed.HumanReview.Decision != request.Decision)
        {
            throw new MultiAgentServiceException("The Multi-Agent server did not record the requested human review.");
        }

        var approvedSummary = request.Decision == "Approve"
            ? ValidateSummary(reviewed.HumanReview.FinalOutput)
            : null;
        return new ProfileSummaryReviewResultDto(reviewed, approvedSummary);
    }

    public async Task<ProfileSummaryProposalDto?> ReviseAsync(
        Guid userId,
        Guid workflowId,
        MultiAgentRevisionRequestDto request,
        CancellationToken cancellationToken)
    {
        var current = await GetOwnedWorkflowAsync(userId, workflowId, cancellationToken);
        if (current is null) return null;
        if (current.Status != "AwaitingHumanReview" || current.HumanReview is not null ||
            request.ExpectedRevision != current.Revision)
        {
            throw new MultiAgentServiceException("This proposal is no longer awaiting review. Generate or review the current revision.",
                statusCode: System.Net.HttpStatusCode.Conflict);
        }

        var revised = await client.ReviseAsync(workflowId, request, cancellationToken);
        EnsureWorkflow(revised, userId, "AwaitingHumanReview");
        if (revised.Revision != request.ExpectedRevision + 1 || revised.HumanReview is not null)
        {
            throw new MultiAgentServiceException("The Multi-Agent server returned an invalid revised proposal.");
        }
        return new ProfileSummaryProposalDto(revised, GetDraftSummary(revised));
    }

    private async Task<MultiAgentWorkflowDto?> GetOwnedWorkflowAsync(
        Guid userId, Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await client.GetAsync(workflowId, cancellationToken);
        return workflow is not null && workflow.Owner == Owner && workflow.StudentId == userId
            ? workflow
            : null;
    }

    private static void EnsureWorkflow(MultiAgentWorkflowDto workflow, Guid userId, string expectedStatus)
    {
        if (workflow.Owner != Owner || workflow.StudentId != userId || workflow.Status != expectedStatus)
        {
            throw new MultiAgentServiceException("The Multi-Agent server returned a workflow with unexpected ownership or status.");
        }
    }

    private static string GetDraftSummary(MultiAgentWorkflowDto workflow) =>
        ValidateSummary(workflow.Worker.DraftOutput);

    private static string ValidateSummary(string? summary)
    {
        var trimmed = summary?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 2000)
        {
            throw new MultiAgentServiceException("The Multi-Agent server returned an empty or oversized profile summary.");
        }
        return trimmed;
    }
}