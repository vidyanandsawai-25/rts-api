using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Manage per-property, per-screen locks (PTIS Lock-Unlock Property).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LockUnlockController : ControllerBase
{
    private readonly ILockUnlockService _service;
    private readonly ILogger<LockUnlockController> _logger;
    private readonly IWebHostEnvironment _environment;

    public LockUnlockController(
        ILockUnlockService service,
        ILogger<LockUnlockController> logger,
        IWebHostEnvironment environment)
    {
        _service = service;
        _logger = logger;
        _environment = environment;
    }

    // GET api/LockUnlock/screens
    [HttpGet("screens")]
    [ProducesResponseType(typeof(ApiResponse<List<LockableScreenDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScreens(CancellationToken ct)
    {
        try
        {
            var result = await _service.GetLockableScreensAsync(ct);
            return Ok(new ApiResponse<List<LockableScreenDto>>
            {
                Success = true,
                Message = "Lockable screens fetched successfully",
                Items = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized access. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Valid user identification is required.", CorrelationId = correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error fetching lockable screens. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = _environment.IsDevelopment() ? $"An error occurred: {ex.Message}" : "An error occurred",
                CorrelationId = correlationId
            });
        }
    }

    // GET api/LockUnlock/properties
    [HttpGet("properties")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PropertyLockRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProperties([FromQuery] FilterPropertyLocksRequestDto request, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetPropertyLocksAsync(request, ct);
            return Ok(new ApiResponse<PagedResult<PropertyLockRowDto>>
            {
                Success = true,
                Message = "Property locks fetched successfully",
                Items = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized access. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Valid user identification is required.", CorrelationId = correlationId });
        }
        catch (ArgumentException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Validation error. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, CorrelationId = correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error filtering property locks. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = _environment.IsDevelopment() ? $"An error occurred: {ex.Message}" : "An error occurred",
                CorrelationId = correlationId
            });
        }
    }

    // POST api/LockUnlock/bulk
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(ApiResponse<BulkLockResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Bulk([FromBody] BulkLockRequestDto request, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            var result = await _service.BulkApplyAsync(request, userId, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized access. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Valid user identification is required.", CorrelationId = correlationId });
        }
        catch (ArgumentException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Validation error during bulk lock/unlock. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, CorrelationId = correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error during bulk lock/unlock. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = _environment.IsDevelopment() ? $"An error occurred: {ex.Message}" : "An error occurred",
                CorrelationId = correlationId
            });
        }
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
            throw new UnauthorizedAccessException("Valid user identification is required.");
        return id;
    }
}
