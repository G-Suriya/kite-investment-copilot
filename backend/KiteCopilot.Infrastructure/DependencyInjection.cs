using KiteCopilot.Application.Common.Interfaces;
using KiteCopilot.Application.Interfaces;
using KiteCopilot.Infrastructure.Data;
using KiteCopilot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KiteCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? (dbProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase) 
                ? "Host=localhost;Port=5432;Database=kite_copilot;Username=postgres;Password=postgres" 
                : "Data Source=kite_copilot.db");

        if (dbProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, o => o.UseVector()));
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));
        }

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // HTTP Client
        services.AddHttpClient<IKiteService, KiteService>();
        services.AddHttpClient<IEmbeddingService, EmbeddingService>();
        services.AddHttpClient<IGeminiCopilotService, GeminiCopilotService>();

        // Application Services
        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddScoped<IRagService, RagService>();
        services.AddScoped<IThesisService, ThesisService>();
        services.AddScoped<IResearchService, ResearchService>();
        services.AddScoped<IMarketIntelligenceService, MarketIntelligenceService>();

        return services;
    }
}
