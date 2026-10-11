using KiteCopilot.Domain.Models;

namespace KiteCopilot.Application.DTOs;

public class IpoGmpItemDto
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string PriceBand { get; set; } = string.Empty;
    public decimal IssuePrice { get; set; }
    public decimal Gmp { get; set; }
    public decimal GmpPercent { get; set; }
    public decimal ExpectedListingPrice { get; set; }
    public string GmpTrend { get; set; } = "UP"; // UP, DOWN, STABLE
    public string Rating { get; set; } = "🔥 High Demand"; // 🔥 High Demand, Moderate, Muted
    public int LotSize { get; set; }
    public decimal IssueSizeCrores { get; set; }
    public decimal SubscriptionTimes { get; set; }
    public string OpenDate { get; set; } = string.Empty;
    public string CloseDate { get; set; } = string.Empty;
    public string ListingDate { get; set; } = string.Empty;
    public string Status { get; set; } = "OPEN"; // OPEN, UPCOMING, CLOSED, LISTED
    public string Sector { get; set; } = string.Empty;
    public string GmpExplanationSimple { get; set; } = string.Empty;

    // Groww-style & Customization fields
    public string IpoType { get; set; } = "Mainboard"; // Mainboard, SME
    public string DocUrl { get; set; } = string.Empty;
    public string LogoColor { get; set; } = "#2563EB";
    public string LogoInitials { get; set; } = string.Empty;
    public string OpeningDateDisplay { get; set; } = "To be announced";
    public string Stage { get; set; } = "UPCOMING"; // UPCOMING, OPEN, CLOSED, SUBSCRIPTION, ALLOTMENT
    public string Registrar { get; set; } = "Link Intime India";
    public string RegistrarUrl { get; set; } = "https://linkintime.co.in/initial_offer/public-issues.html";
    public decimal MinInvestment { get; set; } = 14850m;
    public decimal QibSubscriptionTimes { get; set; }
    public decimal NiiSubscriptionTimes { get; set; }
    public decimal RetailSubscriptionTimes { get; set; }
    public string AllotmentDate { get; set; } = string.Empty;
}

public class FuturesTractionDto
{
    public string Symbol { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public decimal SpotPrice { get; set; }
    public decimal FuturesPrice { get; set; }
    public decimal BasisPremiumDiscount { get; set; }
    public long OpenInterest { get; set; }
    public decimal OpenInterestChangePercent { get; set; }
    public string BuildupType { get; set; } = "Long Buildup"; // Long Buildup, Short Buildup, Short Covering, Long Unwinding
    public string BuildupDescription { get; set; } = string.Empty;
    public decimal PutCallRatio { get; set; }
    public string PcrSentiment { get; set; } = "Bullish"; // Bullish, Bearish, Neutral
    public decimal MaxPain { get; set; }
    public long VolumeContracts { get; set; }
}

public class MarketIndexDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public string Exchange { get; set; } = "NSE";
    public decimal High { get; set; }
    public decimal Low { get; set; }
}

public class MarketBreadthDto
{
    public int Advances { get; set; }
    public int Declines { get; set; }
    public int Unchanged { get; set; }
    public decimal AdvanceDeclineRatio { get; set; }
}

public class InstitutionalFlowsDto
{
    public decimal FiiNetCrores { get; set; }
    public decimal DiiNetCrores { get; set; }
    public decimal NetTotalCrores { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Interpretation { get; set; } = string.Empty;
}

public class StockMoverDto
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal VolumeCrores { get; set; }
    public string Sector { get; set; } = string.Empty;
}

public class SectorPerformanceDto
{
    public string SectorName { get; set; } = string.Empty;
    public decimal ChangePercent { get; set; }
    public string TopStock { get; set; } = string.Empty;
    public string Trend { get; set; } = "BULLISH";
}

