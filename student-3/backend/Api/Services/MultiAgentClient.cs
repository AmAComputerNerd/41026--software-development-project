using System.Net;
using System.Text.Json;
using Api.Configuration;
using Api.DTOs;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class MultiAgentClient(
    HttpClient httpClient,
    IOptions<MultiAgentServerOptions> options) : IMultiAgentClient
{
    private const string Owner = "student-3";
    private static readonly Guid DemoStudentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public bool IsEnabled => options.Value.Enabled is true;

    public Task<MultiAgentWorkflowDto> RunAsync(
        string goal, string evidenceSnapshot, CancellationToken cancellationToken) =>
        SendAsync<MultiAgentWorkflowDto>(HttpMethod.Post, "api/workflows/run",
            new { owner = Owner, studentId = DemoStudentId, goal, evidenceSnapshot },
            cancellationToken);

    public async Task<MultiAgentWorkflowDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var workflow = await SendAsync<MultiAgentWorkflowDto>(
                HttpMethod.Get, $"api/workflows/{id}", null, cancellationToken);
            return workflow.Id == id && IsOwned(workflow) ? workflow : null;
        }
        catch (MultiAgentServiceException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<MultiAgentWorkflowDto> ReviewAsync(
        Guid id, MultiAgentReviewRequestDto request, CancellationToken cancellationToken) =>
        SendAsync<MultiAgentWorkflowDto>(
            HttpMethod.Post, $"api/workflows/{id}/human-review", request, cancellationToken);

    private async Task<T> SendAsync<T>(
        HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        if (!IsEnabled) throw new MultiAgentIntegrationDisabledException();
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new MultiAgentServiceException(
                    $"The Multi-Agent server returned HTTP {(int)response.StatusCode}.",
                    statusCode: response.StatusCode);
            }
            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new MultiAgentServiceException("The Multi-Agent server returned an empty response.");
            if (result is MultiAgentWorkflowDto workflow)
            {
                if (method != HttpMethod.Get && !IsOwned(workflow))
                {
                    throw new MultiAgentServiceException("The Multi-Agent server returned a workflow for another slice.");
                }
                if (workflow.Id == Guid.Empty || workflow.Goal is null || workflow.AuditLog is null ||
                    workflow.Planner?.Steps is null || workflow.Planner.RequiredEvidence is null ||
                    workflow.Worker?.DraftOutput is null || workflow.Reviewer?.IdentifiedRisks is null ||
                    workflow.Reviewer.Recommendations is null || workflow.HumanReview is { FinalOutput: null })
                {
                    throw new MultiAgentServiceException("The Multi-Agent server returned an incomplete workflow.");
                }
            }
            return result;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MultiAgentServiceException(
                "The Multi-Agent request timed out. No tasks were created by this request; the server may still record the workflow or review. Retry a failed save with the same selections and comments.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new MultiAgentServiceException("The Multi-Agent server could not be reached.", exception);
        }
        catch (JsonException exception)
        {
            throw new MultiAgentServiceException("The Multi-Agent server returned an unreadable response.", exception);
        }
    }

    private static bool IsOwned(MultiAgentWorkflowDto workflow) =>
        workflow.Owner == Owner && workflow.StudentId == DemoStudentId;
}

public sealed class MultiAgentIntegrationDisabledException()
    : Exception("Multi-Agent integration is disabled.");

public sealed class MultiAgentServiceException(
    string message,
    Exception? innerException = null,
    HttpStatusCode? statusCode = null) : Exception(message, innerException)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
