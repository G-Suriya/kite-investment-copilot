namespace KiteCopilot.Domain.Models;

public class SyncKiteHoldingsResult
{
    public int TotalSynced { get; set; }
    public int StocksCount { get; set; }
    public int EtfsCount { get; set; }
    public int IposCount { get; set; }
    public List<string> SyncedSymbols { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
