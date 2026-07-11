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

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RTSCitizenSessionQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRTSCitizenSessionDto dto, CancellationToken ct)
        => this.ExecuteCreate(_service, dto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRTSCitizenSessionDto dto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, dto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

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
}
