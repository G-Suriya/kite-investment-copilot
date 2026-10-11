namespace KiteCopilot.Domain.Entities;

public class InvestmentNote
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Thesis { get; set; } = string.Empty;
    public string TimeHorizon { get; set; } = "Long-Term (3-5 Years)"; // Short-Term (< 1 Year), Medium-Term (1-3 Years), Long-Term (3-5 Years)
    public string? KeyRisks { get; set; }
    public string? WatchPoints { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Vector embedding representation stored as JSON for portable EF Core support across both SQLite & PostgreSQL
    public string? EmbeddingJson { get; set; }
}
