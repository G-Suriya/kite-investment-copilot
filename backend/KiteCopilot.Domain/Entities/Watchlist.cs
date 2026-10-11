using KiteCopilot.Domain.Enums;

namespace KiteCopilot.Domain.Entities;

public class Watchlist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Custom"; // e.g. "Stocks", "ETFs", "IPOs", "Custom"
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<WatchlistItem> Items { get; set; } = new();
}

public class WatchlistItem
{
    public int Id { get; set; }
    public int WatchlistId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = "NSE"; // NSE, BSE
    public AssetType AssetType { get; set; } = AssetType.STOCK;
    public string? CompanyName { get; set; }
    public string? Notes { get; set; }
    public decimal? TargetPrice { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public Watchlist? Watchlist { get; set; }
}
