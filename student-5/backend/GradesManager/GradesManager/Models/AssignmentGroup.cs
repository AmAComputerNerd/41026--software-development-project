namespace GradesManager.Models
{
    public class AssignmentGroup
    {
        public Guid GroupId { get; }
        public Guid CourseId { get; set; }
        public Course? Course { get; }
        public string? Name { get; set; }
        public double? Weight { get; set; }
        public long? CanvasAssignmentGroupId { get; set; }
    }
}
