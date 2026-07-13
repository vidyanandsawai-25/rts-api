using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RTSCitizenSession;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class RTSCitizenSessionController : ControllerBase
{
    private readonly IRTSCitizenSessionService _service;
    private readonly ILogger<RTSCitizenSessionController> _logger;

    public RTSCitizenSessionController(
        IRTSCitizenSessionService service,
        ILogger<RTSCitizenSessionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRTSCitizenSessionDto dto, CancellationToken ct)
        => this.ExecuteCreate(_service, dto, _logger, ct);

    [AllowAnonymous]
    [HttpGet("validate/{sessionId}")]
    public async Task<IActionResult> ValidateSession(string sessionId, CancellationToken ct)
    {
        try
        {
            var result = await _service.ValidateAndUpdateSessionAsync(sessionId, ct);
            if (!result.Success)
            {
                return Unauthorized(result);
            }
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error validating session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [AllowAnonymous]
    [HttpPost("logout/{sessionId}")]
    public async Task<IActionResult> LogoutSession(string sessionId, CancellationToken ct)
    {
        try
        {
            var result = await _service.LogoutSessionAsync(sessionId, ct);
            if (!result)
            {
                return NotFound(new { message = "Session not found or already inactive" });
            }
            return Ok(new { success = true, message = "Logged out successfully" });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error logging out session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

