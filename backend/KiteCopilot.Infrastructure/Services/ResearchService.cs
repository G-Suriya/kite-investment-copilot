using KiteCopilot.Application.DTOs;
using KiteCopilot.Application.Interfaces;
using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;
using KiteCopilot.Domain.Models;

namespace KiteCopilot.Infrastructure.Services;

public class ResearchService : IResearchService
{
    private readonly IKiteService _kiteService;
    private readonly IRagService _ragService;
    private readonly IThesisService _thesisService;

    public ResearchService(IKiteService kiteService, IRagService ragService, IThesisService thesisService)
    {
        _kiteService = kiteService;
        _ragService = ragService;
        _thesisService = thesisService;
    }

    public async Task<StockAnalysisDto> GetStockAnalysisAsync(string symbol)
    {
        var cleanSymbol = symbol.Trim().ToUpperInvariant();
        var quote = await _kiteService.GetQuoteAsync(cleanSymbol);
        var holdings = await _kiteService.GetHoldingsAsync();
        var currentHolding = holdings.FirstOrDefault(h => h.Symbol.Equals(cleanSymbol, StringComparison.OrdinalIgnoreCase));
        var thesis = await _thesisService.GetThesisBySymbolAsync(cleanSymbol);
        var filingExcerpts = await _ragService.SearchDocumentsAsync($"{cleanSymbol} financial results revenue profit", cleanSymbol, limit: 2);

        var analysis = new StockAnalysisDto
        {
            Symbol = cleanSymbol,
            CompanyName = quote.CompanyName,
            Quote = quote,
            CurrentHolding = currentHolding,
            UserThesis = thesis,
            KeyFilingExcerpts = filingExcerpts.ToList()
        };

        if (cleanSymbol == "INFY")
        {
            analysis.BusinessSummary = "Infosys Limited is a global leader in next-generation digital services and consulting, serving Fortune 500 enterprises in 56+ countries.";
            analysis.FinancialHighlights = new()
            {
                ["Revenue Growth (YoY)"] = "5.1% Constant Currency",
                ["Operating Margin"] = "21.5% (+40 bps QoQ)",
                ["Free Cash Flow Conversion"] = "92% of Net Income",
                ["Dividend Yield"] = "2.45%",
                ["Large Deal TCV"] = "$3.2 Billion (56% net new)"
            };
            analysis.RisksAndWatchlistPoints = new()
            {
                "North American discretionary tech spending slowdown",
                "Pricing compression on legacy application maintenance contracts",
                "Sub-contractor expense inflation"
            };
        }
        else if (cleanSymbol == "TCS")
        {
            analysis.BusinessSummary = "Tata Consultancy Services is the flagship IT enterprise of Tata Group with premier industry margins and extensive BFSI dominance.";
            analysis.FinancialHighlights = new()
            {
                ["Consolidated Revenue"] = "₹64,259 Crore",
                ["Operating Margin"] = "24.8% (Industry leading)",
                ["BFSI Sequential Growth"] = "+2.1% QoQ",
                ["Headcount Net Addition"] = "+5,726 employees",
                ["Order Book TCV"] = "$8.6 Billion"
            };
            analysis.RisksAndWatchlistPoints = new()
            {
                "UK and European macro uncertainty impacting BFSI transformation velocity",
                "Cross-currency headwinds on GBP and EUR receivables"
            };
        }
        else if (cleanSymbol == "HDFCBANK")
        {
            analysis.BusinessSummary = "HDFC Bank is India's largest private sector bank with over 8,500 branches, serving retail, SME, and wholesale corporate ecosystems.";
            analysis.FinancialHighlights = new()
            {
                ["Q2 Net Profit"] = "₹16,821 Crore",
                ["Deposit Growth"] = "15.4% YoY",
                ["Gross NPA"] = "1.33% (down from 1.39%)",
                ["Net Interest Margin"] = "3.46%",
                ["Credit-to-Deposit Ratio"] = "85.8% (improving towards <85%)"
            };
            analysis.RisksAndWatchlistPoints = new()
            {
                "Systemic deposit mobilization costs and high CASA competition",
                "Unsecured retail personal loan delinquencies in industry"
            };
        }
        else
        {
            analysis.BusinessSummary = $"{quote.CompanyName} ({quote.Symbol}) is listed on {quote.Exchange} in the {quote.Sector ?? "Equities"} sector.";
            analysis.FinancialHighlights = new()
            {
                ["Current Price"] = $"₹{quote.LastPrice:N2}",
                ["Day Change"] = $"{quote.ChangePercent:+0.00;-0.00}%",
                ["52-Week Range"] = $"₹{quote.FiftyTwoWeekLow:N2} - ₹{quote.FiftyTwoWeekHigh:N2}",
                ["P/E Ratio"] = quote.PeRatio.HasValue ? $"{quote.PeRatio.Value:F1}x" : "N/A",
                ["Volume"] = $"{quote.Volume:N0}"
            };
            analysis.RisksAndWatchlistPoints = new()
            {
                "Market volatility and valuation rerating",
                "Verify upcoming quarterly corporate disclosures"
            };
        }

        return analysis;
    }

