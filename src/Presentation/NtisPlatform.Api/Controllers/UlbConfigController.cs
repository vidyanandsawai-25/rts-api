using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// ULB (Urban Local Body) configuration controller
/// Provides organization configuration details like logo, name, and contact information
/// </summary>
[ApiController]
[Route("api/[controller]")]

public class UlbConfigController : ControllerBase
{
    private readonly IUlbConfigService _ulbConfigService;
    private readonly ILogger<UlbConfigController> _logger;

    public UlbConfigController(IUlbConfigService ulbConfigService, ILogger<UlbConfigController> logger)
    {
        _ulbConfigService = ulbConfigService;
        _logger = logger;
    }

    /// <summary>
    /// Get ULB configuration - organization details like logo, name, contact info
    /// Public endpoint - needed for login page display
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ULB configuration details</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UlbConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        try
        {
            var ulbConfig = await _ulbConfigService.GetUlbConfigAsync(cancellationToken);

            if (ulbConfig == null)
            {
                _logger.LogWarning("No active ULB configuration found in database");
                return NotFound(new { message = "ULB configuration not found" });
            }

            return Ok(ulbConfig);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or request timed out - let it propagate
            // ASP.NET Core will handle this appropriately (no 500 error logged)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ULB configuration");
            return StatusCode(500, new { message = "An error occurred while retrieving ULB configuration" });
        }
    }
}
