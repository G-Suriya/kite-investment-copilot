using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;
using KiteCopilot.Domain.Models;

namespace KiteCopilot.Application.Interfaces;

public interface IWatchlistService
{
    Task<IReadOnlyList<Watchlist>> GetWatchlistsAsync();
    Task<Watchlist?> GetWatchlistByIdAsync(int id);
    Task<Watchlist?> GetWatchlistByNameAsync(string name);
    Task<Watchlist> CreateWatchlistAsync(string name, string type = "Custom", string? description = null);
    Task<bool> RenameWatchlistAsync(int id, string newName);
    Task<bool> DeleteWatchlistAsync(int id);
    Task<WatchlistItem> AddItemAsync(int watchlistId, string symbol, string exchange = "NSE", AssetType assetType = AssetType.STOCK, string? notes = null, decimal? targetPrice = null);
    Task<bool> RemoveItemAsync(int watchlistId, string symbol);
    Task<SyncKiteHoldingsResult> SyncKiteHoldingsAsync(IReadOnlyList<Holding> holdings);
}

