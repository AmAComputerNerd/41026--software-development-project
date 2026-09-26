namespace Api.DTOs;

public sealed record UpcomingDeadlinesRequestDto(int Days = 7, int Limit = 10);

public sealed record McpDeadlineToolResultDto(
    string Status,
    string Tool,
    UpcomingDeadlinesDto? Data,
    McpToolErrorDto? Error);

public sealed record UpcomingDeadlinesDto(
    int Days,
    int Count,
    DateTime GeneratedAtUtc,
    IReadOnlyList<UpcomingDeadlineDto> Items);

public sealed record UpcomingDeadlineDto(
    Guid Id,
    string Title,
    DateTime DueDate,
    string Priority,
    string Status,
    string? CourseName);

public sealed record McpToolErrorDto(string Code, string Message);
