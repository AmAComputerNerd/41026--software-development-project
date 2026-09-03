namespace GradesManager.DTOs
{
    public record AssignmentDto
    (
        Guid AssignmentId,
        Guid CourseId,
        Guid GroupId,
        string Name,
        double? MaxMark,
        DateTime? DueAt,
        DateTime? UpdatedAt,
        string? CanvasWorkflowState,
        string? CanvasSubmissionState,
        bool? CanvasIsActive,
        long? CanvasAssignmentId
    );
}
