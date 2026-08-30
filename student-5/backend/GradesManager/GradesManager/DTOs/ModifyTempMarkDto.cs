namespace GradesManager.DTOs
{
    public record ModifyTempMarkDto
    (
        Guid StudentId,
        Guid AssignmentId,
        int? TempMark
    );
}
