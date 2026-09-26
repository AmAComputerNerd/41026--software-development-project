using Api.DTOs;

namespace Api.Services;

public interface IMcpDeadlineClient
{
    Task<McpDeadlineToolResultDto> GetUpcomingDeadlinesAsync(
        int days,
        int limit,
        CancellationToken cancellationToken);
}
