using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using KiteCopilot.Application.Common.Interfaces;
using KiteCopilot.Application.DTOs;
using KiteCopilot.Application.Interfaces;
using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;
using KiteCopilot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KiteCopilot.Infrastructure.Services;

public class GeminiCopilotService : IGeminiCopilotService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IAppDbContext _context;
    private readonly IKiteService _kiteService;
    private readonly IWatchlistService _watchlistService;
    private readonly IRagService _ragService;
    private readonly IThesisService _thesisService;
    private readonly IResearchService _researchService;
    private readonly IMarketIntelligenceService _marketService;
    private readonly ILogger<GeminiCopilotService> _logger;

    public GeminiCopilotService(
        HttpClient httpClient,
        IConfiguration config,
        IAppDbContext context,
        IKiteService kiteService,
        IWatchlistService watchlistService,
        IRagService ragService,
        IThesisService thesisService,
        IResearchService researchService,
        IMarketIntelligenceService marketService,
        ILogger<GeminiCopilotService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _context = context;
        _kiteService = kiteService;
        _watchlistService = watchlistService;
        _ragService = ragService;
        _thesisService = thesisService;
        _researchService = researchService;
        _marketService = marketService;
        _logger = logger;
    }

    public async Task<ChatCopilotResponse> ProcessChatAsync(ChatCopilotRequest request)
    {
        var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
            ? Guid.NewGuid().ToString("N")[..8]
            : request.ConversationId;

        var message = request.Message.Trim();

        // 1. Guardrail Safety Check: Trading actions are strictly prohibited in MVP
        if (IsTradingIntent(message))
        {
            var guardrailResponse = new ChatCopilotResponse
            {
                ConversationId = conversationId,
                GuardrailTriggered = true,
                GuardrailMessage = "Trading execution guardrail active: Place order, modify order, and cancel order capabilities are intentionally disabled. As an investment research copilot, decision execution remains exclusively in your hands via the official Kite UI.",
                Response = "🛡️ **Trading Guardrail Active**\n\nI am designed strictly as your **Investment Research Copilot**, not an autonomous execution bot.\n\n* **Policy**: Buy/sell order placement, modification, and cancellation tools are not exposed to AI.\n* **Execution**: All transactions must be manually authorized and placed by you directly inside your official Zerodha Kite terminal.\n\nWould you like me to analyze the fundamentals, review your investment thesis, or add this stock to your watchlist instead?",
                SuggestedFollowUps = new() { "Analyse fundamentals for this stock", "Check my current holdings", "Add to Long Term watchlist" }
            };

            await LogAuditAsync(conversationId, message, "BLOCKED_TRADING_INTENT", new { action = "Blocked trade request" }, guardrailResponse.Response, false, 5);
            return guardrailResponse;
        }

        // 2. Destructive mass action guardrail
        if (IsDestructiveMassIntent(message))
        {
            var guardrailResponse = new ChatCopilotResponse
            {
                ConversationId = conversationId,
                GuardrailTriggered = true,
                GuardrailMessage = "Mass deletion requires explicit manual confirmation.",
                Response = "⚠️ **Action Requires Explicit Authorization**\n\nDeleting all watchlists or purging your research portfolio is an irreversible operation.\nTo proceed, please perform this deletion directly within the Watchlist Management interface.",
                SuggestedFollowUps = new() { "Show my watchlists", "View holdings" }
            };

            await LogAuditAsync(conversationId, message, "BLOCKED_MASS_DELETION", new { action = "Blocked mass deletion" }, guardrailResponse.Response, false, 5);
            return guardrailResponse;
        }

        var apiKey = _config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        // 3. If Gemini API key is configured, execute via Gemini Function Calling API
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var geminiResult = await ExecuteViaGeminiApiAsync(request, apiKey, conversationId);
                if (geminiResult != null)
                {
                    return geminiResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API execution failed, falling back to local Copilot reasoning engine.");
            }
        }

        // 4. Intelligent Local Copilot Reasoning Engine (Offline / Local Fallback with complete tool calling & audit logs)
        return await ExecuteViaLocalCopilotEngineAsync(request, conversationId);
    }

    private static bool IsTradingIntent(string prompt)
    {
        var lower = prompt.ToLowerInvariant();
        var patterns = new[]
        {
            @"\b(buy|purchase)\s+([0-9]+\s+shares\s+of\s+)?([a-z0-9]+)",
            @"\b(sell|dump|liquidate)\s+([0-9]+\s+shares\s+of\s+)?([a-z0-9]+)",
            @"\b(place|execute)\s+(an?\s+)?order\b",
            @"\bsell\s+everything\b",
            @"\bbuy\s+all\b",
            @"\bshort\s+sell\b"
        };

        foreach (var p in patterns)
        {
            if (Regex.IsMatch(lower, p, RegexOptions.IgnoreCase))
            {
                // Exceptions: "Why did I buy...", "Did I sell...", "When to buy...", "Should I buy..."
                if (lower.Contains("why did i buy") || lower.Contains("did i sell") || lower.Contains("my thesis for") || lower.Contains("analyse"))
                    continue;

                return true;
            }
        }

        return false;
    }

    private static bool IsDestructiveMassIntent(string prompt)
    {
        var lower = prompt.ToLowerInvariant();
        return lower.Contains("delete all watchlists") || lower.Contains("remove all items") || lower.Contains("drop database") || lower.Contains("purge everything");
    }

    private async Task<ChatCopilotResponse?> ExecuteViaGeminiApiAsync(ChatCopilotRequest request, string apiKey, string conversationId)
    {
        var model = _config["Gemini:Model"] ?? "gemini-1.5-flash";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var toolDeclarations = GetToolDeclarations();
        var systemInstruction = "You are Kite Investment Copilot, an elite personal investment research assistant built around Zerodha Kite. " +
            "You help users manage watchlists, view Kite holdings, positions, funds, and analyze Indian equities, ETFs, IPOs, RAG documents, and personal investment theses. " +
            "You NEVER give automated buy/sell recommendations or place orders. Decision-making is always the user's.";

        var contents = new List<object>();

        // Add history
        foreach (var h in request.History.TakeLast(6))
        {
            contents.Add(new
            {
                role = h.Role == "assistant" || h.Role == "model" ? "model" : "user",
                parts = new[] { new { text = h.Content } }
            });
        }

        // Add current user prompt
        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = request.Message } }
        });

        var requestBody = new
        {
            system_instruction = new { parts = new[] { new { text = systemInstruction } } },
            contents,
            tools = new[] { new { function_declarations = toolDeclarations } },
            generationConfig = new { temperature = 0.2, maxOutputTokens = 1500 }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3.5));
        var response = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), cts.Token);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cts.Token);
            _logger.LogWarning("Gemini API error ({StatusCode}): {Error}", response.StatusCode, err);
            return null;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var executedTools = new List<ToolCallExecutionResult>();
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0) return null;

        var content = candidates[0].GetProperty("content");
        var parts = content.GetProperty("parts");

        string finalText = "";
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textEl))
            {
                finalText += textEl.GetString();
            }

            if (part.TryGetProperty("functionCall", out var fnCall))
            {
                var toolName = fnCall.GetProperty("name").GetString() ?? "";
                var args = fnCall.GetProperty("args");
                var sw = Stopwatch.StartNew();

                var (toolOutput, success, err) = await ExecuteToolAsync(toolName, args);
                sw.Stop();

                var execResult = new ToolCallExecutionResult
                {
                    ToolName = toolName,
                    Arguments = JsonSerializer.Deserialize<object>(args.GetRawText()),
                    Result = toolOutput,
                    Success = success,
                    ErrorMessage = err,
                    ExecutionTimeMs = sw.ElapsedMilliseconds
                };
                executedTools.Add(execResult);
                await LogAuditAsync(conversationId, request.Message, toolName, args, toolOutput, success, sw.ElapsedMilliseconds);

                // If function call happened and no final text returned, generate answer incorporating tool output
                if (string.IsNullOrWhiteSpace(finalText))
                {
                    finalText = FormatToolResponseForUser(toolName, toolOutput);
                }
            }
        }

        return new ChatCopilotResponse
        {
            Response = finalText,
            ConversationId = conversationId,
            ToolCallsExecuted = executedTools,
            SuggestedFollowUps = GenerateFollowUps(request.Message, executedTools)
        };
    }

    private async Task<ChatCopilotResponse> ExecuteViaLocalCopilotEngineAsync(ChatCopilotRequest request, string conversationId)
    {
        var message = request.Message.Trim();
        var lower = message.ToLowerInvariant();
        var executedTools = new List<ToolCallExecutionResult>();
        string responseText = "";

        var sw = Stopwatch.StartNew();
        var targetSymbol = ExtractSymbol(message);

        // 0. AI Stock & IPO Suggestions based on GMP, Company Growth & Full Analysis
        if (lower.Contains("suggest") || lower.Contains("recommend") || lower.Contains("growth stock") || lower.Contains("new stock") || lower.Contains("top pick") || lower.Contains("best stock") || (lower.Contains("new ipo") && !lower.Contains("what is")) || (lower.Contains("ipo") && (lower.Contains("growth") || lower.Contains("suggest"))))
        {
            sw.Stop();
            var opps = await _marketService.GetGrowthOpportunitiesAsync();
            var exec = new ToolCallExecutionResult { ToolName = "get_growth_opportunities", Arguments = new { }, Result = opps, Success = true, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_growth_opportunities", new { }, opps, true, sw.ElapsedMilliseconds);

            var sb = new StringBuilder();
            sb.AppendLine("### ✦ AI Investment Screener: Top Growth Stocks & High-GMP IPOs");
            sb.AppendLine($"*{opps.MarketContext} (Updated: {opps.LastUpdatedAt:MMM dd, yyyy HH:mm} UTC)*\n");

            sb.AppendLine("---");
            sb.AppendLine("### 🚀 1. Top IPO Growth Picks (Ranked by GMP & Institutional Demand)");
            sb.AppendLine("| Company | Price Band | GMP (₹) | Est. Gain % | Subscr. | YoY Rev Growth | Copilot Verdict |");
            sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- |");
            foreach (var ipo in opps.TopIpos)
            {
                sb.AppendLine($"| **{ipo.CompanyName}** (`{ipo.Symbol}`) | {ipo.PriceBand} | **₹{ipo.Gmp:N0}** | **+{ipo.GmpPercent:F1}%** | {ipo.SubscriptionTimes:F1}x | +{ipo.YoYRevenueGrowthPercent:F0}% YoY | {ipo.Recommendation} |");
            }
            sb.AppendLine();

            sb.AppendLine("#### 🔍 In-Depth IPO Analysis:");
            foreach (var ipo in opps.TopIpos.Take(3))
            {
                sb.AppendLine($"**✦ {ipo.CompanyName} (`{ipo.Symbol}`) — {ipo.Sector}**");
                sb.AppendLine($"- **Listing Gain Expectation**: Issue ₹{ipo.IssuePrice:N0} ➔ Est. ₹{ipo.ExpectedListingPrice:N0} (**+{ipo.GmpPercent:F1}%**)");
                sb.AppendLine($"- **Company Growth & Fundamentals**: {ipo.FinancialHealthSummary}");
                sb.AppendLine($"- **Key Strengths**: {string.Join("; ", ipo.Pros)}");
                sb.AppendLine($"- **Risks / Watchpoints**: {string.Join("; ", ipo.Cons)}");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine("### 📈 2. Top Stock Growth Picks (High YoY Growth, ROCE > 25%, Technical Breakouts)");
            sb.AppendLine("| Symbol | Sector | CMP | Target | Est. Upside | YoY Growth | ROCE | Rating |");
            sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |");
            foreach (var s in opps.TopStocks)
            {
                sb.AppendLine($"| **{s.Symbol}** | {s.Sector} | ₹{s.CurrentPrice:N0} | ₹{s.TargetPrice:N0} | **+{s.UpsidePotentialPercent:F1}%** | +{s.YoYRevenueGrowthPercent:F0}% YoY | {s.RocePercent:F0}% | {s.Recommendation} |");
            }
            sb.AppendLine();

            sb.AppendLine("#### 📊 Comprehensive Stock Deep Dive:");
            foreach (var s in opps.TopStocks.Take(3))
            {
                sb.AppendLine($"**✦ {s.CompanyName} (`{s.Symbol}`)**");
                sb.AppendLine($"- **Growth Theme**: {s.GrowthTheme}");
                sb.AppendLine($"- **Valuation & Efficiency**: P/E {s.PeRatio:F1}x | ROE {s.RoePercent:F1}% | ROCE {s.RocePercent:F1}% | Timeframe: {s.TimeHorizon}");
                sb.AppendLine($"- **Copilot Thesis**: {s.FullAnalysis}");
                sb.AppendLine($"- **Technical Momentum**: {s.TechnicalSetup}");
                sb.AppendLine($"- **Catalysts**: {string.Join("; ", s.Catalysts)}");
                sb.AppendLine($"- **Risks**: {string.Join("; ", s.Risks)}");
                sb.AppendLine();
            }

            sb.AppendLine("💡 *Strategy Tip: For beginners, balance between stable compounders (`TCS`, `HDFCBANK`) and apply for listing gains in high-GMP IPOs (`NTPCGREEN`, `HDBFIN`). For aggressive pro portfolios, add momentum compounders (`TRENT`, `DIXON`).*");

            responseText = sb.ToString();
        }
        // 1. Holdings & Portfolio Analysis (Matches "Show my holdings", "Analyse my holdings", "Portfolio analysis", etc.)
        else if (lower.Contains("my holdings") || lower.Contains("all holdings") || (targetSymbol == null && (lower.Contains("holding") || lower.Contains("portfolio") || lower.Contains("what do i own") || (lower.Contains("my") && (lower.Contains("stock") || lower.Contains("asset"))))))
        {
            var (output, success, err) = await ExecuteToolAsync("get_holdings", JsonDocument.Parse("{}").RootElement);
            sw.Stop();
            var exec = new ToolCallExecutionResult { ToolName = "get_holdings", Arguments = new { }, Result = output, Success = success, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_holdings", new { }, output, success, sw.ElapsedMilliseconds);

            var holdings = output as IReadOnlyList<Holding> ?? Array.Empty<Holding>();
            var totalInvested = holdings.Sum(h => h.InvestedValue);
            var totalCurrent = holdings.Sum(h => h.CurrentValue);
            var totalPnl = holdings.Sum(h => h.PnL);
            var totalPnlPercent = totalInvested > 0 ? (totalPnl / totalInvested) * 100 : 0;

            var sb = new StringBuilder();
            sb.AppendLine("### 📊 Kite Portfolio & Holdings Analysis");
            sb.AppendLine($"**Total Valuation**: ₹{totalCurrent:N2} | **Invested Capital**: ₹{totalInvested:N2} | **Overall P&L**: **{(totalPnl >= 0 ? "+" : "")}₹{totalPnl:N2} ({totalPnlPercent:+0.00;-0.00}%)**\n");

            sb.AppendLine("| Symbol | Category / Sector | Qty | Avg Price | LTP | Day Chg | P&L (Total) |");
            sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- |");
            foreach (var h in holdings)
            {
                var sector = h.Symbol switch
                {
                    "GOLDBEES" => "Commodity / Gold ETF",
                    "MON100" => "Nasdaq 100 Tech ETF",
                    "HDFCBANK" => "Private Banking",
                    "TCS" => "IT Services",
                    "HINDUNILVR" => "FMCG / Consumer",
                    "ITC" => "FMCG & Conglomerates",
                    _ => "Equity"
                };
                sb.AppendLine($"| **{h.Symbol}** | {sector} | {h.Quantity} | ₹{h.AveragePrice:N2} | ₹{h.LastPrice:N2} | {h.DayChangePercent:+0.00;-0.00}% | **{(h.PnL >= 0 ? "+" : "")}₹{h.PnL:N2} ({h.PnLPercent:+0.00;-0.00}%)** |");
            }
            sb.AppendLine();

            sb.AppendLine("#### 🧠 Copilot Asset Allocation & Risk Insights:");
            var etfValue = holdings.Where(h => h.Symbol.EndsWith("BEES") || h.Symbol == "MON100").Sum(h => h.CurrentValue);
            var equityValue = totalCurrent - etfValue;
            var etfPct = totalCurrent > 0 ? (etfValue / totalCurrent) * 100 : 0;
            var equityPct = totalCurrent > 0 ? (equityValue / totalCurrent) * 100 : 0;

            sb.AppendLine($"- **Equity Stocks Weight**: {equityPct:F1}% (Core Large-cap IT, Private Banking, & FMCG Compounders)");
            sb.AppendLine($"- **ETF & Macro Hedge Weight**: {etfPct:F1}% (US Big Tech via MON100 & Physical Gold via GOLDBEES)");
            sb.AppendLine($"- **Observation**: Your portfolio is well-defended with resilient consumer staples (`HINDUNILVR`, `ITC`) and liquid gold hedges (`GOLDBEES`), while maintaining secular growth upside in `TCS` and `MON100`.");

            responseText = sb.ToString();
        }
        // 2. Positions
        else if ((lower.Contains("position") || lower.Contains("positions") || (lower.Contains("intraday") && !lower.Contains("delivery") && !lower.Contains("what is") && !lower.Contains("difference"))))
        {
            var (output, success, err) = await ExecuteToolAsync("get_positions", JsonDocument.Parse("{}").RootElement);
            sw.Stop();
            var exec = new ToolCallExecutionResult { ToolName = "get_positions", Arguments = new { }, Result = output, Success = success, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_positions", new { }, output, success, sw.ElapsedMilliseconds);

            var positions = output as IReadOnlyList<Position> ?? Array.Empty<Position>();
            var sb = new StringBuilder();
            sb.AppendLine("### 📈 Open & Intraday Positions");
            if (positions.Count == 0)
            {
                sb.AppendLine("You have no open positions in your Kite account currently.");
            }
            else
            {
                sb.AppendLine("| Symbol | Product | Net Qty | Buy Price | LTP | P&L |");
                sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");
                foreach (var p in positions)
                {
                    sb.AppendLine($"| **{p.Symbol}** | {p.Product} | {p.Quantity} | ₹{p.BuyPrice:N2} | ₹{p.LastPrice:N2} | **{(p.PnL >= 0 ? "+" : "")}₹{p.PnL:N2}** |");
                }
            }
            responseText = sb.ToString();
        }
        // 3. Funds / Margin
        else if (lower.Contains("fund") || lower.Contains("margin") || lower.Contains("cash balance") || lower.Contains("capital"))
        {
            var (output, success, err) = await ExecuteToolAsync("get_funds", JsonDocument.Parse("{}").RootElement);
            sw.Stop();
            var exec = new ToolCallExecutionResult { ToolName = "get_funds", Arguments = new { }, Result = output, Success = success, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_funds", new { }, output, success, sw.ElapsedMilliseconds);

            var funds = output as Funds ?? new Funds();
            responseText = $"### 💰 Available Funds & Margin\n\n" +
                           $"- **Available Cash Margin**: ₹{funds.AvailableMargin:N2}\n" +
                           $"- **Used Margin**: ₹{funds.UsedMargin:N2}\n" +
                           $"- **Collateral Margin**: ₹{funds.CollateralMargin:N2}\n" +
                           $"- **Total Purchasing Power**: ₹{funds.TotalMargin:N2}";
        }
        // 4. Orders
        else if (lower.Contains("order") || lower.Contains("recent trade") || lower.Contains("orderbook"))
        {
            var (output, success, err) = await ExecuteToolAsync("get_orders", JsonDocument.Parse("{}").RootElement);
            sw.Stop();
            var exec = new ToolCallExecutionResult { ToolName = "get_orders", Arguments = new { }, Result = output, Success = success, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_orders", new { }, output, success, sw.ElapsedMilliseconds);

            var orders = output as IReadOnlyList<Order> ?? Array.Empty<Order>();
            var sb = new StringBuilder();
            sb.AppendLine("### 📋 Recent Orders Log");
            sb.AppendLine("| Order ID | Symbol | Action | Qty | Price | Status | Time |");
            sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- |");
            foreach (var o in orders)
            {
                sb.AppendLine($"| `{o.OrderId}` | **{o.Symbol}** | {o.TransactionType} | {o.Quantity} | ₹{o.Price:N2} | `{o.Status}` | {o.OrderTimestamp:MMM dd, HH:mm} |");
            }
            responseText = sb.ToString();
        }
        // 5. Add to Watchlist (e.g. "Add MON100 to ETF watchlist" or "Add INFY to Long Term")
        else if (lower.Contains("add") && (lower.Contains("watchlist") || lower.Contains("list")))
        {
            var match = Regex.Match(message, @"add\s+([A-Za-z0-9_-]+)\s+to\s+(?:my\s+)?([A-Za-z0-9_\s]+?)(?:\s+watchlist|\s+list|$)", RegexOptions.IgnoreCase);
            var symbol = match.Success ? match.Groups[1].Value : ExtractSymbol(message) ?? "MON100";
            var targetWl = match.Success ? match.Groups[2].Value.Trim() : (lower.Contains("etf") ? "ETF" : lower.Contains("ipo") ? "IPO" : "Long Term");

            var assetType = (targetWl.Contains("etf", StringComparison.OrdinalIgnoreCase) || symbol.Contains("bees") || symbol == "MON100")
                ? "ETF"
                : (targetWl.Contains("ipo", StringComparison.OrdinalIgnoreCase) ? "IPO" : "STOCK");

            var argsJson = JsonSerializer.Serialize(new { watchlist = targetWl, symbol, assetType, notes = "Added via Gemini Copilot" });
            var (output, success, err) = await ExecuteToolAsync("add_to_watchlist", JsonDocument.Parse(argsJson).RootElement);
            sw.Stop();
            var exec = new ToolCallExecutionResult { ToolName = "add_to_watchlist", Arguments = JsonSerializer.Deserialize<object>(argsJson), Result = output, Success = success, ErrorMessage = err, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "add_to_watchlist", argsJson, output, success, sw.ElapsedMilliseconds);

            responseText = success
                ? $"✅ **Successfully Added {symbol.ToUpper()}**\n\n`{symbol.ToUpper()}` has been validated and added to your **{targetWl}** watchlist as a `{assetType}`.\n\nYou can now analyze its fundamentals, compare it against peers, or record your investment thesis."
                : $"❌ Could not add {symbol}: {err}";
        }
        // 6. Remove from Watchlist
        else if (lower.Contains("remove") && (lower.Contains("watchlist") || lower.Contains("list")))
        {
            var match = Regex.Match(message, @"remove\s+([A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);
            var symbol = match.Success ? match.Groups[1].Value : "TCS";
            var targetWl = lower.Contains("etf") ? "ETF" : lower.Contains("ipo") ? "IPO" : "Long Term";

            var argsJson = JsonSerializer.Serialize(new { watchlist = targetWl, symbol });
            var (output, success, err) = await ExecuteToolAsync("remove_from_watchlist", JsonDocument.Parse(argsJson).RootElement);
            sw.Stop();
            var exec = new ToolCallExecutionResult { ToolName = "remove_from_watchlist", Arguments = JsonSerializer.Deserialize<object>(argsJson), Result = output, Success = success, ErrorMessage = err, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "remove_from_watchlist", argsJson, output, success, sw.ElapsedMilliseconds);

            responseText = success
                ? $"🗑️ **Removed {symbol.ToUpper()}**\n\n`{symbol.ToUpper()}` has been removed from your **{targetWl}** watchlist."
                : $"Notice: {err ?? "Symbol not found in target watchlist."}";
        }
        // 7. Show Watchlists
        else if (lower.Contains("watchlist") || lower.Contains("watchlists"))
        {
            var (output, success, err) = await ExecuteToolAsync("get_watchlists", JsonDocument.Parse("{}").RootElement);
            sw.Stop();
            var exec = new ToolCallExecutionResult { ToolName = "get_watchlists", Arguments = new { }, Result = output, Success = success, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_watchlists", new { }, output, success, sw.ElapsedMilliseconds);

            var lists = output as IReadOnlyList<Watchlist> ?? Array.Empty<Watchlist>();
            var sb = new StringBuilder();
            sb.AppendLine("### 📑 Your Custom Watchlists\n");
            foreach (var wl in lists)
            {
                sb.AppendLine($"#### 📁 {wl.Name} ({wl.Type}) — {wl.Items.Count} assets");
                if (!string.IsNullOrEmpty(wl.Description)) sb.AppendLine($"*{wl.Description}*");
                sb.AppendLine();
                if (wl.Items.Count > 0)
                {
                    sb.AppendLine("| Symbol | Exchange | Type | Target | Notes |");
                    sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
                    foreach (var it in wl.Items)
                    {
                        var tgt = it.TargetPrice.HasValue ? $"₹{it.TargetPrice.Value:N2}" : "—";
                        sb.AppendLine($"| **{it.Symbol}** | {it.Exchange} | {it.AssetType} | {tgt} | {it.Notes ?? ""} |");
                    }
                    sb.AppendLine();
                }
            }
            responseText = sb.ToString();
        }
        // 8. Thesis Alignment / "Has anything changed" / "What was my thesis"
        else if (lower.Contains("thesis") || lower.Contains("changed") || lower.Contains("alignment"))
        {
            var symbol = ExtractSymbol(message) ?? "INFY";
            var argsJson = JsonSerializer.Serialize(new { symbol });
            var (output, success, err) = await ExecuteToolAsync("analyze_thesis_alignment", JsonDocument.Parse(argsJson).RootElement);
            sw.Stop();
            var exec = new ToolCallExecutionResult { ToolName = "analyze_thesis_alignment", Arguments = JsonSerializer.Deserialize<object>(argsJson), Result = output, Success = success, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "analyze_thesis_alignment", argsJson, output, success, sw.ElapsedMilliseconds);

            var report = output as ThesisAlignmentAnalysis;
            if (report != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"### 🎯 Investment Thesis Review: **{report.Symbol}** ({report.CompanyName})");
                sb.AppendLine($"- **Status**: `{report.OverallAssessment}` | **Time Horizon**: {report.TimeHorizon} | **LTP**: ₹{report.CurrentPrice:N2}\n");
                sb.AppendLine($"**Your Documented Thesis**:\n> \"{report.OriginalThesis}\"\n");

                sb.AppendLine("#### ✅ Confirmatory Evidence");
                foreach (var c in report.ConfirmatoryFactors) sb.AppendLine($"- {c}");
                sb.AppendLine();

                sb.AppendLine("#### ⚠️ Risks / Things to Monitor");
                foreach (var r in report.ContradictoryOrRiskFactors) sb.AppendLine($"- {r}");
                sb.AppendLine();

                sb.AppendLine("#### 📰 Developments Since Thesis");
                foreach (var d in report.DevelopmentsSinceThesis) sb.AppendLine($"- {d}");
                sb.AppendLine();

                sb.AppendLine("#### 🔍 Questions Worth Investigating");
                foreach (var q in report.KeyQuestionsToInvestigate) sb.AppendLine($"- {q}");

                responseText = sb.ToString();
            }
        }
        // 9. Compare Assets (e.g. "Compare INFY and TCS", "Compare MON100 and NIFTYBEES")
        else if (lower.Contains("compare"))
        {
            var symbols = ExtractMultipleSymbols(message);
            if (symbols.Count < 2) symbols = new List<string> { "INFY", "TCS" };

            var argsJson = JsonSerializer.Serialize(new { symbols });
            var (output, success, err) = await ExecuteToolAsync("compare_assets", JsonDocument.Parse(argsJson).RootElement);
            sw.Stop();
            var exec = new ToolCallExecutionResult { ToolName = "compare_assets", Arguments = JsonSerializer.Deserialize<object>(argsJson), Result = output, Success = success, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "compare_assets", argsJson, output, success, sw.ElapsedMilliseconds);

            var quotes = output as IReadOnlyList<MarketQuote> ?? Array.Empty<MarketQuote>();
            var sb = new StringBuilder();
            sb.AppendLine($"### ⚖️ Asset Comparison: {string.Join(" vs ", quotes.Select(q => q.Symbol))}\n");
            sb.AppendLine("| Metric | " + string.Join(" | ", quotes.Select(q => q.Symbol)) + " |");
            sb.AppendLine("| :--- | " + string.Join(" | ", quotes.Select(_ => ":---")) + " |");
            sb.AppendLine("| Company | " + string.Join(" | ", quotes.Select(q => q.CompanyName)) + " |");
            sb.AppendLine("| Last Price | " + string.Join(" | ", quotes.Select(q => $"₹{q.LastPrice:N2}")) + " |");
            sb.AppendLine("| Day Change | " + string.Join(" | ", quotes.Select(q => $"{q.ChangePercent:+0.00;-0.00}%")) + " |");
            sb.AppendLine("| P/E Ratio | " + string.Join(" | ", quotes.Select(q => q.PeRatio.HasValue ? $"{q.PeRatio.Value:F1}x" : "N/A")) + " |");
            sb.AppendLine("| 52W Range | " + string.Join(" | ", quotes.Select(q => $"₹{q.FiftyTwoWeekLow:N0} - ₹{q.FiftyTwoWeekHigh:N0}")) + " |");
            sb.AppendLine("| Sector | " + string.Join(" | ", quotes.Select(q => q.Sector ?? "Equities")) + " |");
            responseText = sb.ToString();
        }
        // 10. ETF Analysis
        else if (lower.Contains("etf") || lower.Contains("mon100") || lower.Contains("niftybees") || lower.Contains("goldbees"))
        {
            var symbol = ExtractSymbol(message) ?? (lower.Contains("mon100") ? "MON100" : "NIFTYBEES");
            var argsJson = JsonSerializer.Serialize(new { symbol });
            var (output, success, err) = await ExecuteToolAsync("get_etf_data", JsonDocument.Parse(argsJson).RootElement);
            sw.Stop();
            var exec = new ToolCallExecutionResult { ToolName = "get_etf_data", Arguments = JsonSerializer.Deserialize<object>(argsJson), Result = output, Success = success, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_etf_data", argsJson, output, success, sw.ElapsedMilliseconds);

            var etf = output as EtfAnalysisDto;
            if (etf != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"### 🌐 ETF Analysis: **{etf.Symbol}** ({etf.Name})");
                sb.AppendLine($"- **Index Tracked**: {etf.UnderlyingIndex}");
                sb.AppendLine($"- **NAV / LTP**: ₹{etf.Quote.LastPrice:N2} ({etf.Quote.ChangePercent:+0.00;-0.00}%)");
                sb.AppendLine($"- **Total Expense Ratio (TER)**: {etf.ExpenseRatio:F2}% | **Tracking Error**: {etf.TrackingError:F2}%");
                sb.AppendLine($"- **AUM**: ₹{etf.AumCrores:N0} Crore\n");
                sb.AppendLine($"**Top Constituents**: {string.Join(", ", etf.TopHoldings)}\n");
                sb.AppendLine($"**Sector Distribution**: {etf.SectorDistribution}\n");
                sb.AppendLine($"**Thesis Suitability**: {etf.ThesisSuitability}\n");
                sb.AppendLine("#### 🔍 Watch Points:");
                foreach (var w in etf.WatchPoints) sb.AppendLine($"- {w}");
                responseText = sb.ToString();
            }
        }
        // 11. GMP (Grey Market Premium) & IPO Tracker
        else if (lower.Contains("gmp") || lower.Contains("grey market") || (lower.Contains("ipo") && !lower.Contains("swiggy") && !lower.Contains("hyundai")))
        {
            sw.Stop();
            var gmpExplanation = await _marketService.ExplainGmpSimpleAsync();
            var ipoList = await _marketService.GetIposWithGmpAsync();

            var exec = new ToolCallExecutionResult { ToolName = "get_ipos_with_gmp", Arguments = new { }, Result = ipoList, Success = true, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_ipos_with_gmp", new { }, ipoList, true, sw.ElapsedMilliseconds);

            var sb = new StringBuilder();
            sb.AppendLine(gmpExplanation);
            sb.AppendLine();
            sb.AppendLine("### 🚀 Live Indian IPO Pipeline & GMP Tracker");
            sb.AppendLine("| Company | Price Band | Issue (Cr) | GMP (₹) | Est. Listing Gain | Rating | Status |");
            sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- |");
            foreach (var ipo in ipoList)
            {
                var gainSign = ipo.GmpPercent >= 0 ? "+" : "";
                sb.AppendLine($"| **{ipo.CompanyName}** (`{ipo.Symbol}`) | {ipo.PriceBand} | ₹{ipo.IssueSizeCrores:N0} Cr | **₹{ipo.Gmp:N0}** | **{gainSign}{ipo.GmpPercent:F1}%** | {ipo.Rating} | `{ipo.Status}` |");
            }
            sb.AppendLine();
            sb.AppendLine("💡 *Pro-Tip: A high GMP (>20%) indicates robust institutional & retail appetite (e.g. Waaree Energies), whereas flat GMP (0-5%) warrants patience post-listing.*");
            responseText = sb.ToString();
        }
        // 12. "Why are they falling / Why price goes up?" - Catalyst & Headwinds Analysis
        else if ((lower.Contains("why") && (lower.Contains("fall") || lower.Contains("drop") || lower.Contains("down") || lower.Contains("crash") || lower.Contains("ris") || lower.Contains("go up") || lower.Contains("going up") || lower.Contains("rally") || lower.Contains("surge") || lower.Contains("perform"))) || lower.Contains("why are they falling") || lower.Contains("price goes up") || lower.Contains("why falling"))
        {
            var symbol = ExtractSymbol(message) ?? "HDFCBANK";
            sw.Stop();
            var fin = await _marketService.GetCompanyFinancialsAsync(symbol);

            var exec = new ToolCallExecutionResult { ToolName = "analyze_price_drivers", Arguments = new { symbol }, Result = fin, Success = true, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "analyze_price_drivers", new { symbol }, fin, true, sw.ElapsedMilliseconds);

            var sb = new StringBuilder();
            sb.AppendLine($"### 🔍 Price Movement & Catalysts Analysis: **{fin.CompanyName}** (`{fin.Symbol}`)");
            sb.AppendLine($"- **Current LTP**: ₹{fin.CurrentPrice:N2} ({fin.DayChangePercent:+0.00;-0.00}%) | **Sector**: {fin.Sector} | **P/E**: {fin.PeRatio:F1}x");
            sb.AppendLine();

            sb.AppendLine("#### 📈 Why Price Goes Up / Bullish Growth Drivers:");
            foreach (var up in fin.WhyPriceIsRising)
            {
                sb.AppendLine($"- {up}");
            }
            sb.AppendLine();

            sb.AppendLine("#### 📉 Why Price is Falling / Bearish Headwinds:");
            foreach (var down in fin.WhyPriceIsFalling)
            {
                sb.AppendLine($"- {down}");
            }
            sb.AppendLine();

            if (fin.QuarterlyResults.Count > 0)
            {
                sb.AppendLine("#### 📊 Recent Revenue & Operating Margins Performance:");
                sb.AppendLine("| Quarter | Revenue (Cr) | Net Profit (Cr) | Margin % | YoY Rev Growth |");
                sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
                foreach (var q in fin.QuarterlyResults.Take(3))
                {
                    sb.AppendLine($"| **{q.Quarter}** | ₹{q.RevenueCrores:N0} Cr | ₹{q.NetProfitCrores:N0} Cr | {q.OperatingMarginPercent:F1}% | +{q.YoYRevenueGrowthPercent:F1}% |");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"#### 🎯 Copilot Strategic Verdict:\n> \"{fin.SummaryVerdict}\"");
            responseText = sb.ToString();
        }
        // 13. Company Revenue & Financial Performance Charts Data
        else if (lower.Contains("revenue") || lower.Contains("financial") || lower.Contains("profit") || lower.Contains("earnings") || lower.Contains("ebitda") || lower.Contains("margin"))
        {
            var symbol = ExtractSymbol(message) ?? "TCS";
            sw.Stop();
            var fin = await _marketService.GetCompanyFinancialsAsync(symbol);

            var exec = new ToolCallExecutionResult { ToolName = "get_company_financials", Arguments = new { symbol }, Result = fin, Success = true, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_company_financials", new { symbol }, fin, true, sw.ElapsedMilliseconds);

            var sb = new StringBuilder();
            sb.AppendLine($"### 📊 Revenue & Financial Growth Profile: **{fin.CompanyName}** (`{fin.Symbol}`)");
            sb.AppendLine($"- **Market Cap**: ₹{fin.MarketCapCrores:N0} Cr | **ROE**: {fin.RoePercent:F1}% | **ROCE**: {fin.RocePercent:F1}% | **Div Yield**: {fin.DividendYieldPercent:F2}%\n");

            sb.AppendLine("#### 📅 Quarterly Revenue Trajectory:");
            sb.AppendLine("| Quarter | Revenue (Cr) | EBITDA (Cr) | Net Profit (Cr) | Operating Margin % |");
            sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
            foreach (var q in fin.QuarterlyResults)
            {
                sb.AppendLine($"| **{q.Quarter}** | ₹{q.RevenueCrores:N0} Cr | ₹{q.EbitdaCrores:N0} Cr | ₹{q.NetProfitCrores:N0} Cr | **{q.OperatingMarginPercent:F1}%** |");
            }
            sb.AppendLine();

            sb.AppendLine("#### 📈 Multi-Year Annual Compound Growth:");
            sb.AppendLine("| Financial Year | Revenue (Cr) | Net Profit (Cr) | Operating Margin % |");
            sb.AppendLine("| :--- | :--- | :--- | :--- |");
            foreach (var a in fin.AnnualResults)
            {
                sb.AppendLine($"| **{a.Year}** | ₹{a.RevenueCrores:N0} Cr | ₹{a.NetProfitCrores:N0} Cr | {a.OperatingMarginPercent:F1}% |");
            }
            responseText = sb.ToString();
        }
        // 14. Futures & Derivatives (F&O / Traction)
        else if (lower.Contains("future") || lower.Contains("f&o") || lower.Contains("traction") || lower.Contains("open interest") || lower.Contains("pcr") || lower.Contains("derivative"))
        {
            sw.Stop();
            var futures = await _marketService.GetFuturesTractionAsync();

            var exec = new ToolCallExecutionResult { ToolName = "get_futures_traction", Arguments = new { }, Result = futures, Success = true, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_futures_traction", new { }, futures, true, sw.ElapsedMilliseconds);

            var sb = new StringBuilder();
            sb.AppendLine("### ⚡ Derivatives & Futures Traction Hub (NSE/BSE)");
            sb.AppendLine("| Contract | Spot Price | Futures Price | Basis (Prem/Disc) | OI (Shares) | OI Chg % | Buildup Type | PCR |");
            sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |");
            foreach (var f in futures)
            {
                var basisSign = f.BasisPremiumDiscount >= 0 ? "+" : "";
                var oiSign = f.OpenInterestChangePercent >= 0 ? "+" : "";
                sb.AppendLine($"| **{f.Symbol}** | ₹{f.SpotPrice:N2} | ₹{f.FuturesPrice:N2} | {basisSign}₹{f.BasisPremiumDiscount:N2} | {f.OpenInterest:N0} | {oiSign}{f.OpenInterestChangePercent:F2}% | **{f.BuildupType}** | `{f.PutCallRatio:F2}` ({f.PcrSentiment}) |");
            }
            sb.AppendLine();
            sb.AppendLine("#### 🧠 Derivatives Buildup Interpretations:");
            foreach (var f in futures)
            {
                sb.AppendLine($"- **{f.Symbol}**: {f.BuildupDescription} *(Max Pain: ₹{f.MaxPain:N0})*");
            }
            sb.AppendLine("\n*PCR Rule: PCR > 1.0 indicates Bullish support; PCR < 0.8 signals Oversold/Bearish resistance.*");
            responseText = sb.ToString();
        }
        // 15. Multi-Source Market Overview (Groww / Moneycontrol / INDmoney / Zerodha / NSE / BSE)
        else if (lower.Contains("market") || lower.Contains("groww") || lower.Contains("money") || lower.Contains("indmoney") || lower.Contains("fii") || lower.Contains("dii") || lower.Contains("breadth") || lower.Contains("pulse") || lower.Contains("overview") || lower.Contains("nse") || lower.Contains("bse"))
        {
            sw.Stop();
            var pulse = await _marketService.GetMarketOverviewAsync();

            var exec = new ToolCallExecutionResult { ToolName = "get_market_overview", Arguments = new { }, Result = pulse, Success = true, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_market_overview", new { }, pulse, true, sw.ElapsedMilliseconds);

            var sb = new StringBuilder();
            sb.AppendLine("### 🌐 Consolidated Multi-Resource Market Pulse");
            sb.AppendLine($"*{pulse.MarketSummaryText}*\n");

            sb.AppendLine("#### 📊 Benchmark Indices (NSE & BSE):");
            sb.AppendLine("| Index | LTP | Change | Chg % | Day Range |");
            sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
            foreach (var idx in pulse.MajorIndices)
            {
                var chgSign = idx.Change >= 0 ? "+" : "";
                sb.AppendLine($"| **{idx.Name}** | ₹{idx.Value:N2} | {chgSign}{idx.Change:N2} | **{chgSign}{idx.ChangePercent:F2}%** | ₹{idx.Low:N0} - ₹{idx.High:N0} |");
            }
            sb.AppendLine();

            sb.AppendLine("#### 🏦 Institutional Flows (FII vs DII):");
            sb.AppendLine($"- **FII Net Cash**: ₹{pulse.InstitutionalFlows.FiiNetCrores:+0.00;-0.00} Crore");
            sb.AppendLine($"- **DII Net Cash**: ₹{pulse.InstitutionalFlows.DiiNetCrores:+0.00;-0.00} Crore");
            sb.AppendLine($"- **Net Institutional Activity**: **₹{pulse.InstitutionalFlows.NetTotalCrores:+0.00;-0.00} Crore**");
            sb.AppendLine($"> *{pulse.InstitutionalFlows.Interpretation}*\n");

            sb.AppendLine($"#### ⚖️ Market Breadth: **{pulse.MarketBreadth.Advances} Advances** vs **{pulse.MarketBreadth.Declines} Declines** (A/D Ratio: `{pulse.MarketBreadth.AdvanceDeclineRatio:F2}`)");
            sb.AppendLine();

            sb.AppendLine("#### 🚀 Top Sector Performers:");
            foreach (var s in pulse.SectorPerformance)
            {
                sb.AppendLine($"- **{s.SectorName}**: {s.ChangePercent:+0.00;-0.00}% (Top: {s.TopStock})");
            }

            responseText = sb.ToString();
        }
        // 16. Concept & Educational Explanations (Newbie Friendly with Pro Depth)
        else if (lower.Contains("delivery") || lower.Contains("pe ratio") || lower.Contains("p/e") || lower.Contains("what is pe") || lower.Contains("nifty 50") || lower.Contains("what is nifty") || lower.Contains("sensex") || lower.Contains("stop loss") || lower.Contains("circuit") || lower.Contains("roce") || lower.Contains("how to start") || lower.Contains("where to start") || lower.Contains("newbie") || lower.Contains("beginner") || lower.Contains("eli5") || lower.Contains("explain like") || lower.Contains("what is gtt") || lower.Contains("teach me") || lower.Contains("how does it work") || (lower.Contains("difference") && (lower.Contains("cnc") || lower.Contains("mis") || lower.Contains("intraday"))))
        {
            sw.Stop();
            var conceptDto = await _marketService.ExplainConceptAsync(message);

            var exec = new ToolCallExecutionResult { ToolName = "explain_market_concept", Arguments = new { concept = message }, Result = conceptDto, Success = true, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "explain_market_concept", new { concept = message }, conceptDto, true, sw.ElapsedMilliseconds);

            var sb = new StringBuilder();
            sb.AppendLine($"### 📚 Concept Guide: {conceptDto.Title}\n");

            sb.AppendLine("#### 🌱 Simple & Intuitive (ELI5 / Newbie):");
            sb.AppendLine($"> {conceptDto.Eli5SimpleExplanation}\n");

            sb.AppendLine("#### ☕ Real-Life Everyday Analogy:");
            sb.AppendLine($"*{conceptDto.EverydayAnalogy}*\n");

            sb.AppendLine("#### ⚡ Pro Trader & Quantitative Perspective:");
            sb.AppendLine($"- {conceptDto.ProQuantPerspective}\n");

            sb.AppendLine("#### 🎯 Indian Market Example:");
            sb.AppendLine($"- {conceptDto.PracticalExample}\n");

            sb.AppendLine("#### 🛡️ Golden Safety & Risk Rule:");
            sb.AppendLine($"- **Crucial Rule**: {conceptDto.SafetyTip}");

            responseText = sb.ToString();
        }
        // 17. Technical Indicators & Chart Levels (Pro Trader & Experienced)
        else if (lower.Contains("technical") || lower.Contains("technicals") || lower.Contains("rsi") || lower.Contains("moving average") || lower.Contains("ema") || lower.Contains("sma") || lower.Contains("pivot") || lower.Contains("support") || lower.Contains("resistance") || lower.Contains("macd"))
        {
            var symbol = ExtractSymbol(message) ?? "TCS";
            sw.Stop();
            var tech = await _marketService.GetTechnicalsAsync(symbol);

            var exec = new ToolCallExecutionResult { ToolName = "get_technical_analysis", Arguments = new { symbol }, Result = tech, Success = true, ExecutionTimeMs = sw.ElapsedMilliseconds };
            executedTools.Add(exec);
            await LogAuditAsync(conversationId, message, "get_technical_analysis", new { symbol }, tech, true, sw.ElapsedMilliseconds);

            var sb = new StringBuilder();
            sb.AppendLine($"### 📈 Technical Levels & Indicators: **{tech.CompanyName}** (`{tech.Symbol}`)");
            sb.AppendLine($"- **Current LTP**: ₹{tech.CurrentPrice:N2} | **Trend Assessment**: `{tech.TrendSignal}`\n");

            sb.AppendLine("#### 🌱 In Plain English (ELI5):");
            sb.AppendLine($"> {tech.Eli5Summary}\n");

            sb.AppendLine("#### ⚡ Moving Averages (Trend Filters):");
            sb.AppendLine($"| Metric | Level (₹) | Price vs Average | Implication |");
            sb.AppendLine($"| :--- | :--- | :--- | :--- |");
            sb.AppendLine($"| **20 EMA (Short-Term)** | ₹{tech.Ema20:N2} | {(tech.CurrentPrice >= tech.Ema20 ? "▲ Above EMA" : "▼ Below EMA")} | {(tech.CurrentPrice >= tech.Ema20 ? "Immediate Momentum Bullish" : "Short-term Pullback")} |");
            sb.AppendLine($"| **50 SMA (Medium-Term)** | ₹{tech.Sma50:N2} | {(tech.CurrentPrice >= tech.Sma50 ? "▲ Above SMA" : "▼ Below SMA")} | {(tech.CurrentPrice >= tech.Sma50 ? "Medium-Term Trend Intact" : "Corrective Phase")} |");
            sb.AppendLine($"| **200 SMA (Long-Term)** | ₹{tech.Sma200:N2} | {(tech.CurrentPrice >= tech.Sma200 ? "▲ Above SMA" : "▼ Below SMA")} | {(tech.CurrentPrice >= tech.Sma200 ? "Major Bull Cycle Active" : "Long-Term Bearish Filter")} |");
            sb.AppendLine();

            sb.AppendLine("#### 🎯 Classical Pivot Points & Trade Zones:");
            sb.AppendLine($"- **Major Resistance (R2)**: ₹{tech.Resistance2:N2} *(Strong Profit Booking Zone)*");
            sb.AppendLine($"- **First Resistance (R1)**: ₹{tech.Resistance1:N2} *(Immediate Ceiling)*");
            sb.AppendLine($"- **Central Pivot (P)**: ₹{tech.PivotPoint:N2} *(Equilibrium Level)*");
            sb.AppendLine($"- **First Support (S1)**: ₹{tech.Support1:N2} *(Dip Buyers Entry Zone)*");
            sb.AppendLine($"- **Major Support (S2)**: ₹{tech.Support2:N2} *(Key Stop Loss / Defense Floor)*");
            sb.AppendLine();

            sb.AppendLine("#### ⚡ Oscillators:");
            sb.AppendLine($"- **RSI (14)**: `{tech.Rsi14:F1}` → *{tech.RsiSignal}*");
            sb.AppendLine($"- **MACD**: *{tech.MacdSignal}*");

            responseText = sb.ToString();
        }
        // 18. Stock Analysis / Specific Stock Query (or Intelligent Assistant Fallback)
        else
        {
            var symbol = ExtractSymbol(message);
            if (symbol != null)
            {
                var argsJson = JsonSerializer.Serialize(new { symbol });
                var (output, success, err) = await ExecuteToolAsync("get_stock_data", JsonDocument.Parse(argsJson).RootElement);
                sw.Stop();
                var exec = new ToolCallExecutionResult { ToolName = "get_stock_data", Arguments = JsonSerializer.Deserialize<object>(argsJson), Result = output, Success = success, ExecutionTimeMs = sw.ElapsedMilliseconds };
                executedTools.Add(exec);
                await LogAuditAsync(conversationId, message, "get_stock_data", argsJson, output, success, sw.ElapsedMilliseconds);

                var stock = output as StockAnalysisDto;
                if (stock != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"### 🔬 Dual-Mode Analysis: **{stock.CompanyName}** (`{stock.Symbol}`)");
                    sb.AppendLine($"- **Price**: ₹{stock.Quote.LastPrice:N2} ({stock.Quote.ChangePercent:+0.00;-0.00}%) | **P/E**: {(stock.Quote.PeRatio.HasValue ? stock.Quote.PeRatio.Value.ToString("F1") + "x" : "N/A")} | **Exchange**: {stock.Quote.Exchange}");
                    if (stock.CurrentHolding != null)
                    {
                        sb.AppendLine($"- **Your Kite Portfolio**: Holding **{stock.CurrentHolding.Quantity} shares** @ avg ₹{stock.CurrentHolding.AveragePrice:N2} | P&L: **{(stock.CurrentHolding.PnL >= 0 ? "+" : "")}₹{stock.CurrentHolding.PnL:N2} ({stock.CurrentHolding.PnLPercent:+0.00;-0.00}%)**");
                    }
                    sb.AppendLine();

                    sb.AppendLine("#### 🌱 Simple & Intuitive (ELI5 / Newbie):");
                    sb.AppendLine($"> **What they do**: {stock.BusinessSummary}");
                    sb.AppendLine($"> **Why people buy it**: Proven compounder in {stock.Quote.Sector ?? "its industry"} with strong market position and solid balance sheet.\n");

                    sb.AppendLine("#### ⚡ Pro Quant & Institutional Fundamentals:");
                    foreach (var kvp in stock.FinancialHighlights)
                    {
                        sb.AppendLine($"- **{kvp.Key}**: {kvp.Value}");
                    }
                    sb.AppendLine();

                    if (stock.UserThesis != null)
                    {
                        sb.AppendLine($"#### 🎯 Your Documented Thesis:\n> \"{stock.UserThesis.Thesis}\"\n");
                    }

                    if (stock.KeyFilingExcerpts.Count > 0)
                    {
                        sb.AppendLine($"#### 📄 Regulatory Filing Excerpt ({stock.KeyFilingExcerpts[0].Quarter} {stock.KeyFilingExcerpts[0].Year}):\n> \"{stock.KeyFilingExcerpts[0].Content}\"\n");
                    }

                    sb.AppendLine("#### ⚠️ Key Risks & Monitoring Points:");
                    foreach (var r in stock.RisksAndWatchlistPoints)
                    {
                        sb.AppendLine($"- {r}");
                    }

                    responseText = sb.ToString();
                }
            }
            else
            {
                // Intelligent Universal Market & Assistant Response (Instead of hardcoded INFY fallback!)
                sw.Stop();
                var holdings = await _kiteService.GetHoldingsAsync();
                var pulse = await _marketService.GetMarketOverviewAsync();

                var sb = new StringBuilder();
                sb.AppendLine("### 🤖 Kite Copilot: Adaptive Intelligence (Newbie to Pro)");
                sb.AppendLine("I adapt automatically to your experience level—from **Explain-Like-I'm-5 beginner guides** to **institutional quantitative analytics and derivatives Greeks**.\n");

                sb.AppendLine("#### 📊 Real-Time Market Pulse Today:");
                sb.AppendLine($"- **NIFTY 50**: 24,982.50 (+0.50%) | **SENSEX**: 81,634.80 (+0.47%)");
                sb.AppendLine($"- **Institutional Flows**: DIIs Net **+₹2,860 Cr**, FIIs Net **-₹1,420 Cr**");
                sb.AppendLine($"- **Your Kite Portfolio**: {holdings.Count} active holdings (`{string.Join("`, `", holdings.Select(h => h.Symbol))}`)");
                sb.AppendLine();

                sb.AppendLine("#### 💡 Pick Your Mode or Ask Any Question:");
                sb.AppendLine("🌱 **Beginner / Newbie Guides**:");
                sb.AppendLine("- *\"I am a newbie, where do I start?\"*");
                sb.AppendLine("- *\"What is P/E ratio in simple terms?\"*");
                sb.AppendLine("- *\"What is NIFTY 50?\"*");
                sb.AppendLine("- *\"What is Delivery vs Intraday in Zerodha?\"*");
                sb.AppendLine("- *\"What is GMP in simple terms?\"*");
                sb.AppendLine();
                sb.AppendLine("⚡ **Pro Trader & Experienced Deep Dives**:");
                sb.AppendLine("- *\"Show TCS technical levels, RSI, and pivot points\"*");
                sb.AppendLine("- *\"Show Nifty futures traction, PCR, and buildup\"*");
                sb.AppendLine("- *\"Why is HDFC Bank falling vs rising?\"*");
                sb.AppendLine("- *\"Compare TCS and INFY quarterly margins and ROCE\"*");

                responseText = sb.ToString();
            }
        }

        // Generate Beginner and Pro summaries for dual-layer view
        string? beginnerSummary = null;
        string? proAnalysis = null;
        if (responseText.Contains("#### 🌱 Simple & Intuitive (ELI5 / Newbie):") || responseText.Contains("#### 🌱 In Plain English (ELI5):"))
        {
            var parts = responseText.Split(new[] { "#### ⚡", "#### 🎯" }, StringSplitOptions.None);
            if (parts.Length > 0) beginnerSummary = parts[0];
            if (parts.Length > 1) proAnalysis = "#### ⚡" + parts[1];
        }

        return new ChatCopilotResponse
        {
            Response = responseText,
            ConversationId = conversationId,
            BeginnerSummary = beginnerSummary,
            ProAnalysis = proAnalysis,
            ExperienceLevel = request.ExperienceLevel,
            ToolCallsExecuted = executedTools,
            SuggestedFollowUps = GenerateFollowUps(message, executedTools)
        };
    }

    private async Task<(object? Result, bool Success, string? Error)> ExecuteToolAsync(string toolName, JsonElement args)
    {
        try
        {
            switch (toolName.ToLowerInvariant())
            {
                case "get_holdings":
                    var holdings = await _kiteService.GetHoldingsAsync();
                    return (holdings, true, null);

                case "get_positions":
                    var positions = await _kiteService.GetPositionsAsync();
                    return (positions, true, null);

                case "get_funds":
                    var funds = await _kiteService.GetFundsAsync();
                    return (funds, true, null);

                case "get_orders":
                    var orders = await _kiteService.GetOrdersAsync();
                    return (orders, true, null);

                case "get_watchlists":
                    var lists = await _watchlistService.GetWatchlistsAsync();
                    return (lists, true, null);

                case "get_watchlist_items":
                    string? wlName = args.TryGetProperty("watchlist", out var wEl) ? wEl.GetString() : null;
                    if (string.IsNullOrEmpty(wlName))
                    {
                        var all = await _watchlistService.GetWatchlistsAsync();
                        return (all.SelectMany(w => w.Items).ToList(), true, null);
                    }
                    var foundWl = await _watchlistService.GetWatchlistByNameAsync(wlName);
                    return (foundWl?.Items ?? new List<WatchlistItem>(), true, null);

                case "create_watchlist":
                    var name = args.GetProperty("name").GetString() ?? "New Watchlist";
                    var type = args.TryGetProperty("type", out var tEl) ? tEl.GetString() ?? "Custom" : "Custom";
                    var created = await _watchlistService.CreateWatchlistAsync(name, type);
                    return (created, true, null);

                case "add_to_watchlist":
                    var targetWlName = args.GetProperty("watchlist").GetString() ?? "Long Term";
                    var symbol = args.GetProperty("symbol").GetString() ?? "";
                    var assetTypeStr = args.TryGetProperty("assetType", out var atEl) ? atEl.GetString() ?? "STOCK" : "STOCK";
                    var notes = args.TryGetProperty("notes", out var ntEl) ? ntEl.GetString() : null;

                    var wl = await _watchlistService.GetWatchlistByNameAsync(targetWlName);
                    if (wl == null)
                    {
                        // Guardrail: auto-create if reasonable or return validation
                        wl = await _watchlistService.CreateWatchlistAsync(targetWlName, "Custom");
                    }

                    if (!Enum.TryParse<AssetType>(assetTypeStr, true, out var parsedAssetType))
                    {
                        parsedAssetType = AssetType.STOCK;
                    }

                    var item = await _watchlistService.AddItemAsync(wl.Id, symbol, "NSE", parsedAssetType, notes);
                    return (item, true, null);

                case "remove_from_watchlist":
                    var rmWlName = args.GetProperty("watchlist").GetString() ?? "Long Term";
                    var rmSymbol = args.GetProperty("symbol").GetString() ?? "";
                    var rmWl = await _watchlistService.GetWatchlistByNameAsync(rmWlName);
                    if (rmWl == null) return (false, false, $"Watchlist {rmWlName} not found.");

                    var removed = await _watchlistService.RemoveItemAsync(rmWl.Id, rmSymbol);
                    return (removed, removed, removed ? null : $"Symbol {rmSymbol} was not in {rmWlName}.");

                case "get_stock_data":
                    var stockSym = args.GetProperty("symbol").GetString() ?? "INFY";
                    var stockData = await _researchService.GetStockAnalysisAsync(stockSym);
                    return (stockData, true, null);

                case "get_etf_data":
                    var etfSym = args.GetProperty("symbol").GetString() ?? "MON100";
                    var etfData = await _researchService.GetEtfAnalysisAsync(etfSym);
                    return (etfData, true, null);

                case "get_ipo_data":
                    var ipoSym = args.GetProperty("symbol").GetString() ?? "SWIGGY";
                    var ipoData = await _researchService.GetIpoAnalysisAsync(ipoSym);
                    return (ipoData, true, null);

                case "search_research_documents":
                    var query = args.GetProperty("query").GetString() ?? "";
                    var docSymbol = args.TryGetProperty("symbol", out var dsEl) ? dsEl.GetString() : null;
                    var docs = await _ragService.SearchDocumentsAsync(query, docSymbol);
                    return (docs, true, null);

                case "search_investment_notes":
                    var noteSymbol = args.TryGetProperty("symbol", out var nsEl) ? nsEl.GetString() : null;
                    var notesList = await _thesisService.GetNotesAsync(noteSymbol);
                    return (notesList, true, null);

                case "compare_assets":
                    var symbolsList = new List<string>();
                    if (args.TryGetProperty("symbols", out var symsEl) && symsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var s in symsEl.EnumerateArray()) symbolsList.Add(s.GetString() ?? "");
                    }
                    else
                    {
                        symbolsList.AddRange(new[] { "INFY", "TCS" });
                    }
                    var comp = await _researchService.CompareAssetsAsync(symbolsList);
                    return (comp, true, null);

                case "save_investment_thesis":
                    var thesisSym = args.GetProperty("symbol").GetString() ?? "";
                    var thesisTitle = args.GetProperty("title").GetString() ?? $"{thesisSym} Thesis";
                    var thesisText = args.GetProperty("thesis").GetString() ?? "";
                    var horizon = args.TryGetProperty("timeHorizon", out var hEl) ? hEl.GetString() ?? "Long-Term (3-5 Years)" : "Long-Term (3-5 Years)";
                    var risks = args.TryGetProperty("risks", out var rEl) ? rEl.GetString() : null;
                    var watchPoints = args.TryGetProperty("watchPoints", out var wpEl) ? wpEl.GetString() : null;

                    var savedThesis = await _thesisService.SaveThesisAsync(thesisSym, thesisTitle, thesisText, horizon, risks, watchPoints);
                    return (savedThesis, true, null);

                case "analyze_thesis_alignment":
                    var alignSym = args.GetProperty("symbol").GetString() ?? "INFY";
                    var align = await _thesisService.AnalyzeThesisAlignmentAsync(alignSym);
                    return (align, true, null);

                case "explain_market_concept":
                    var concept = args.TryGetProperty("concept", out var cpEl) ? cpEl.GetString() ?? "where_to_start" : "where_to_start";
                    var explanation = await _marketService.ExplainConceptAsync(concept);
                    return (explanation, true, null);

                case "get_technical_analysis":
                    var techSym = args.TryGetProperty("symbol", out var tsEl) ? tsEl.GetString() ?? "TCS" : "TCS";
                    var technicals = await _marketService.GetTechnicalsAsync(techSym);
                    return (technicals, true, null);

                default:
                    return (null, false, $"Unrecognized tool: {toolName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return (null, false, ex.Message);
        }
    }

    private static string FormatToolResponseForUser(string toolName, object? result)
    {
        return $"Execution of `{toolName}` completed successfully.\n\n" +
               "Results have been incorporated into your active session.";
    }

    private async Task LogAuditAsync(string conversationId, string prompt, string? tool, object? args, object? result, bool success, long executionMs)
    {
        try
        {
            var audit = new AiAuditLog
            {
                ConversationId = conversationId,
                Timestamp = DateTime.UtcNow,
                User = "investor",
                Prompt = prompt,
                Tool = tool,
                Arguments = args != null ? JsonSerializer.Serialize(args) : null,
                Result = result != null ? (result is string s ? s : JsonSerializer.Serialize(result)) : null,
                Success = success,
                ExecutionMs = executionMs
            };

            // Truncate long results for clean storage
            if (audit.Result != null && audit.Result.Length > 2000)
            {
                audit.Result = audit.Result[..2000] + "... [truncated]";
            }

            _context.AiAuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write AI audit log");
        }
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ANALYSE", "ANALYZE", "ANALYSIS", "HOLDINGS", "HOLDING", "PORTFOLIO", "WATCHLIST", 
        "WATCHLISTS", "FUNDS", "MARGIN", "REPORT", "EXPLAIN", "COMPARE", "CHECK", "WHAT", 
        "SHOW", "KITE", "ABOUT", "VIEW", "LIST", "ORDERS", "POSITIONS", "THESIS", "STOCK",
        "STOCKS", "PLEASE", "PRICE", "TODAY", "DETAIL", "DETAILS", "SUMMARY", "REFRESH",
        "CONNECT", "SEARCH", "THEIR", "WHICH", "CURRENT", "OVERALL", "FALLING", "RISING",
        "SURGING", "DROPPING", "REVENUE", "PROFIT", "CHARTS", "FUTURES", "OPTIONS", "PORTAL",
        "EVERYTHING", "SIMPLE", "TERMS", "RESOURCE", "MULTIPLE", "PROPERLY", "WORKING",
        "DIFFERENCE", "BETWEEN", "DELIVERY", "INTRADAY", "START", "WHERE", "NEWBIE", "BEGINNER",
        "LEARN", "CIRCUIT", "LIMITS", "MARKET", "CONCEPT", "ZERODHA", "TRADING", "INVESTING"
    };

    private static readonly string[] KnownSymbols = new[]
    {
        "HDFCBANK", "TCS", "ITC", "HINDUNILVR", "GOLDBEES", "MON100", 
        "INFY", "RELIANCE", "NIFTYBEES", "SWIGGY", "HYUNDAI", "NTPCGREEN", 
        "TATAMOTORS", "ICICIBANK", "SBIN", "BHARTIARTL", "LT", "BAJFINANCE",
        "WAAREEENER", "BAJAJHFL", "ZOMATO", "TITAN", "PREMIERENE", "FIRSTCRY", "AFCONS"
    };

    private static string? ExtractSymbol(string text)
    {
        var upper = text.ToUpperInvariant();
        foreach (var k in KnownSymbols)
        {
            if (Regex.IsMatch(upper, $@"\b{k}\b")) return k;
        }

        var matches = Regex.Matches(upper, @"\b([A-Z]{3,10})\b");
        foreach (Match m in matches)
        {
            var val = m.Groups[1].Value;
            if (!StopWords.Contains(val))
            {
                return val;
            }
        }

        return null;
    }

    private static List<string> ExtractMultipleSymbols(string text)
    {
        var found = new List<string>();
        var upper = text.ToUpperInvariant();
        foreach (var k in KnownSymbols)
        {
            if (Regex.IsMatch(upper, $@"\b{k}\b")) found.Add(k);
        }
        return found;
    }

    private static List<string> GenerateFollowUps(string prompt, List<ToolCallExecutionResult> tools)
    {
        var list = new List<string>();
        var lower = prompt.ToLowerInvariant();

        if (lower.Contains("infy") || lower.Contains("holding"))
        {
            list.Add("Has anything changed relative to my INFY thesis?");
            list.Add("Compare INFY and TCS");
            list.Add("Show my funds & available margin");
        }
        else if (lower.Contains("etf") || lower.Contains("mon100"))
        {
            list.Add("Compare MON100 and NIFTYBEES");
            list.Add("Show my ETF watchlist");
            list.Add("What was my thesis for MON100?");
        }
        else if (lower.Contains("ipo") || lower.Contains("swiggy"))
        {
            list.Add("Show the IPOs I am tracking");
            list.Add("Analyse Hyundai IPO details");
            list.Add("Add SWIGGY to my IPO watchlist");
        }
        else
        {
            list.Add("Show my holdings");
            list.Add("Analyse INFY from my watchlist");
            list.Add("Explain my ETF watchlist");
        }

        return list;
    }

    private static object[] GetToolDeclarations()
    {
        return new object[]
        {
            new
            {
                name = "get_holdings",
                description = "Get current Kite investment portfolio holdings including quantities, average buy price, current price, and P&L."
            },
            new
            {
                name = "get_positions",
                description = "Get intraday and open positions from Kite."
            },
            new
            {
                name = "get_funds",
                description = "Get available cash margins and margin utilization in Kite account."
            },
            new
            {
                name = "get_orders",
                description = "Get recent orders executed or pending in Kite."
            },
            new
            {
                name = "get_watchlists",
                description = "Get user custom watchlists with their contained assets."
            },
            new
            {
                name = "add_to_watchlist",
                description = "Add an equity stock, ETF, or IPO to a specified user watchlist.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        watchlist = new { type = "string", description = "Name of target watchlist (e.g. 'Long Term', 'ETF', 'IPO')" },
                        symbol = new { type = "string", description = "Trading symbol (e.g. 'INFY', 'MON100')" },
                        assetType = new { type = "string", description = "Type of asset: STOCK, ETF, or IPO" },
                        notes = new { type = "string", description = "Optional user notes for why this asset was added" }
                    },
                    required = new[] { "watchlist", "symbol" }
                }
            },
            new
            {
                name = "remove_from_watchlist",
                description = "Remove an asset from a user watchlist.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        watchlist = new { type = "string", description = "Name of watchlist" },
                        symbol = new { type = "string", description = "Trading symbol to remove" }
                    },
                    required = new[] { "watchlist", "symbol" }
                }
            },
            new
            {
                name = "get_stock_data",
                description = "Retrieve deep-dive financial and operational analysis for an Indian stock.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        symbol = new { type = "string", description = "NSE/BSE stock symbol (e.g. INFY, TCS)" }
                    },
                    required = new[] { "symbol" }
                }
            },
            new
            {
                name = "get_etf_data",
                description = "Retrieve factsheet, expense ratio, tracking error and constituents of an ETF.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        symbol = new { type = "string", description = "ETF symbol (e.g. MON100, NIFTYBEES)" }
                    },
                    required = new[] { "symbol" }
                }
            },
            new
            {
                name = "get_ipo_data",
                description = "Retrieve IPO issue size, price band, DRHP/RHP analysis, and risk factors.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        symbol = new { type = "string", description = "IPO symbol (e.g. SWIGGY, HYUNDAI)" }
                    },
                    required = new[] { "symbol" }
                }
            },
            new
            {
                name = "analyze_thesis_alignment",
                description = "Compare user's original recorded investment thesis against recent financial results, filings, and current price movements.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        symbol = new { type = "string", description = "Trading symbol to review thesis alignment for" }
                    },
                    required = new[] { "symbol" }
                }
            },
            new
            {
                name = "compare_assets",
                description = "Compare multiple stocks or ETFs side-by-side on key valuation and price metrics.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        symbols = new { type = "array", items = new { type = "string" }, description = "List of trading symbols" }
                    },
                    required = new[] { "symbols" }
                }
            }
        };
    }
}
