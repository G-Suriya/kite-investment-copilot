using KiteCopilot.Application.DTOs;
using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;
using KiteCopilot.Domain.Models;


namespace KiteCopilot.Application.Interfaces;

public interface IRagService
{
    Task<IReadOnlyList<DocumentChunkResult>> SearchDocumentsAsync(string query, string? symbol = null, DocumentType? docType = null, int limit = 5);
    Task<ResearchDocument> IngestDocumentAsync(string symbol, string company, AssetType assetType, DocumentType docType, string title, string content, int? year = null, string? quarter = null, string source = "Filing");
    Task<IReadOnlyList<ResearchDocument>> GetDocumentsAsync(string? symbol = null, AssetType? assetType = null);
}

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    float CosineSimilarity(float[] vectorA, float[] vectorB);
}

public interface IThesisService
{
    Task<IReadOnlyList<InvestmentNote>> GetNotesAsync(string? symbol = null);
    Task<InvestmentNote?> GetThesisBySymbolAsync(string symbol);
    Task<InvestmentNote> SaveThesisAsync(string symbol, string title, string thesis, string timeHorizon, string? risks, string? watchPoints);
    Task<ThesisAlignmentAnalysis> AnalyzeThesisAlignmentAsync(string symbol);
}

public interface IResearchService
{
    Task<StockAnalysisDto> GetStockAnalysisAsync(string symbol);
    Task<EtfAnalysisDto> GetEtfAnalysisAsync(string symbol);
    Task<IpoAnalysisDto> GetIpoAnalysisAsync(string symbol);
    Task<IReadOnlyList<MarketQuote>> CompareAssetsAsync(IEnumerable<string> symbols);
}

public interface IGeminiCopilotService
{
    Task<ChatCopilotResponse> ProcessChatAsync(ChatCopilotRequest request);
}
