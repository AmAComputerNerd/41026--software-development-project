namespace GradesManager.DTOs
{
    public record StudentDto
    (
        Guid StudentId,
        String? Name,
        double? IdealMark,
        long? CanvasUserId
    );
}
