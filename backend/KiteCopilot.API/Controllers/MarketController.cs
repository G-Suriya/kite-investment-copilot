using KiteCopilot.Application.DTOs;
using KiteCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KiteCopilot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketController : ControllerBase
{
    private readonly IMarketIntelligenceService _marketService;
    private readonly ILogger<MarketController> _logger;

    public MarketController(IMarketIntelligenceService marketService, ILogger<MarketController> logger)
    {
        _marketService = marketService;
        _logger = logger;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<MultiSourceMarketPulseDto>> GetMarketOverview()
    {
        var overview = await _marketService.GetMarketOverviewAsync();
        return Ok(overview);
    }

    [HttpGet("ipos")]
    public async Task<ActionResult<IReadOnlyList<IpoGmpItemDto>>> GetIpos()
    {
        var ipos = await _marketService.GetIposWithGmpAsync();
        return Ok(ipos);
    }

    [HttpGet("futures")]
    public async Task<ActionResult<IReadOnlyList<FuturesTractionDto>>> GetFutures()
    {
        var futures = await _marketService.GetFuturesTractionAsync();
        return Ok(futures);
    }

    [HttpGet("financials/{symbol}")]
    public async Task<ActionResult<CompanyFinancialsDto>> GetFinancials(string symbol)
    {
        var financials = await _marketService.GetCompanyFinancialsAsync(symbol);
        return Ok(financials);
    }

    [HttpGet("gmp-explained")]
    public async Task<ActionResult<object>> GetGmpExplanation()
    {
        var explanation = await _marketService.ExplainGmpSimpleAsync();
        return Ok(new { explanation });
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<GrowthOpportunitiesResponseDto>> GetSuggestions()
    {
        var suggestions = await _marketService.GetGrowthOpportunitiesAsync();
        return Ok(suggestions);
    }
}
