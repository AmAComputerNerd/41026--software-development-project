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
    private const string Owner = "student-4";

    public bool IsEnabled => options.Value.Enabled is true;

    public Task<MultiAgentWorkflowDto> RunAsync(
        Guid userId, string goal, string evidenceSnapshot, CancellationToken cancellationToken) =>
        SendAsync<MultiAgentWorkflowDto>(HttpMethod.Post, "api/workflows/run",
            new { owner = Owner, studentId = userId, goal, evidenceSnapshot }, cancellationToken);

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

    public Task<MultiAgentWorkflowDto> ReviseAsync(
        Guid id, MultiAgentRevisionRequestDto request, CancellationToken cancellationToken) =>
        SendAsync<MultiAgentWorkflowDto>(
            HttpMethod.Post, $"api/workflows/{id}/revise", request, cancellationToken);

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
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    throw new MultiAgentServiceException(
                        "The workflow changed or a different human decision was recorded. Only the current proposal can be reviewed.",
                        statusCode: response.StatusCode);
                }
                throw new MultiAgentServiceException(
                    $"The Multi-Agent server returned HTTP {(int)response.StatusCode}.",
                    statusCode: response.StatusCode);
            }
            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new MultiAgentServiceException("The Multi-Agent server returned an empty response.");
            if (result is MultiAgentWorkflowDto workflow &&
                (workflow.Id == Guid.Empty || workflow.Goal is null || workflow.AuditLog is null ||
                 workflow.Planner?.Steps is null || workflow.Planner.RequiredEvidence is null ||
                 workflow.Worker?.DraftOutput is null || workflow.Reviewer?.IdentifiedRisks is null ||
                 workflow.Reviewer.Recommendations is null ||
                 (method != HttpMethod.Get && !IsOwned(workflow))))
            {
                throw new MultiAgentServiceException("The Multi-Agent server returned an invalid or foreign workflow.");
            }
            return result;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MultiAgentServiceException(
                "The Multi-Agent request timed out. Its workflow may still have been recorded; check before retrying.",
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
        workflow.Owner == Owner && workflow.StudentId != Guid.Empty;
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