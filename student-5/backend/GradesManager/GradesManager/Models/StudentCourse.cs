namespace GradesManager.Models
{
    public class StudentCourse
    {
        public Guid StudentId { get; set; }
        public Student? Student { get; }
        public Guid CourseId { get; set; }
        public Course? Course { get; }
    }
}
