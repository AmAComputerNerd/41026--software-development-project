namespace Api.Models;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatSessionId { get; set; }
    public ChatSession ChatSession { get; set; } = null!;
    public required string Role { get; set; } // "user" or "assistant"
    public required string Content { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? CitationsJson { get; set; }
    public string? Confidence { get; set; } // "HIGH", "MEDIUM", "LOW", "INSUFFICIENT_CONTEXT"
}
