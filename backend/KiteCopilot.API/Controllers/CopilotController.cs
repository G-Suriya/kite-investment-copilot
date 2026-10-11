using KiteCopilot.Application.Common.Interfaces;
using KiteCopilot.Application.DTOs;
using KiteCopilot.Application.Interfaces;
using KiteCopilot.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiteCopilot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CopilotController : ControllerBase
{
    private readonly IGeminiCopilotService _copilotService;
    private readonly IAppDbContext _context;
    private readonly ILogger<CopilotController> _logger;

    public CopilotController(IGeminiCopilotService copilotService, IAppDbContext context, ILogger<CopilotController> logger)
    {
        _copilotService = copilotService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatCopilotResponse>> Chat([FromBody] ChatCopilotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty.");
        }

        var response = await _copilotService.ProcessChatAsync(request);
        return Ok(response);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<IReadOnlyList<AiAuditLog>>> GetAuditLogs([FromQuery] int limit = 50)
    {
        var logs = await _context.AiAuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync();

        return Ok(logs);
    }
}
