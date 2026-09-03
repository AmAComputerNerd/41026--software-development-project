namespace GradesManager.Models
{
    public class StudentAssignment
    {
        public Guid StudentId { get; set; }
        public Student? Student { get; }
        public Guid AssignmentId { get; set; }
        public Assignment? Assignment { get; }
        public double? TempMark { get; set; }
        public double? FinalMark { get; set; }
    }
}
