namespace GradesManager.DTOs
{
    public record CourseDto(
        Guid CourseId,
        string Code,
        string Name,
        long? CanvasCourseId,
        bool? CanvasIsActive,
        DateTime? LastCanvasSyncAt
    );
}
