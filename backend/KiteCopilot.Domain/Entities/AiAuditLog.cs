namespace KiteCopilot.Domain.Entities;

public class AiAuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string User { get; set; } = "default-user";
    public string ConversationId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string? Tool { get; set; }
    public string? Arguments { get; set; }
    public string? Result { get; set; }
    public bool Success { get; set; } = true;
    public long ExecutionMs { get; set; }
}
