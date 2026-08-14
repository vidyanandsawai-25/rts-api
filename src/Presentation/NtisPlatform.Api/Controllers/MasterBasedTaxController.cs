using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Master-based tax configuration — keyed lookup grid (FIXED/PERCENT result per master key)
/// for the MASTER_BASED calculation mode of the Dynamic Tax Register.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MasterBasedTaxController : ControllerBase
{
    private readonly IMasterBasedTaxService _service;
    private readonly ILogger<MasterBasedTaxController> _logger;

    public MasterBasedTaxController(
        IMasterBasedTaxService service,
        ILogger<MasterBasedTaxController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("mappings")]
    [ProducesResponseType(typeof(PagedResult<TaxMasterMappingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMappings(
        [FromQuery] int taxId,
        [FromQuery] int? assessmentYearRangeId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.GetMappingsAsync(taxId, assessmentYearRangeId, pageNumber, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving master-based mappings for tax {TaxId}", taxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving master-based tax mappings"
            });
        }
    }

    [HttpPost("save")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Save([FromBody] SaveMasterMappingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var affected = await _service.SaveAsync(request, cancellationToken);
            return Ok(new ApiResponse<int> { Success = true, Items = affected, Message = $"{affected} row(s) saved" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving master-based mappings for tax {TaxId}", request.TaxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while saving master-based tax mappings"
            });
        }
    }

    [HttpPost("bulk-apply")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkApply([FromBody] BulkApplyMasterMappingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var affected = await _service.BulkApplyAsync(request, cancellationToken);
            return Ok(new ApiResponse<int> { Success = true, Items = affected, Message = $"Applied to {affected} row(s)" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk-applying master-based mappings for tax {TaxId}", request.TaxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while bulk-applying master-based tax mappings"
            });
        }
    }
}
