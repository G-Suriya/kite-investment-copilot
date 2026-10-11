using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;
using KiteCopilot.Domain.Models;

namespace KiteCopilot.Application.DTOs;

public class ChatCopilotRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string ExperienceLevel { get; set; } = "adaptive"; // "beginner", "pro", "adaptive"
    public List<ChatMessageHistoryDto> History { get; set; } = new();
}

public class ChatMessageHistoryDto
{
    public string Role { get; set; } = "user"; // "user", "model", "tool"
    public string Content { get; set; } = string.Empty;
    public string? ToolName { get; set; }
    public string? ToolCallId { get; set; }
}

public class ChatCopilotResponse
{
    public string Response { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string? BeginnerSummary { get; set; }
    public string? ProAnalysis { get; set; }
    public string ExperienceLevel { get; set; } = "adaptive";
    public List<ToolCallExecutionResult> ToolCallsExecuted { get; set; } = new();
    public List<string> SuggestedFollowUps { get; set; } = new();
    public bool GuardrailTriggered { get; set; }
    public string? GuardrailMessage { get; set; }
}

public class ToolCallExecutionResult
{
    public string ToolName { get; set; } = string.Empty;
    public object? Arguments { get; set; }
    public object? Result { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
}

public class DocumentChunkResult
{
    public int DocumentId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float SimilarityScore { get; set; }
    public string? Quarter { get; set; }
    public int? Year { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class ThesisAlignmentAnalysis
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string OriginalThesis { get; set; } = string.Empty;
    public string TimeHorizon { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal? TargetPrice { get; set; }
    public List<string> ConfirmatoryFactors { get; set; } = new();
    public List<string> ContradictoryOrRiskFactors { get; set; } = new();
    public List<string> DevelopmentsSinceThesis { get; set; } = new();
    public List<string> KeyQuestionsToInvestigate { get; set; } = new();
    public string OverallAssessment { get; set; } = string.Empty; // "Thesis Intact", "Watch Closely", "Under Review"
}

public class StockAnalysisDto
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public MarketQuote Quote { get; set; } = new();
    public Holding? CurrentHolding { get; set; }
    public InvestmentNote? UserThesis { get; set; }
    public List<DocumentChunkResult> KeyFilingExcerpts { get; set; } = new();
    public string BusinessSummary { get; set; } = string.Empty;
    public Dictionary<string, string> FinancialHighlights { get; set; } = new();
    public List<string> RisksAndWatchlistPoints { get; set; } = new();
}

public class EtfAnalysisDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnderlyingIndex { get; set; } = string.Empty;
    public MarketQuote Quote { get; set; } = new();
    public decimal ExpenseRatio { get; set; }
    public decimal TrackingError { get; set; }
    public decimal AumCrores { get; set; }
    public List<string> TopHoldings { get; set; } = new();
    public string SectorDistribution { get; set; } = string.Empty;
    public string ThesisSuitability { get; set; } = string.Empty;
    public List<string> WatchPoints { get; set; } = new();
}

public class IpoAnalysisDto
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string IssuePeriod { get; set; } = string.Empty;
    public string PriceBand { get; set; } = string.Empty;
    public decimal IssueSizeCrores { get; set; }
    public decimal FreshIssueCrores { get; set; }
    public decimal OfsCrores { get; set; }
    public string PostIssuePe { get; set; } = string.Empty;
    public string PromoterHoldingPrePost { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> KeyRisks { get; set; } = new();
    public string Status { get; set; } = "UPCOMING"; // UPCOMING, ACTIVE, CLOSED, LISTED
}
