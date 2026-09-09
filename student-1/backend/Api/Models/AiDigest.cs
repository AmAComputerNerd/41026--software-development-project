namespace Api.Models;

public class AiDigest
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public required string Summary { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}
