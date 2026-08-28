namespace GradesManager.Models
{
    public class StudentCourse
    {
        public Guid StudentID { get; }
        public Student? Student { get; }
        public Guid CourseID { get; }
        public Course? Course { get; }
    }
}
