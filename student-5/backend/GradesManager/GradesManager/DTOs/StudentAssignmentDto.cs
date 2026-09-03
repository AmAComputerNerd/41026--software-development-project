namespace GradesManager.DTOs
{
    public record StudentAssignmentDto
    (
        Guid StudentId,
        Guid AssignmentId,
        double? TempMark,
        double? FinalMark
    );
}
