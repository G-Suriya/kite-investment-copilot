using KiteCopilot.Application.DTOs;

namespace KiteCopilot.Application.Interfaces;

public interface IMarketIntelligenceService
{
    Task<MultiSourceMarketPulseDto> GetMarketOverviewAsync();
    Task<IReadOnlyList<IpoGmpItemDto>> GetIposWithGmpAsync();
    Task<IReadOnlyList<FuturesTractionDto>> GetFuturesTractionAsync();
    Task<CompanyFinancialsDto> GetCompanyFinancialsAsync(string symbol);
    Task<string> ExplainGmpSimpleAsync();
    Task<ConceptExplanationDto> ExplainConceptAsync(string concept);
    Task<TechnicalAnalysisDto> GetTechnicalsAsync(string symbol);
    Task<GrowthOpportunitiesResponseDto> GetGrowthOpportunitiesAsync();
}
