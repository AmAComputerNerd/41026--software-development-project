namespace GradesManager.Models
{
    public class Assignment
    {
        public Guid AssignmentID { get; }
        public Guid CourseID { get; }
        public Course? Course { get; }
        public required string Name { get; set; }
        public double? Weight { get; set; }
        public int? MaxMark { get; set; }
        public int? TempMark { get; set; }
        public int? FinalMark { get; set; }
        public bool? Completed { get; set; }
    }
}
