namespace GradesManager.DTOs
{
    public record StudentAssignmentDto
    (
        Guid StudentId,
        Guid AssignmentId,
        int? TempMark,
        int? FinalMark
    );
}