public class MultiSourceMarketPulseDto
{
    public List<MarketIndexDto> MajorIndices { get; set; } = new();
    public MarketBreadthDto MarketBreadth { get; set; } = new();
    public InstitutionalFlowsDto InstitutionalFlows { get; set; } = new();
    public List<StockMoverDto> TopGainers { get; set; } = new();
    public List<StockMoverDto> TopLosers { get; set; } = new();
    public List<SectorPerformanceDto> SectorPerformance { get; set; } = new();
    public string MarketSummaryText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class QuarterlyFinancialDto
{
    public string Quarter { get; set; } = string.Empty; // e.g. Q2 FY26
    public int Year { get; set; }
    public decimal RevenueCrores { get; set; }
    public decimal EbitdaCrores { get; set; }
    public decimal NetProfitCrores { get; set; }
    public decimal OperatingMarginPercent { get; set; }
    public decimal YoYRevenueGrowthPercent { get; set; }
    public decimal YoYProfitGrowthPercent { get; set; }
}

public class AnnualFinancialDto
{
    public string Year { get; set; } = string.Empty; // e.g. FY24, FY25, FY26E
    public decimal RevenueCrores { get; set; }
    public decimal NetProfitCrores { get; set; }
    public decimal EbitdaCrores { get; set; }
    public decimal OperatingMarginPercent { get; set; }
}

public class CompanyFinancialsDto
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public decimal MarketCapCrores { get; set; }
    public decimal? PeRatio { get; set; }
    public decimal? PbRatio { get; set; }
    public decimal? RoePercent { get; set; }
    public decimal? RocePercent { get; set; }
    public decimal? DividendYieldPercent { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DayChangePercent { get; set; }

    public List<QuarterlyFinancialDto> QuarterlyResults { get; set; } = new();
    public List<AnnualFinancialDto> AnnualResults { get; set; } = new();

    // Why stock price is rising vs falling
    public List<string> WhyPriceIsRising { get; set; } = new();
    public List<string> WhyPriceIsFalling { get; set; } = new();
    public string SummaryVerdict { get; set; } = string.Empty;
}

public class TechnicalAnalysisDto
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal Ema20 { get; set; }
    public decimal Sma50 { get; set; }
    public decimal Sma200 { get; set; }
    public decimal Rsi14 { get; set; }
    public string RsiSignal { get; set; } = "Neutral";
    public string MacdSignal { get; set; } = "Bullish Momentum";
    public decimal PivotPoint { get; set; }
    public decimal Resistance1 { get; set; }
    public decimal Resistance2 { get; set; }
    public decimal Support1 { get; set; }
    public decimal Support2 { get; set; }
    public string TrendSignal { get; set; } = "Strong Uptrend";
    public string Eli5Summary { get; set; } = string.Empty;
}

public class ConceptExplanationDto
{
    public string Concept { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Eli5SimpleExplanation { get; set; } = string.Empty;
    public string EverydayAnalogy { get; set; } = string.Empty;
    public string ProQuantPerspective { get; set; } = string.Empty;
    public string PracticalExample { get; set; } = string.Empty;
    public string SafetyTip { get; set; } = string.Empty;
}

public class StockGrowthOpportunityDto
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal TargetPrice { get; set; }
    public decimal UpsidePotentialPercent { get; set; }
    public decimal? PeRatio { get; set; }
    public decimal? RoePercent { get; set; }
    public decimal? RocePercent { get; set; }
    public decimal YoYRevenueGrowthPercent { get; set; }
    public decimal YoYProfitGrowthPercent { get; set; }
    public string GrowthTheme { get; set; } = string.Empty;
    public string FullAnalysis { get; set; } = string.Empty;
    public List<string> Catalysts { get; set; } = new();
    public List<string> Risks { get; set; } = new();
    public string Recommendation { get; set; } = "BUY";
    public string TechnicalSetup { get; set; } = string.Empty;
    public string TimeHorizon { get; set; } = "1-2 Years";
}

public class IpoGrowthOpportunityDto
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string PriceBand { get; set; } = string.Empty;
    public decimal IssuePrice { get; set; }
    public decimal IssueSizeCrores { get; set; }
    public decimal Gmp { get; set; }
    public decimal GmpPercent { get; set; }
    public decimal ExpectedListingPrice { get; set; }
    public decimal SubscriptionTimes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public decimal YoYRevenueGrowthPercent { get; set; }
    public string FinancialHealthSummary { get; set; } = string.Empty;
    public string FullAnalysis { get; set; } = string.Empty;
    public List<string> Pros { get; set; } = new();
    public List<string> Cons { get; set; } = new();
}

public class GrowthOpportunitiesResponseDto
{
    public List<IpoGrowthOpportunityDto> TopIpos { get; set; } = new();
    public List<StockGrowthOpportunityDto> TopStocks { get; set; } = new();
    public string MarketContext { get; set; } = string.Empty;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}

