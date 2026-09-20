namespace McpServer.Services;

public interface IDeadlineClient
{
    Task<UpcomingDeadlinesData> GetUpcomingDeadlinesAsync(
        int days,
        int limit,
        CancellationToken cancellationToken);
}

public sealed record UpcomingDeadlinesData(
    int Days,
    int Count,
    DateTime GeneratedAtUtc,
    IReadOnlyList<UpcomingDeadlineItem> Items);

public sealed record UpcomingDeadlineItem(
    Guid Id,
    string Title,
    DateTime DueDate,
    string Priority,
    string Status,
    string? CourseName);
