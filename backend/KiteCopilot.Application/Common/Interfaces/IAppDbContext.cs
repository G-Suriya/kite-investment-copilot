using Microsoft.EntityFrameworkCore;
using KiteCopilot.Domain.Entities;

namespace KiteCopilot.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Watchlist> Watchlists { get; }
    DbSet<WatchlistItem> WatchlistItems { get; }
    DbSet<InvestmentNote> InvestmentNotes { get; }
    DbSet<ResearchDocument> ResearchDocuments { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }
    DbSet<AiAuditLog> AiAuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
