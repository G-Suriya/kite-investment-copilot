namespace KiteCopilot.Domain.Models;

public class KiteProfile
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserType { get; set; } = "individual";
    public string Email { get; set; } = string.Empty;
    public string Broker { get; set; } = "ZERODHA";
    public bool IsConnected { get; set; }
    public DateTime? ConnectedAt { get; set; }
}

public class Holding
{
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = "NSE";
    public string InstrumentToken { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int T1Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal LastPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal PnL => (LastPrice - AveragePrice) * Quantity;
    public decimal PnLPercent => AveragePrice > 0 ? ((LastPrice - AveragePrice) / AveragePrice) * 100 : 0;
    public decimal DayChange => LastPrice - ClosePrice;
    public decimal DayChangePercent => ClosePrice > 0 ? ((LastPrice - ClosePrice) / ClosePrice) * 100 : 0;
    public decimal CurrentValue => LastPrice * Quantity;
    public decimal InvestedValue => AveragePrice * Quantity;
}

public class Position
{
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = "NSE";
    public string Product { get; set; } = "CNC"; // CNC, MIS, NRML
    public int Quantity { get; set; }
    public int BuyQuantity { get; set; }
    public int SellQuantity { get; set; }
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public decimal LastPrice { get; set; }
    public decimal PnL { get; set; }
    public decimal Value => LastPrice * Math.Abs(Quantity);
}

public class Funds
{
    public decimal AvailableMargin { get; set; }
    public decimal UsedMargin { get; set; }
    public decimal CashBalance { get; set; }
    public decimal CollateralMargin { get; set; }
    public decimal TotalMargin => AvailableMargin + UsedMargin;
    public string Currency { get; set; } = "INR";
}

public class Order
{
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = "NSE";
    public string TransactionType { get; set; } = "BUY"; // BUY, SELL
    public string OrderType { get; set; } = "LIMIT"; // LIMIT, MARKET, SL
    public string Product { get; set; } = "CNC";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = "COMPLETE"; // COMPLETE, REJECTED, CANCELLED, OPEN
    public string? StatusMessage { get; set; }
    public DateTime OrderTimestamp { get; set; } = DateTime.UtcNow;
}

public class MarketQuote
{
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = "NSE";
    public string CompanyName { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal PreviousClose { get; set; }
    public long Volume { get; set; }
    public decimal? PeRatio { get; set; }
    public decimal? PbRatio { get; set; }
    public decimal? MarketCapCrores { get; set; }
    public decimal FiftyTwoWeekHigh { get; set; }
    public decimal FiftyTwoWeekLow { get; set; }
    public decimal? DividendYield { get; set; }
    public string? Sector { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class KiteSessionInfo
{
    public bool IsAuthenticated { get; set; }
    public bool IsDemoMode { get; set; } = true;
    public string? UserName { get; set; }
    public string? UserId { get; set; }
    public DateTime? LoginTime { get; set; }
    public string ApiKeyMasked { get; set; } = string.Empty;
}
