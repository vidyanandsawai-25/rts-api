using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// HYBRID tax strategy configuration (evaluation priority + fallback) for the
/// HYBRID calculation mode of the Dynamic Tax Register.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HybridTaxController : ControllerBase
{
    private readonly IHybridTaxService _service;
    private readonly ILogger<HybridTaxController> _logger;

    public HybridTaxController(
        IHybridTaxService service,
        ILogger<HybridTaxController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("{taxId}/config")]
    [ProducesResponseType(typeof(TaxHybridConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig(int taxId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetConfigAsync(taxId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hybrid config for tax {TaxId}", taxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving the hybrid tax configuration"
            });
        }
    }

    [HttpPut("{taxId}/config")]
    [ProducesResponseType(typeof(ApiResponse<TaxHybridConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveConfig(int taxId, [FromBody] TaxHybridConfigDto config, CancellationToken cancellationToken)
    {
        try
        {
            config.TaxId = taxId;
            var result = await _service.SaveConfigAsync(config, cancellationToken);
            return Ok(new ApiResponse<TaxHybridConfigDto> { Success = true, Items = result, Message = "Hybrid configuration saved" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving hybrid config for tax {TaxId}", taxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while saving the hybrid tax configuration"
            });
        }
    }
}
