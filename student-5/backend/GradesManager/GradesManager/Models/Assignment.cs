namespace GradesManager.Models
{
    public class Assignment
    {
        public Guid AssignmentId { get; }
        public Guid CourseId { get; set; }
        public Course? Course { get; }
        public required string Name { get; set; }
        public double? Weight { get; set; }
        public int? MaxMark { get; set; }
        public bool? Completed { get; set; }
    }
}
