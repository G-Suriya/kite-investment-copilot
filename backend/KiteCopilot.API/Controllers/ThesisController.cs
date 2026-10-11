using KiteCopilot.Application.DTOs;
using KiteCopilot.Application.Interfaces;
using KiteCopilot.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace KiteCopilot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThesisController : ControllerBase
{
    private readonly IThesisService _thesisService;
    private readonly ILogger<ThesisController> _logger;

    public ThesisController(IThesisService thesisService, ILogger<ThesisController> logger)
    {
        _thesisService = thesisService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvestmentNote>>> GetNotes([FromQuery] string? symbol)
    {
        var notes = await _thesisService.GetNotesAsync(symbol);
        return Ok(notes);
    }

    [HttpGet("{symbol}")]
    public async Task<ActionResult<InvestmentNote>> GetThesis(string symbol)
    {
        var note = await _thesisService.GetThesisBySymbolAsync(symbol);
        if (note == null) return NotFound();
        return Ok(note);
    }

    [HttpPost]
    public async Task<ActionResult<InvestmentNote>> SaveThesis([FromBody] SaveThesisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol) || string.IsNullOrWhiteSpace(request.Thesis))
        {
            return BadRequest("Symbol and Thesis are required.");
        }

        var saved = await _thesisService.SaveThesisAsync(
            request.Symbol,
            request.Title ?? $"{request.Symbol.ToUpper()} Thesis",
            request.Thesis,
            request.TimeHorizon ?? "Long-Term (3-5 Years)",
            request.KeyRisks,
            request.WatchPoints);

        return Ok(saved);
    }

    [HttpGet("{symbol}/alignment")]
    public async Task<ActionResult<ThesisAlignmentAnalysis>> AnalyzeAlignment(string symbol)
    {
        var analysis = await _thesisService.AnalyzeThesisAlignmentAsync(symbol);
        return Ok(analysis);
    }
}

public class SaveThesisRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Thesis { get; set; } = string.Empty;
    public string? TimeHorizon { get; set; } = "Long-Term (3-5 Years)";
    public string? KeyRisks { get; set; }
    public string? WatchPoints { get; set; }
}
