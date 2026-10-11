using KiteCopilot.Application.Interfaces;
using KiteCopilot.Domain.Entities;
using KiteCopilot.Domain.Enums;
using KiteCopilot.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace KiteCopilot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WatchlistsController : ControllerBase
{
    private readonly IWatchlistService _watchlistService;
    private readonly IKiteService _kiteService;
    private readonly ILogger<WatchlistsController> _logger;

    public WatchlistsController(
        IWatchlistService watchlistService,
        IKiteService kiteService,
        ILogger<WatchlistsController> logger)
    {
        _watchlistService = watchlistService;
        _kiteService = kiteService;
        _logger = logger;
    }

    [HttpPost("sync-kite-holdings")]
    public async Task<ActionResult<SyncKiteHoldingsResult>> SyncKiteHoldings()
    {
        try
        {
            var holdings = await _kiteService.GetHoldingsAsync();
            var result = await _watchlistService.SyncKiteHoldingsAsync(holdings);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Kite holdings to watchlists.");
            return StatusCode(500, new { message = "Failed to sync Kite holdings: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Watchlist>>> GetWatchlists()
    {
        var lists = await _watchlistService.GetWatchlistsAsync();
        return Ok(lists);
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<Watchlist>> GetWatchlist(int id)
    {
        var list = await _watchlistService.GetWatchlistByIdAsync(id);
        if (list == null) return NotFound();
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<Watchlist>> CreateWatchlist([FromBody] CreateWatchlistRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Watchlist name is required.");
        }

        var wl = await _watchlistService.CreateWatchlistAsync(request.Name, request.Type ?? "Custom", request.Description);
        return CreatedAtAction(nameof(GetWatchlist), new { id = wl.Id }, wl);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> RenameWatchlist(int id, [FromBody] RenameWatchlistRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewName))
        {
            return BadRequest("New name is required.");
        }

        var success = await _watchlistService.RenameWatchlistAsync(id, request.NewName);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWatchlist(int id)
    {
        var success = await _watchlistService.DeleteWatchlistAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/items")]
    public async Task<ActionResult<WatchlistItem>> AddItem(int id, [FromBody] AddWatchlistItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            return BadRequest("Symbol is required.");
        }

        if (!Enum.TryParse<AssetType>(request.AssetType, true, out var assetType))
        {
            assetType = AssetType.STOCK;
        }

        try
        {
            var item = await _watchlistService.AddItemAsync(
                id,
                request.Symbol,
                request.Exchange ?? "NSE",
                assetType,
                request.Notes,
                request.TargetPrice);

            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id}/items/{symbol}")]
    public async Task<IActionResult> RemoveItem(int id, string symbol)
    {
        var success = await _watchlistService.RemoveItemAsync(id, symbol);
        if (!success) return NotFound();
        return NoContent();
    }
}

public class CreateWatchlistRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; } = "Custom";
    public string? Description { get; set; }
}

public class RenameWatchlistRequest
{
    public string NewName { get; set; } = string.Empty;
}

public class AddWatchlistItemRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string? Exchange { get; set; } = "NSE";
    public string? AssetType { get; set; } = "STOCK";
    public string? Notes { get; set; }
    public decimal? TargetPrice { get; set; }
}
