namespace McpServer.Services
{
    public interface IGradesClient
    {
        Task<GradeWeightingsData> GetGradesWeightingsAsync(
            double weight,
            int limit,
            CancellationToken cancellationToken);
    }

    public sealed record GradeWeightingsData(
        double Weight,
        int Count,
        DateTime GeneratedAtUtc,
        IReadOnlyList<GradeWeightingItem> Items);

    public sealed record GradeWeightingItem(
        Guid AssignmentId,
        string Name,
        DateTime DueDate,
        double? Weight,
        int? MaxMark);
}
