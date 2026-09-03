namespace GradesManager.DTOs
{
    public record AssignmentGroupDto
    (
        Guid GroupId,
        Guid CourseId,
        string? Name,
        double? Weight,
        long? CanvasAssignmentGroupId
    );
}
