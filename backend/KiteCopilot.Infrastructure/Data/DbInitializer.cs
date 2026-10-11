using System.Text.Json;
using KiteCopilot.Application.Interfaces;
using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KiteCopilot.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        await context.Database.EnsureCreatedAsync();

        if (!await context.Watchlists.AnyAsync())
        {
            logger.LogInformation("Seeding live Kite watchlists...");
            var holdingsList = new Watchlist
            {
                Name = "Kite Portfolio Holdings",
                Type = "All",
                Description = "All live holdings imported directly from connected Zerodha Kite account",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<WatchlistItem>
                {
                    new() { Symbol = "GOLDBEES", Exchange = "BSE", AssetType = AssetType.ETF, CompanyName = "Nippon India ETF Gold BeES", TargetPrice = 149.26m, Notes = "Sector: Commodities / Gold ETF | Live holding: 40 shares @ ₹129.79", AddedAt = DateTime.UtcNow.AddDays(-10) },
                    new() { Symbol = "MON100", Exchange = "BSE", AssetType = AssetType.ETF, CompanyName = "Motilal Oswal Nasdaq 100 ETF", TargetPrice = 393.93m, Notes = "Sector: International Tech Index ETF | Live holding: 5 shares @ ₹342.55", AddedAt = DateTime.UtcNow.AddDays(-10) },
                    new() { Symbol = "HDFCBANK", Exchange = "NSE", AssetType = AssetType.STOCK, CompanyName = "HDFC Bank Limited", TargetPrice = 875.61m, Notes = "Sector: Banking & Financial Services | Live holding: 20 shares @ ₹761.40", AddedAt = DateTime.UtcNow.AddDays(-10) },
                    new() { Symbol = "HINDUNILVR", Exchange = "BSE", AssetType = AssetType.STOCK, CompanyName = "Hindustan Unilever Limited", TargetPrice = 2586.29m, Notes = "Sector: FMCG & Consumer Goods | Live holding: 2 shares @ ₹2248.95", AddedAt = DateTime.UtcNow.AddDays(-10) },
                    new() { Symbol = "ITC", Exchange = "BSE", AssetType = AssetType.STOCK, CompanyName = "ITC Limited", TargetPrice = 322.98m, Notes = "Sector: FMCG & Conglomerates | Live holding: 1 shares @ ₹280.85", AddedAt = DateTime.UtcNow.AddDays(-10) },
                    new() { Symbol = "TCS", Exchange = "BSE", AssetType = AssetType.STOCK, CompanyName = "Tata Consultancy Services Ltd", TargetPrice = 2616.53m, Notes = "Sector: Information Technology Services | Live holding: 13 shares @ ₹2275.25", AddedAt = DateTime.UtcNow.AddDays(-10) }
                }
            };

            var stocksList = new Watchlist
            {
                Name = "Kite Stocks",
                Type = "Stocks",
                Description = "Direct equities and stock investments from Kite account",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<WatchlistItem>
                {
                    new() { Symbol = "HDFCBANK", Exchange = "NSE", AssetType = AssetType.STOCK, CompanyName = "HDFC Bank Limited", TargetPrice = 875.61m, Notes = "Sector: Banking & Financial Services | Live holding: 20 shares @ ₹761.40", AddedAt = DateTime.UtcNow.AddDays(-10) },
                    new() { Symbol = "HINDUNILVR", Exchange = "BSE", AssetType = AssetType.STOCK, CompanyName = "Hindustan Unilever Limited", TargetPrice = 2586.29m, Notes = "Sector: FMCG & Consumer Goods | Live holding: 2 shares @ ₹2248.95", AddedAt = DateTime.UtcNow.AddDays(-10) },
                    new() { Symbol = "ITC", Exchange = "BSE", AssetType = AssetType.STOCK, CompanyName = "ITC Limited", TargetPrice = 322.98m, Notes = "Sector: FMCG & Conglomerates | Live holding: 1 shares @ ₹280.85", AddedAt = DateTime.UtcNow.AddDays(-10) },
                    new() { Symbol = "TCS", Exchange = "BSE", AssetType = AssetType.STOCK, CompanyName = "Tata Consultancy Services Ltd", TargetPrice = 2616.53m, Notes = "Sector: Information Technology Services | Live holding: 13 shares @ ₹2275.25", AddedAt = DateTime.UtcNow.AddDays(-10) }
                }
            };

            var etfsList = new Watchlist
            {
                Name = "Kite ETFs",
                Type = "ETFs",
                Description = "Exchange Traded Funds, Gold BeES and index trackers from Kite account",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<WatchlistItem>
                {
                    new() { Symbol = "GOLDBEES", Exchange = "BSE", AssetType = AssetType.ETF, CompanyName = "Nippon India ETF Gold BeES", TargetPrice = 149.26m, Notes = "Sector: Commodities / Gold ETF | Live holding: 40 shares @ ₹129.79", AddedAt = DateTime.UtcNow.AddDays(-10) },
                    new() { Symbol = "MON100", Exchange = "BSE", AssetType = AssetType.ETF, CompanyName = "Motilal Oswal Nasdaq 100 ETF", TargetPrice = 393.93m, Notes = "Sector: International Tech Index ETF | Live holding: 5 shares @ ₹342.55", AddedAt = DateTime.UtcNow.AddDays(-10) }
                }
            };

            context.Watchlists.AddRange(holdingsList, stocksList, etfsList);
            await context.SaveChangesAsync();
        }

        if (!await context.InvestmentNotes.AnyAsync())
        {
            logger.LogInformation("Seeding personal investment theses...");
            var theses = new List<InvestmentNote>
            {
                new()
                {
                    Symbol = "INFY",
                    Title = "Infosys: Generative AI Cloud Deals & Margin Resilience",
                    Content = "Infosys is positioned to benefit from enterprise GenAI migration via Topaz suite. Deal pipeline exceeds $14B with 52% net new large deals. Operating margins have bottomed around 21% and are expanding towards 22-23%. High free cash flow conversion (>85%) and 80%+ dividend payout policy.",
                    Thesis = "Long-term compounder on global digital transformation and AI integration. Resilient free cash flow, margin expansion in AI cloud services, and capital return discipline.",
                    TimeHorizon = "Long-Term (3-5 Years)",
                    KeyRisks = "US discretionary IT spending delay, European macroeconomic slowdown, wage inflation.",
                    WatchPoints = "Quarterly large deal total contract value (TCV), attrition rate, Topaz enterprise customer wins.",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new()
                {
                    Symbol = "MON100",
                    Title = "MON100: Nasdaq 100 Structural Tech Supercycle",
                    Content = "Motilal Oswal Nasdaq 100 ETF tracks the 100 largest non-financial companies listed on Nasdaq. Core exposure to global AI infrastructure (Nvidia, Microsoft, Apple, Alphabet, Amazon). Offers intrinsic INR depreciation hedge (~3-4% annual historic tailwind for Indian investors).",
                    Thesis = "Core satellite holding for global tech secular growth and AI supercycle with currency depreciation shield.",
                    TimeHorizon = "Long-Term (5+ Years)",
                    KeyRisks = "US Federal Reserve interest rate policy, Big Tech anti-trust regulatory actions, high valuation multiples.",
                    WatchPoints = "Tracking error, expense ratio (0.54%), premium/discount to intraday NAV.",
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    UpdatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new()
                {
                    Symbol = "HDFCBANK",
                    Title = "HDFC Bank: Post-Merger Normalization & NIM Expansion",
                    Content = "Post-merger transition with parent HDFC Ltd is resolving. Deposit mobilization has accelerated with ~18% YoY growth, outpacing credit growth to restore the Credit-to-Deposit (CD) ratio down to ~86%. RoA is expected to expand from 1.7% back to pre-merger 1.95-2.00% by FY27.",
                    Thesis = "High conviction turnaround in return metrics as branch additions mature and wholesale borrowing is replaced by low-cost retail CASA deposits.",
                    TimeHorizon = "Medium-Term (2-3 Years)",
                    KeyRisks = "Intense deposit competition across Indian banking sector, unsecured retail loan asset quality pressure.",
                    WatchPoints = "Incremental CD ratio, CASA ratio, net interest margin (NIM) trajectory, branch productivity.",
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            foreach (var thesis in theses)
            {
                var vec = await embeddingService.GenerateEmbeddingAsync($"{thesis.Symbol} {thesis.Title} {thesis.Thesis} {thesis.Content}");
                thesis.EmbeddingJson = JsonSerializer.Serialize(vec);
                context.InvestmentNotes.Add(thesis);
            }

            await context.SaveChangesAsync();
        }

        if (!await context.ResearchDocuments.AnyAsync())
        {
            logger.LogInformation("Seeding RAG research documents & filings...");
            var docs = new List<(string Symbol, string Company, AssetType AssetType, DocumentType DocType, string Title, string Content, int Year, string Quarter, string Source)>
            {
                (
                    "INFY", "Infosys Limited", AssetType.STOCK, DocumentType.QuarterlyResult,
                    "Infosys Q2 FY26 Earnings Release & Business Highlights",
                    "Infosys delivered strong constant currency revenue growth of 3.8% QoQ and 5.1% YoY in Q2 FY26. Operating margin expanded by 40 bps to 21.5% driven by Project Maximus operational efficiencies. Large deal TCV stood at $3.2 Billion with 56% net new. Topaz generative AI suite surpassed 300 active enterprise client engagements. Management revised constant currency revenue guidance upward to 4.5%-5.0% for FY26. Free cash flow conversion stood at 92% of net profit with interim dividend of ₹21 per share.",
                    2026, "Q2", "BSE/NSE Filing"
                ),
                (
                    "TCS", "Tata Consultancy Services Ltd", AssetType.STOCK, DocumentType.QuarterlyResult,
                    "TCS Q2 FY26 Financial Statement & MD Commentary",
                    "Tata Consultancy Services reported consolidated Q2 FY26 revenue of ₹64,259 crore, up 4.2% YoY. Operating margin remained industry-leading at 24.8%. BFSI sector showed definitive recovery signs in North America, growing 2.1% sequentially. AI and Cloud deal pipeline reached record levels. Headcount increased by 5,726 employees, signaling confidence in multi-year enterprise transformation demand. Order book TCV was $8.6 Billion.",
                    2026, "Q2", "BSE/NSE Filing"
                ),
                (
                    "HDFCBANK", "HDFC Bank Limited", AssetType.STOCK, DocumentType.QuarterlyResult,
                    "HDFC Bank Q2 FY26 Earnings Conference Call Transcript & Investor Deck",
                    "HDFC Bank reported net profit of ₹16,821 crore for Q2 FY26. Net Interest Margin (NIM) stabilized at 3.46% on total assets. Deposit growth stood at 15.4% YoY with retail deposits constituting 84% of total deposits. Gross NPA ratio improved to 1.33% from 1.39% in the previous quarter. The management reiterated commitment to calibrate credit growth below deposit growth to bring Credit-Deposit ratio below 85% within next two quarters.",
                    2026, "Q2", "Investor Relations"
                ),
                (
                    "MON100", "Motilal Oswal Nasdaq 100 ETF", AssetType.ETF, DocumentType.Factsheet,
                    "Motilal Oswal Nasdaq 100 ETF Portfolio Factsheet & Tracking Update",
                    "The Motilal Oswal Nasdaq 100 ETF (Symbol: MON100) provides exposure to the 100 largest global non-financial giants. Total AUM exceeds ₹8,200 crore. Total Expense Ratio is 0.54%. 1-year annualized tracking error is 0.18%. Top sector allocations: Information Technology (51.2%), Consumer Discretionary (18.4%), Communication Services (15.1%). Top constituents include Apple (9.1%), Microsoft (8.8%), Nvidia (8.4%), Amazon (5.6%), Meta (4.9%). Suitable for investors looking for international diversification and hedge against domestic currency risk.",
                    2026, "Q2", "Fund Factsheet"
                ),
                (
                    "NIFTYBEES", "Nippon India Nifty 50 BeES ETF", AssetType.ETF, DocumentType.Factsheet,
                    "Nippon India Nifty 50 BeES ETF Key Information Memorandum",
                    "Nippon India ETF Nifty 50 BeES is India's largest and most liquid ETF with AUM exceeding ₹24,000 crore. Average daily trading volume on NSE exceeds 2.5 million units. Total expense ratio is ultra-low at 0.04% per annum. Tracking error against Nifty 50 Total Return Index is 0.03%. Sector composition: Financial Services (32.8%), IT (14.2%), Oil & Gas (11.5%), Fast Moving Consumer Goods (8.4%). Dividend yield equivalent re-invested into NAV.",
                    2026, "Q2", "Fund Factsheet"
                ),
                (
                    "SWIGGY", "Swiggy Limited", AssetType.IPO, DocumentType.DRHP_RHP,
                    "Swiggy Limited Red Herring Prospectus (RHP) & Operational Metric Analysis",
                    "Swiggy Limited operates two core consumer businesses: Food Delivery and Instamart Quick Commerce. Food Delivery gross order value (GOV) grew 16% YoY with adjusted EBITDA margin of 2.8% of GOV. Instamart operates 650 active dark stores across top 45 cities with average delivery time of 12.4 minutes. Instamart contribution margin improved from -4.5% to -1.1% of GOV as average order value (AOV) increased to ₹485. Fresh issue proceeds of ₹4,499 crore allocated to dark store expansion and technology infrastructure.",
                    2026, "Q2", "SEBI DRHP/RHP"
                )
            };

            foreach (var doc in docs)
            {
                var researchDoc = new ResearchDocument
                {
                    Symbol = doc.Symbol,
                    Company = doc.Company,
                    AssetType = doc.AssetType,
                    DocumentType = doc.DocType,
                    Title = doc.Title,
                    Content = doc.Content,
                    FinancialYear = doc.Year,
                    Quarter = doc.Quarter,
                    Source = doc.Source,
                    PublishedDate = DateTime.UtcNow.AddDays(-15),
                    CreatedAt = DateTime.UtcNow
                };

                // Split content into chunks
                var chunks = SplitIntoChunks(doc.Content, chunkSize: 300, overlap: 50);
                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunkText = chunks[i];
                    var vec = await embeddingService.GenerateEmbeddingAsync($"{doc.Symbol} {doc.Title} {chunkText}");
                    researchDoc.Chunks.Add(new DocumentChunk
                    {
                        ChunkIndex = i,
                        Content = chunkText,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            symbol = doc.Symbol,
                            company = doc.Company,
                            docType = doc.DocType.ToString(),
                            year = doc.Year,
                            quarter = doc.Quarter
                        }),
                        EmbeddingJson = JsonSerializer.Serialize(vec),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                context.ResearchDocuments.Add(researchDoc);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Database seeding complete.");
        }
    }

    private static List<string> SplitIntoChunks(string text, int chunkSize = 300, int overlap = 50)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        if (text.Length <= chunkSize)
        {
            result.Add(text);
            return result;
        }

        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(chunkSize, text.Length - start);
            result.Add(text.Substring(start, length));
            start += (chunkSize - overlap);
            if (start + overlap >= text.Length) break;
        }

        return result;
    }
}
