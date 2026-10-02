using Api.DTOs;

namespace Api.Services;

public interface IMcpAutomationClient
{
    Task<McpAutomationResultDto> GetAutomationHealthAsync(
        int days,
        CancellationToken cancellationToken);
}