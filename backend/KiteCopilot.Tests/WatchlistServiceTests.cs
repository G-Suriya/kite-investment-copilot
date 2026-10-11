using KiteCopilot.Domain.Enums;
using KiteCopilot.Infrastructure.Data;
using KiteCopilot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KiteCopilot.Tests;

public class WatchlistServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateWatchlist_ShouldPersistNewWatchlist()
    {
        var context = CreateContext();
        var service = new WatchlistService(context, NullLogger<WatchlistService>.Instance);

        var wl = await service.CreateWatchlistAsync("Tech Compounders", "Stocks", "High growth software tech");

        Assert.NotNull(wl);
        Assert.Equal("Tech Compounders", wl.Name);
        var retrieved = await service.GetWatchlistByIdAsync(wl.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Tech Compounders", retrieved.Name);
    }

    [Fact]
    public async Task AddItemAsync_ShouldAddStockAndPreventDuplicates()
    {
        var context = CreateContext();
        var service = new WatchlistService(context, NullLogger<WatchlistService>.Instance);
        var wl = await service.CreateWatchlistAsync("ETF Watch", "ETFs");

        var item1 = await service.AddItemAsync(wl.Id, "MON100", "NSE", AssetType.ETF, "US Nasdaq proxy");
        var item2 = await service.AddItemAsync(wl.Id, "MON100", "NSE", AssetType.ETF, "Updated notes");

        Assert.Equal(item1.Id, item2.Id);
        var updatedWl = await service.GetWatchlistByIdAsync(wl.Id);
        Assert.Single(updatedWl!.Items);
        Assert.Equal("Updated notes", updatedWl.Items[0].Notes);
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldRemoveExistingStock()
    {
        var context = CreateContext();
        var service = new WatchlistService(context, NullLogger<WatchlistService>.Instance);
        var wl = await service.CreateWatchlistAsync("Long Term", "Stocks");
        await service.AddItemAsync(wl.Id, "TCS", "NSE", AssetType.STOCK);

        var removed = await service.RemoveItemAsync(wl.Id, "TCS");
        Assert.True(removed);

        var updatedWl = await service.GetWatchlistByIdAsync(wl.Id);
        Assert.Empty(updatedWl!.Items);
    }
}
