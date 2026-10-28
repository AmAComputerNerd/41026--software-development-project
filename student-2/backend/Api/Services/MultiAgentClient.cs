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
    public const string Owner = "student-2";

    public bool IsEnabled => options.Value.Enabled is true;

    public Task<MultiAgentWorkflowDto> RunAsync(
        Guid studentId,
        string goal,
        string evidenceSnapshot,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Post,
            "api/workflows/run",
            new { owner = Owner, studentId, goal, evidenceSnapshot },
            cancellationToken);

    public async Task<MultiAgentWorkflowDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var workflow = await SendAsync(HttpMethod.Get, $"api/workflows/{id}", null, cancellationToken);
            return workflow.Id == id ? workflow : null;
        }
        catch (MultiAgentServiceException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<MultiAgentWorkflowDto> ReviewAsync(
        Guid id,
        MultiAgentReviewRequestDto request,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, $"api/workflows/{id}/human-review", request, cancellationToken);

    public Task<MultiAgentWorkflowDto> ReviseAsync(
        Guid id,
        MultiAgentRevisionRequestDto request,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, $"api/workflows/{id}/revise", request, cancellationToken);

    private async Task<MultiAgentWorkflowDto> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            throw new MultiAgentIntegrationDisabledException();
        }

        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new MultiAgentConflictException(
                    "The Multi-Agent workflow changed or already has a human decision.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new MultiAgentServiceException(
                    $"The Multi-Agent server returned HTTP {(int)response.StatusCode}.",
                    statusCode: response.StatusCode);
            }

            var workflow = await response.Content.ReadFromJsonAsync<MultiAgentWorkflowDto>(cancellationToken)
                ?? throw new MultiAgentServiceException("The Multi-Agent server returned an empty response.");
            if (workflow.Id == Guid.Empty || workflow.Owner != Owner ||
                workflow.Worker?.DraftOutput is null || workflow.Reviewer?.Critique is null ||
                workflow.Planner?.Steps is null || workflow.AuditLog is null)
            {
                throw new MultiAgentServiceException(
                    "The Multi-Agent server returned an incomplete or foreign workflow.");
            }

            return workflow;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MultiAgentServiceException("The Multi-Agent request timed out.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new MultiAgentServiceException("The Multi-Agent server could not be reached.", exception);
        }
        catch (JsonException exception)
        {
            throw new MultiAgentServiceException(
                "The Multi-Agent server returned an unreadable response.",
                exception);
        }
    }
}
