namespace GradesManager.DTOs
{
    public record CourseDto(
        Guid CourseId,
        string Code,
        string Name,
        long? CanvasCourseId,
        string? CanvasWorkflowState,
        bool? CanvasIsActive,
        DateTime? LastCanvasSyncAt
    );
}
