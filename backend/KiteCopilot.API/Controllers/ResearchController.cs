using KiteCopilot.Application.DTOs;
using KiteCopilot.Application.Interfaces;
using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;
using KiteCopilot.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace KiteCopilot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResearchController : ControllerBase
{
    private readonly IResearchService _researchService;
    private readonly IRagService _ragService;
    private readonly ILogger<ResearchController> _logger;

    public ResearchController(IResearchService researchService, IRagService ragService, ILogger<ResearchController> logger)
    {
        _researchService = researchService;
        _ragService = ragService;
        _logger = logger;
    }

    [HttpGet("stock/{symbol}")]
    public async Task<ActionResult<StockAnalysisDto>> GetStockAnalysis(string symbol)
    {
        var analysis = await _researchService.GetStockAnalysisAsync(symbol);
        return Ok(analysis);
    }

    [HttpGet("etf/{symbol}")]
    public async Task<ActionResult<EtfAnalysisDto>> GetEtfAnalysis(string symbol)
    {
        var analysis = await _researchService.GetEtfAnalysisAsync(symbol);
        return Ok(analysis);
    }

    [HttpGet("ipo/{symbol}")]
    public async Task<ActionResult<IpoAnalysisDto>> GetIpoAnalysis(string symbol)
    {
        var analysis = await _researchService.GetIpoAnalysisAsync(symbol);
        return Ok(analysis);
    }

    [HttpPost("compare")]
    public async Task<ActionResult<IReadOnlyList<MarketQuote>>> CompareAssets([FromBody] CompareAssetsRequest request)
    {
        var quotes = await _researchService.CompareAssetsAsync(request.Symbols);
        return Ok(quotes);
    }

    [HttpGet("documents")]
    public async Task<ActionResult<IReadOnlyList<ResearchDocument>>> GetDocuments([FromQuery] string? symbol, [FromQuery] string? assetType)
    {
        AssetType? parsedAssetType = null;
        if (!string.IsNullOrEmpty(assetType) && Enum.TryParse<AssetType>(assetType, true, out var at))
        {
            parsedAssetType = at;
        }

        var docs = await _ragService.GetDocumentsAsync(symbol, parsedAssetType);
        return Ok(docs);
    }

    [HttpPost("documents/search")]
    public async Task<ActionResult<IReadOnlyList<DocumentChunkResult>>> SearchDocuments([FromBody] SearchDocumentsRequest request)
    {
        DocumentType? parsedDocType = null;
        if (!string.IsNullOrEmpty(request.DocumentType) && Enum.TryParse<DocumentType>(request.DocumentType, true, out var dt))
        {
            parsedDocType = dt;
        }

        var results = await _ragService.SearchDocumentsAsync(request.Query, request.Symbol, parsedDocType, request.Limit ?? 5);
        return Ok(results);
    }

    [HttpPost("documents/upload")]
    public async Task<ActionResult<ResearchDocument>> UploadDocument([FromBody] UploadDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Symbol))
        {
            return BadRequest("Symbol, Title and Content are required.");
        }

        if (!Enum.TryParse<AssetType>(request.AssetType, true, out var assetType)) assetType = AssetType.STOCK;
        if (!Enum.TryParse<DocumentType>(request.DocumentType, true, out var docType)) docType = DocumentType.QuarterlyResult;

        var doc = await _ragService.IngestDocumentAsync(
            request.Symbol,
            request.Company ?? request.Symbol,
            assetType,
            docType,
            request.Title,
            request.Content,
            request.FinancialYear,
            request.Quarter,
            request.Source ?? "User Upload");

        return Ok(doc);
    }
}

public class CompareAssetsRequest
{
    public List<string> Symbols { get; set; } = new();
}

public class SearchDocumentsRequest
{
    public string Query { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public string? DocumentType { get; set; }
    public int? Limit { get; set; } = 5;
}

public class UploadDocumentRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? AssetType { get; set; } = "STOCK";
    public string? DocumentType { get; set; } = "QuarterlyResult";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? FinancialYear { get; set; }
    public string? Quarter { get; set; }
    public string? Source { get; set; }
}
