namespace Api.Models;

public class CanvasRequestLog
{
    public Guid Id { get; }
    public required string Operation { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public bool Succeeded { get; set; }
    public int ItemCount { get; set; }
    public int? UpstreamStatusCode { get; set; }
}
