using KiteCopilot.Domain.Enums;

namespace KiteCopilot.Domain.Entities;

public class ResearchDocument
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public AssetType AssetType { get; set; } = AssetType.STOCK;
    public DocumentType DocumentType { get; set; } = DocumentType.QuarterlyResult;
    public int? FinancialYear { get; set; }
    public string? Quarter { get; set; } // Q1, Q2, Q3, Q4, Annual
    public DateTime PublishedDate { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "BSE/NSE Filing";
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DocumentChunk> Chunks { get; set; } = new();
}

public class DocumentChunk
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    
    // Portable vector embedding representation
    public string? EmbeddingJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ResearchDocument? Document { get; set; }
}