    public async Task<EtfAnalysisDto> GetEtfAnalysisAsync(string symbol)
    {
        var cleanSymbol = symbol.Trim().ToUpperInvariant();
        var quote = await _kiteService.GetQuoteAsync(cleanSymbol);

        if (cleanSymbol == "MON100")
        {
            return new EtfAnalysisDto
            {
                Symbol = "MON100",
                Name = "Motilal Oswal Nasdaq 100 ETF",
                UnderlyingIndex = "Nasdaq-100 TRI (USD)",
                Quote = quote,
                ExpenseRatio = 0.54m,
                TrackingError = 0.18m,
                AumCrores = 8240m,
                TopHoldings = new() { "Apple (9.1%)", "Microsoft (8.8%)", "Nvidia (8.4%)", "Amazon (5.6%)", "Meta Platforms (4.9%)", "Alphabet Class A (3.2%)" },
                SectorDistribution = "Information Technology 51.2%, Consumer Discretionary 18.4%, Communication 15.1%, Healthcare 6.2%",
                ThesisSuitability = "Ideal for long-term investors seeking global technology innovation leadership and hedge against Indian Rupee (INR) depreciation.",
                WatchPoints = new() { "Intraday NAV premium/discount", "US Federal Reserve interest rate moves", "Big Tech antitrust regulatory pressure" }
            };
        }

        if (cleanSymbol == "NIFTYBEES")
        {
            return new EtfAnalysisDto
            {
                Symbol = "NIFTYBEES",
                Name = "Nippon India Nifty 50 BeES ETF",
                UnderlyingIndex = "Nifty 50 Total Return Index (INR)",
                Quote = quote,
                ExpenseRatio = 0.04m,
                TrackingError = 0.03m,
                AumCrores = 24150m,
                TopHoldings = new() { "HDFC Bank (11.8%)", "Reliance Industries (9.4%)", "ICICI Bank (7.6%)", "Infosys (5.8%)", "TCS (4.1%)", "Bharti Airtel (3.9%)" },
                SectorDistribution = "Financial Services 32.8%, IT 14.2%, Oil & Gas 11.5%, FMCG 8.4%, Automobile 7.2%",
                ThesisSuitability = "Ultra low-cost institutional proxy for core Indian economic compounding with highest market liquidity.",
                WatchPoints = new() { "RBI repo rate commentary", "Foreign institutional investor (FII) flows", "Gross corporate earnings trajectory" }
            };
        }

        // Generic ETF
        return new EtfAnalysisDto
        {
            Symbol = cleanSymbol,
            Name = quote.CompanyName,
            UnderlyingIndex = "Benchmark Exchange Index",
            Quote = quote,
            ExpenseRatio = 0.25m,
            TrackingError = 0.12m,
            AumCrores = 1200m,
            TopHoldings = new() { "Top Constituent 1 (12%)", "Top Constituent 2 (9%)", "Top Constituent 3 (8%)" },
            SectorDistribution = "Diversified Sector Allocation",
            ThesisSuitability = "Passive portfolio diversification and lower beta risk.",
            WatchPoints = new() { "Tracking difference", "Liquidity and average daily traded volume" }
        };
    }

    public async Task<IpoAnalysisDto> GetIpoAnalysisAsync(string symbol)
    {
        var cleanSymbol = symbol.Trim().ToUpperInvariant();

        if (cleanSymbol == "SWIGGY")
        {
            return new IpoAnalysisDto
            {
                Symbol = "SWIGGY",
                CompanyName = "Swiggy Limited",
                IssuePeriod = "Recent Listing / Active Watch",
                PriceBand = "₹371 - ₹390",
                IssueSizeCrores = 11327m,
                FreshIssueCrores = 4499m,
                OfsCrores = 6828m,
                PostIssuePe = "Negative (Adj EBITDA turning positive)",
                PromoterHoldingPrePost = "Professionally managed (No promoters)",
                Strengths = new()
                {
                    "Dominant duopoly in Indian food delivery (45%+ market share)",
                    "Instamart quick commerce hyper-growth with 650+ dark stores",
                    "Integrated membership (Swiggy One) driving sticky cross-platform loyalty"
                },
                KeyRisks = new()
                {
                    "Intense price competition against Zomato/Blinkit and Zepto",
                    "Dark store expansion capex dragging near-term consolidated margins",
                    "Gig economy worker minimum wage legislation scrutiny"
                },
                Status = "LISTED"
            };
        }

        if (cleanSymbol == "HYUNDAI")
        {
            return new IpoAnalysisDto
            {
                Symbol = "HYUNDAI",
                CompanyName = "Hyundai Motor India Limited",
                IssuePeriod = "Recent Listing",
                PriceBand = "₹1,865 - ₹1,960",
                IssueSizeCrores = 27870m,
                FreshIssueCrores = 0m,
                OfsCrores = 27870m,
                PostIssuePe = "26.5x FY24 P/E",
                PromoterHoldingPrePost = "100% -> 82.5%",
                Strengths = new()
                {
                    "Second largest passenger vehicle manufacturer in India (14.6% market share)",
                    "High SUV mix (>67% of sales) commanding superior operating margins",
                    "State of the art Talegaon plant acquisition increasing annual capacity to 1M units"
                },
                KeyRisks = new()
                {
                    "Entire issue was 100% Offer For Sale (zero fresh capital into India ops)",
                    "Rising royalty payout (3.5% of revenue) to Korean parent company",
                    "Increasing competition in mid-size electric vehicle SUV segment"
                },
                Status = "LISTED"
            };
        }

        // Generic IPO
        return new IpoAnalysisDto
        {
            Symbol = cleanSymbol,
            CompanyName = cleanSymbol,
            IssuePeriod = "Upcoming Listing",
            PriceBand = "₹100 - ₹120",
            IssueSizeCrores = 1200m,
            FreshIssueCrores = 800m,
            OfsCrores = 400m,
            PostIssuePe = "22x",
            PromoterHoldingPrePost = "75% -> 58%",
            Strengths = new() { "Strong industry tailwinds", "Demonstrated operational profitability" },
            KeyRisks = new() { "Valuation multiple expansion", "Working capital cycle" },
            Status = "UPCOMING"
        };
    }

    public async Task<IReadOnlyList<MarketQuote>> CompareAssetsAsync(IEnumerable<string> symbols)
    {
        return await _kiteService.GetQuotesAsync(symbols);
    }
}
