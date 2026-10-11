using System.Text.Json;
using KiteCopilot.Application.Common.Interfaces;
using KiteCopilot.Application.DTOs;
using KiteCopilot.Application.Interfaces;
using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KiteCopilot.Infrastructure.Services;

public class RagService : IRagService
{
    private readonly IAppDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<RagService> _logger;

    public RagService(IAppDbContext context, IEmbeddingService embeddingService, ILogger<RagService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentChunkResult>> SearchDocumentsAsync(string query, string? symbol = null, DocumentType? docType = null, int limit = 5)
    {
        var queryVector = await _embeddingService.GenerateEmbeddingAsync(query);

        var queryable = _context.DocumentChunks
            .Include(c => c.Document)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(symbol))
        {
            var cleanSymbol = symbol.Trim().ToUpperInvariant();
            queryable = queryable.Where(c => c.Document!.Symbol.ToUpper() == cleanSymbol);
        }

        if (docType.HasValue)
        {
            queryable = queryable.Where(c => c.Document!.DocumentType == docType.Value);
        }

        var chunks = await queryable.ToListAsync();
        var scoredList = new List<DocumentChunkResult>();

        foreach (var chunk in chunks)
        {
            if (string.IsNullOrWhiteSpace(chunk.EmbeddingJson)) continue;

            try
            {
                var chunkVector = JsonSerializer.Deserialize<float[]>(chunk.EmbeddingJson);
                if (chunkVector == null) continue;

                var score = _embeddingService.CosineSimilarity(queryVector, chunkVector);
                scoredList.Add(new DocumentChunkResult
                {
                    DocumentId = chunk.DocumentId,
                    Symbol = chunk.Document?.Symbol ?? "",
                    Company = chunk.Document?.Company ?? "",
                    DocumentType = chunk.Document?.DocumentType.ToString() ?? "",
                    Title = chunk.Document?.Title ?? "",
                    Content = chunk.Content,
                    SimilarityScore = score,
                    Quarter = chunk.Document?.Quarter,
                    Year = chunk.Document?.FinancialYear,
                    Source = chunk.Document?.Source ?? "Filing"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse chunk embedding for chunk {Id}", chunk.Id);
            }
        }

        return scoredList
            .OrderByDescending(s => s.SimilarityScore)
            .Take(limit)
            .ToList();
    }

    public async Task<ResearchDocument> IngestDocumentAsync(string symbol, string company, AssetType assetType, DocumentType docType, string title, string content, int? year = null, string? quarter = null, string source = "Filing")
    {
        var doc = new ResearchDocument
        {
            Symbol = symbol.Trim().ToUpperInvariant(),
            Company = company.Trim(),
            AssetType = assetType,
            DocumentType = docType,
            Title = title.Trim(),
            Content = content.Trim(),
            FinancialYear = year,
            Quarter = quarter,
            Source = source,
            PublishedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var chunks = SplitIntoChunks(content, chunkSize: 300, overlap: 50);
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunkText = chunks[i];
            var vec = await _embeddingService.GenerateEmbeddingAsync($"{symbol} {title} {chunkText}");
            doc.Chunks.Add(new DocumentChunk
            {
                ChunkIndex = i,
                Content = chunkText,
                MetadataJson = JsonSerializer.Serialize(new { symbol, company, docType = docType.ToString(), year, quarter }),
                EmbeddingJson = JsonSerializer.Serialize(vec),
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.ResearchDocuments.Add(doc);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Ingested document '{Title}' with {Count} chunks for {Symbol}", title, chunks.Count, symbol);
        return doc;
    }

    public async Task<IReadOnlyList<ResearchDocument>> GetDocumentsAsync(string? symbol = null, AssetType? assetType = null)
    {
        var q = _context.ResearchDocuments
            .Include(d => d.Chunks)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(symbol))
        {
            var cleanSymbol = symbol.Trim().ToUpperInvariant();
            q = q.Where(d => d.Symbol.ToUpper() == cleanSymbol);
        }

        if (assetType.HasValue)
        {
            q = q.Where(d => d.AssetType == assetType.Value);
        }

        return await q.OrderByDescending(d => d.PublishedDate).ToListAsync();
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
