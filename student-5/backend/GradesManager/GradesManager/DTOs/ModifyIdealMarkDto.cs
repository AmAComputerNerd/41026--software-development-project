namespace GradesManager.DTOs
{
    public record ModifyIdealMarkDto
    (
        Guid StudentId,
        double? idealMark
    );
}
