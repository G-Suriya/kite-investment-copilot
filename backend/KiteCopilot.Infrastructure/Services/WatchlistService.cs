using KiteCopilot.Application.Common.Interfaces;
using KiteCopilot.Application.Interfaces;
using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;
using KiteCopilot.Domain.Models;
using KiteCopilot.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KiteCopilot.Infrastructure.Services;

public class WatchlistService : IWatchlistService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<WatchlistService> _logger;

    public WatchlistService(IAppDbContext context, ILogger<WatchlistService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Watchlist>> GetWatchlistsAsync()
    {
        return await _context.Watchlists
            .Include(w => w.Items)
            .OrderBy(w => w.Id)
            .ToListAsync();
    }

    public async Task<Watchlist?> GetWatchlistByIdAsync(int id)
    {
        return await _context.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<Watchlist?> GetWatchlistByNameAsync(string name)
    {
        return await _context.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Name.ToLower() == name.Trim().ToLower());
    }

    public async Task<Watchlist> CreateWatchlistAsync(string name, string type = "Custom", string? description = null)
    {
        var cleanName = name.Trim();
        var existing = await _context.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Name.ToLower() == cleanName.ToLower());
        if (existing != null)
        {
            return existing;
        }

        var watchlist = new Watchlist
        {
            Name = cleanName,
            Type = type,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Watchlists.Add(watchlist);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created watchlist: {Name} (Type: {Type})", cleanName, type);
        return watchlist;
    }

    public async Task<bool> RenameWatchlistAsync(int id, string newName)
    {
        var wl = await _context.Watchlists.FindAsync(id);
        if (wl == null) return false;

        wl.Name = newName.Trim();
        wl.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWatchlistAsync(int id)
    {
        var wl = await _context.Watchlists.FindAsync(id);
        if (wl == null) return false;

        _context.Watchlists.Remove(wl);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<WatchlistItem> AddItemAsync(int watchlistId, string symbol, string exchange = "NSE", AssetType assetType = AssetType.STOCK, string? notes = null, decimal? targetPrice = null)
    {
        var cleanSymbol = symbol.Trim().ToUpperInvariant();
        var watchlist = await _context.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == watchlistId);

        if (watchlist == null)
        {
            throw new ArgumentException($"Watchlist with ID {watchlistId} was not found.");
        }

        var detectedType = AssetClassifier.DetectAssetType(cleanSymbol);
        var finalAssetType = (assetType == AssetType.STOCK && detectedType != AssetType.STOCK) ? detectedType : assetType;
        var companyName = AssetClassifier.GetCompanyName(cleanSymbol);

        var existingItem = watchlist.Items.FirstOrDefault(i => i.Symbol.Equals(cleanSymbol, StringComparison.OrdinalIgnoreCase));
        if (existingItem != null)
        {
            // Already present, update fields
            existingItem.AssetType = finalAssetType;
            if (string.IsNullOrWhiteSpace(existingItem.CompanyName) || existingItem.CompanyName == existingItem.Symbol)
            {
                existingItem.CompanyName = companyName;
            }
            if (!string.IsNullOrWhiteSpace(notes)) existingItem.Notes = notes;
            if (targetPrice.HasValue) existingItem.TargetPrice = targetPrice;
            await _context.SaveChangesAsync();
            return existingItem;
        }

        var item = new WatchlistItem
        {
            WatchlistId = watchlistId,
            Symbol = cleanSymbol,
            Exchange = exchange,
            AssetType = finalAssetType,
            CompanyName = companyName,
            Notes = notes,
            TargetPrice = targetPrice,
            AddedAt = DateTime.UtcNow
        };

        watchlist.UpdatedAt = DateTime.UtcNow;
        watchlist.Items.Add(item);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added {Symbol} to Watchlist {Name}", cleanSymbol, watchlist.Name);
        return item;
    }

    public async Task<bool> RemoveItemAsync(int watchlistId, string symbol)
    {
        var cleanSymbol = symbol.Trim().ToUpperInvariant();
        var item = await _context.WatchlistItems
            .FirstOrDefaultAsync(i => i.WatchlistId == watchlistId && i.Symbol.ToUpper() == cleanSymbol);

        if (item == null) return false;

        _context.WatchlistItems.Remove(item);
        var wl = await _context.Watchlists.FindAsync(watchlistId);
        if (wl != null) wl.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Removed {Symbol} from Watchlist {WatchlistId}", cleanSymbol, watchlistId);
        return true;
    }

    public async Task<SyncKiteHoldingsResult> SyncKiteHoldingsAsync(IReadOnlyList<Holding> holdings)
    {
        if (holdings == null || holdings.Count == 0)
        {
            return new SyncKiteHoldingsResult
            {
                TotalSynced = 0,
                Message = "No holdings found in Kite account to sync."
            };
        }

        // 1. Get or create the categorized Kite watchlists
        var allWl = await GetOrCreateWatchlistInternalAsync("Kite Portfolio Holdings", "All", "All live holdings imported directly from connected Zerodha Kite account");
        var stocksWl = await GetOrCreateWatchlistInternalAsync("Kite Stocks", "Stocks", "Direct equities and stock investments from Kite account");
        var etfsWl = await GetOrCreateWatchlistInternalAsync("Kite ETFs", "ETFs", "Exchange Traded Funds, Gold BeES and index trackers from Kite account");
        
        Watchlist? iposWl = null;

        int stocksCount = 0;
        int etfsCount = 0;
        int iposCount = 0;
        var syncedSymbols = new List<string>();

        foreach (var h in holdings)
        {
            var sym = h.Symbol.Trim().ToUpperInvariant();
            var assetType = AssetClassifier.DetectAssetType(sym);
            var compName = AssetClassifier.GetCompanyName(sym);
            var sector = AssetClassifier.GetSector(sym);
            var notes = $"Sector: {sector} | Live holding: {h.Quantity} shares @ ₹{h.AveragePrice:F2} | Current: ₹{h.LastPrice:F2} | P&L: ₹{h.PnL:F2} ({h.PnLPercent:F1}%)";
            var targetPrice = Math.Round(h.AveragePrice > 0 ? h.AveragePrice * 1.15m : h.LastPrice * 1.15m, 2);

            syncedSymbols.Add(sym);

            // Add/update to unified watchlist
            UpsertItemInternal(allWl, sym, h.Exchange, assetType, compName, notes, targetPrice);

            // Add/update to category-specific watchlist
            if (assetType == AssetType.STOCK)
            {
                stocksCount++;
                UpsertItemInternal(stocksWl, sym, h.Exchange, assetType, compName, notes, targetPrice);
            }
            else if (assetType == AssetType.ETF)
            {
                etfsCount++;
                UpsertItemInternal(etfsWl, sym, h.Exchange, assetType, compName, notes, targetPrice);
            }
            else if (assetType == AssetType.IPO)
            {
                iposCount++;
                if (iposWl == null)
                {
                    iposWl = await GetOrCreateWatchlistInternalAsync("Kite IPOs", "IPOs", "New-age listings & IPO allocations from Kite account");
                }
                UpsertItemInternal(iposWl, sym, h.Exchange, assetType, compName, notes, targetPrice);
            }
        }

        // Retroactively fix existing items across all database watchlists
        var allDbItems = await _context.WatchlistItems.ToListAsync();
        foreach (var item in allDbItems)
        {
            var detected = AssetClassifier.DetectAssetType(item.Symbol);
            if (item.AssetType == AssetType.STOCK && detected != AssetType.STOCK)
            {
                item.AssetType = detected;
            }
            if (string.IsNullOrWhiteSpace(item.CompanyName) || item.CompanyName == item.Symbol)
            {
                item.CompanyName = AssetClassifier.GetCompanyName(item.Symbol);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully synced {Count} holdings from Kite (Stocks: {Stocks}, ETFs: {ETFs}, IPOs: {IPOs})",
            holdings.Count, stocksCount, etfsCount, iposCount);

        return new SyncKiteHoldingsResult
        {
            TotalSynced = holdings.Count,
            StocksCount = stocksCount,
            EtfsCount = etfsCount,
            IposCount = iposCount,
            SyncedSymbols = syncedSymbols,
            Message = $"Successfully synced {holdings.Count} Kite assets into categorized watchlists ({stocksCount} Stocks, {etfsCount} ETFs)."
        };
    }

    private async Task<Watchlist> GetOrCreateWatchlistInternalAsync(string name, string type, string description)
    {
        var existing = await _context.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Name.ToLower() == name.Trim().ToLower());

        if (existing != null)
        {
            existing.Type = type;
            existing.Description = description;
            return existing;
        }

        var wl = new Watchlist
        {
            Name = name.Trim(),
            Type = type,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Watchlists.Add(wl);
        await _context.SaveChangesAsync();
        return wl;
    }

    private void UpsertItemInternal(Watchlist watchlist, string symbol, string exchange, AssetType assetType, string companyName, string notes, decimal targetPrice)
    {
        var item = watchlist.Items.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            item.Exchange = exchange;
            item.AssetType = assetType;
            item.CompanyName = companyName;
            item.Notes = notes;
            item.TargetPrice = targetPrice;
        }
        else
        {
            watchlist.Items.Add(new WatchlistItem
            {
                WatchlistId = watchlist.Id,
                Symbol = symbol,
                Exchange = exchange,
                AssetType = assetType,
                CompanyName = companyName,
                Notes = notes,
                TargetPrice = targetPrice,
                AddedAt = DateTime.UtcNow
            });
        }
        watchlist.UpdatedAt = DateTime.UtcNow;
    }
}

