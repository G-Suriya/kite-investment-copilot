using Microsoft.EntityFrameworkCore;
using KiteCopilot.Application.Common.Interfaces;
using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;

namespace KiteCopilot.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Watchlist> Watchlists => Set<Watchlist>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<InvestmentNote> InvestmentNotes => Set<InvestmentNote>();
    public DbSet<ResearchDocument> ResearchDocuments => Set<ResearchDocument>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<AiAuditLog> AiAuditLogs => Set<AiAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Watchlist>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Name).IsRequired().HasMaxLength(100);
            entity.HasMany(w => w.Items)
                  .WithOne(i => i.Watchlist)
                  .HasForeignKey(i => i.WatchlistId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WatchlistItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Symbol).IsRequired().HasMaxLength(50);
            entity.Property(i => i.Exchange).HasMaxLength(20);
            entity.Property(i => i.AssetType).HasConversion<string>();
        });

        modelBuilder.Entity<InvestmentNote>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Symbol).IsRequired().HasMaxLength(50);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<ResearchDocument>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Symbol).IsRequired().HasMaxLength(50);
            entity.Property(d => d.AssetType).HasConversion<string>();
            entity.Property(d => d.DocumentType).HasConversion<string>();
            entity.HasMany(d => d.Chunks)
                  .WithOne(c => c.Document)
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(c => c.Id);
        });

        modelBuilder.Entity<AiAuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.User).HasMaxLength(100);
            entity.Property(a => a.Tool).HasMaxLength(100);
        });
    }
}
