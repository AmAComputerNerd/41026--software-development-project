namespace GradesManager.Models
{
    public class Assignment
    {
        public Guid AssignmentId { get; }
        public Guid CourseId { get; set; }
        public Guid GroupId { get; set; }
        public Course? Course { get; }
        public AssignmentGroup? Group { get; }
        public required string Name { get; set; }
        public int? MaxMark { get; set; }
        public DateTime? DueAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CanvasWorkflowState { get; set; }
        public string? CanvasSubmissionState { get; set; }
        public bool? CanvasIsActive { get; set; }
        public long? CanvasAssignmentId { get; set; }
    }
}
