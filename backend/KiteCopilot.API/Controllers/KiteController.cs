using KiteCopilot.Application.Interfaces;
using KiteCopilot.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace KiteCopilot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KiteController : ControllerBase
{
    private readonly IKiteService _kiteService;
    private readonly ILogger<KiteController> _logger;

    public KiteController(IKiteService kiteService, ILogger<KiteController> logger)
    {
        _kiteService = kiteService;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<ActionResult<KiteSessionInfo>> GetStatus()
    {
        var profile = await _kiteService.GetProfileAsync();
        return Ok(new KiteSessionInfo
        {
            IsAuthenticated = true,
            IsDemoMode = _kiteService.IsDemoMode,
            UserId = profile.UserId,
            UserName = profile.UserName,
            LoginTime = profile.ConnectedAt,
            ApiKeyMasked = "kite_live_****"
        });
    }

    [HttpPost("demo-mode")]
    public ActionResult ToggleDemoMode([FromBody] DemoModeRequest request)
    {
        _kiteService.SetDemoMode(request.EnableDemo);
        return Ok(new { isDemoMode = _kiteService.IsDemoMode });
    }

    [HttpPost("set-token")]
    public async Task<ActionResult> SetToken([FromBody] System.Text.Json.JsonElement body)
    {
        if (body.TryGetProperty("token", out var tokenEl))
        {
            var token = tokenEl.GetString()?.Trim();
            if (!string.IsNullOrEmpty(token))
            {
                var sessionPath = Path.Combine(AppContext.BaseDirectory, "kite_session.json");
                await System.IO.File.WriteAllTextAsync(sessionPath, token);
                return Ok(new { success = true, message = "Kite access token saved successfully." });
            }
        }
        return BadRequest(new { success = false, message = "Invalid token provided." });
    }

    [HttpGet("login")]
    public ActionResult GetLoginUrl()
    {
        var url = _kiteService.GetLoginUrl();
        return Ok(new { loginUrl = url });
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? request_token, [FromQuery] string? status)
    {
        var clientHost = "192.168.1.2";
        var frontendBase = $"http://{clientHost}:4200";

        if (status == "cancelled" || string.IsNullOrEmpty(request_token))
        {
            return Redirect($"{frontendBase}/#/kite-auth?status=cancelled");
        }

        var success = await _kiteService.ExchangeTokenAsync(request_token);
        if (success)
        {
            return Redirect($"{frontendBase}/#/portfolio?auth=success");
        }

        return Redirect($"{frontendBase}/#/kite-auth?status=failed");
    }

    [HttpGet("profile")]
    public async Task<ActionResult<KiteProfile>> GetProfile()
    {
        var profile = await _kiteService.GetProfileAsync();
        return Ok(profile);
    }

    [HttpGet("funds")]
    public async Task<ActionResult<Funds>> GetFunds()
    {
        var funds = await _kiteService.GetFundsAsync();
        return Ok(funds);
    }

    [HttpGet("holdings")]
    public async Task<ActionResult<IReadOnlyList<Holding>>> GetHoldings()
    {
        var holdings = await _kiteService.GetHoldingsAsync();
        return Ok(holdings);
    }

    [HttpGet("positions")]
    public async Task<ActionResult<IReadOnlyList<Position>>> GetPositions()
    {
        var positions = await _kiteService.GetPositionsAsync();
        return Ok(positions);
    }

    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<Order>>> GetOrders()
    {
        var orders = await _kiteService.GetOrdersAsync();
        return Ok(orders);
    }

    [HttpGet("quote/{symbol}")]
    public async Task<ActionResult<MarketQuote>> GetQuote(string symbol)
    {
        var quote = await _kiteService.GetQuoteAsync(symbol);
        return Ok(quote);
    }

    [HttpPost("quotes")]
    public async Task<ActionResult<IReadOnlyList<MarketQuote>>> GetQuotes([FromBody] IEnumerable<string> symbols)
    {
        var quotes = await _kiteService.GetQuotesAsync(symbols);
        return Ok(quotes);
    }
}

public class DemoModeRequest
{
    public bool EnableDemo { get; set; }
}
