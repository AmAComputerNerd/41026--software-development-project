using System.Net;
using System.Net.Http.Json;
using Api.Configuration;
using Api.DTOs;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class MultiAgentClient(
    HttpClient httpClient,
    IOptions<MultiAgentServerOptions> options) : IMultiAgentClient
{
    private readonly MultiAgentServerOptions _options = options.Value;

    public bool IsEnabled => _options.Enabled;

    public async Task<MultiAgentWorkflowDto> RunWorkflowAsync(
        Guid studentId,
        string goal,
        string? evidenceSnapshot = null,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();

        using var response = await httpClient.PostAsJsonAsync(
            "api/workflows/run",
            new
            {
                owner = "student-1",
                studentId,
                goal,
                evidenceSnapshot
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var record = await response.Content.ReadFromJsonAsync<MultiAgentWorkflowDto>(
            cancellationToken: cancellationToken);
        return record ?? throw new InvalidOperationException("Multi-Agent Server returned an empty workflow record.");
    }

    public async Task<MultiAgentWorkflowDto> SubmitHumanReviewAsync(
        Guid workflowId,
        SubmitHumanReviewRequestDto reviewRequest,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();

        using var response = await httpClient.PostAsJsonAsync(
            $"api/workflows/{workflowId}/human-review",
            reviewRequest,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Workflow {workflowId} was not found.");
        }

        response.EnsureSuccessStatusCode();
        var record = await response.Content.ReadFromJsonAsync<MultiAgentWorkflowDto>(
            cancellationToken: cancellationToken);
        return record ?? throw new InvalidOperationException("Multi-Agent Server returned an empty review record.");
    }

    public async Task<IReadOnlyList<MultiAgentWorkflowDto>> ListWorkflowsAsync(
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return [];
        }

        try
        {
            var url = studentId.HasValue && studentId.Value != Guid.Empty
                ? $"api/workflows?owner=student-1&studentId={studentId.Value}"
                : "api/workflows?owner=student-1";

            var items = await httpClient.GetFromJsonAsync<List<MultiAgentWorkflowDto>>(
                url,
                cancellationToken);
            return items ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<MultiAgentWorkflowDto?> GetWorkflowByIdAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();

        using var response = await httpClient.GetAsync($"api/workflows/{workflowId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MultiAgentWorkflowDto>(
            cancellationToken: cancellationToken);
    }

    private void EnsureEnabled()
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Multi-Agent Server integration is disabled for this environment.");
        }
    }
}
