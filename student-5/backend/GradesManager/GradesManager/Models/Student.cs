namespace GradesManager.Models
{
    public class Student
    {
        public Guid StudentId { get; }
        public String? Name { get; set; }
        public double? IdealMark { get; set; }
    }
}
