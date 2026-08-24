namespace Api.Models;

public class CanvasRequestLog
{
    public Guid Id { get; }
    public required string Operation { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public bool Succeeded { get; set; }
    public int ItemCount { get; set; }
    public int? UpstreamStatusCode { get; set; }
}
