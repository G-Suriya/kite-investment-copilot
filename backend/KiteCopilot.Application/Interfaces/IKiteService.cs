using KiteCopilot.Domain.Models;

namespace KiteCopilot.Application.Interfaces;

public interface IKiteService
{
    bool IsAuthenticated { get; }
    bool IsDemoMode { get; }
    void SetDemoMode(bool isDemo);
    string GetLoginUrl();
    Task<bool> ExchangeTokenAsync(string requestToken);
    Task<KiteProfile> GetProfileAsync();
    Task<Funds> GetFundsAsync();
    Task<IReadOnlyList<Holding>> GetHoldingsAsync();
    Task<IReadOnlyList<Position>> GetPositionsAsync();
    Task<IReadOnlyList<Order>> GetOrdersAsync();
    Task<MarketQuote> GetQuoteAsync(string symbol, string exchange = "NSE");
    Task<IReadOnlyList<MarketQuote>> GetQuotesAsync(IEnumerable<string> symbols);
}
