namespace GradesManager.DTOs
{
    public sealed record WeightingsRequestDto(double Weight = 0.4, int Limit = 5);

    public sealed record McpGradesToolResultDto(
        string Status,
        string Tool,
        WeightingsDto? Data,
        McpToolErrorDto? Error);

    public sealed record WeightingsDto(
        double Weight,
        int Count,
        DateTime GeneratedAtUtc,
        IReadOnlyList<WeightingDto> Items);

    public sealed record WeightingDto(
        Guid AssignmentId,
        string Name,
        double? Weight,
        int? MaxMark);

    public sealed record McpToolErrorDto(
        string Code,
        string Message
    );
}
