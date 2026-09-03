namespace GradesManager.Models
{
    public class Course
    {
        public Guid CourseId { get; }
        public required string Code { get; set; }

        public required string Name { get; set; }

        public long? CanvasCourseId { get; set; }
        public string? CanvasWorkflowState { get; set; }
        public bool? CanvasIsActive { get; set; }
        public DateTime? LastCanvasSyncAt { get; set; }
    }
}
