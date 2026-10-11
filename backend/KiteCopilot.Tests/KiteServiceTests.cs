using KiteCopilot.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KiteCopilot.Tests;

public class KiteServiceTests
{
    [Fact]
    public async Task DemoMode_ShouldReturnHoldingsWithCalculatedPnL()
    {
        var config = new ConfigurationBuilder().Build();
        var client = new HttpClient();
        var kiteService = new KiteService(client, config, NullLogger<KiteService>.Instance);

        kiteService.SetDemoMode(true);
        var holdings = await kiteService.GetHoldingsAsync();

        Assert.NotEmpty(holdings);
        var infy = holdings.FirstOrDefault(h => h.Symbol == "INFY");
        Assert.NotNull(infy);
        Assert.True(infy.Quantity > 0);
        Assert.True(infy.CurrentValue > infy.InvestedValue);
        Assert.True(infy.PnL > 0);
    }

    [Fact]
    public async Task DemoMode_ShouldReturnFundsAndProfile()
    {
        var config = new ConfigurationBuilder().Build();
        var client = new HttpClient();
        var kiteService = new KiteService(client, config, NullLogger<KiteService>.Instance);

        var profile = await kiteService.GetProfileAsync();
        var funds = await kiteService.GetFundsAsync();

        Assert.Equal("ZD9821", profile.UserId);
        Assert.True(funds.AvailableMargin > 0);
    }
}
