namespace GradesManager.DTOs
{
    public record AssignmentDto
    (
        Guid AssignmentId,
        Guid CourseId,
        string Name,
        double? Weight,
        int? MaxMark,
        bool? Completed
    );
}
