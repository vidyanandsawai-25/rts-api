using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Value-based tax configuration — per-type-of-use percentages on RV,
/// backing the VALUE_BASED calculation mode of the Dynamic Tax Register.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ValueBasedTaxController : ControllerBase
{
    private readonly IValueBasedTaxService _service;
    private readonly ILogger<ValueBasedTaxController> _logger;

    public ValueBasedTaxController(
        IValueBasedTaxService service,
        ILogger<ValueBasedTaxController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("percentages")]
    [ProducesResponseType(typeof(PagedResult<ValueBasedTaxRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPercentages(
        [FromQuery] int taxId,
        [FromQuery] int? yearRangeRVId,
        [FromQuery] string? userGroup,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.GetPercentagesAsync(taxId, yearRangeRVId, userGroup, pageNumber, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error retrieving value-based percentages for tax {TaxId}", taxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value-based percentages for tax {TaxId}", taxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving value-based tax percentages"
            });
        }
    }

    [HttpPost("save")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Save([FromBody] SaveValueBasedTaxRequest request, CancellationToken cancellationToken)
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
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error saving value-based percentages for tax {TaxId}", request.TaxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving value-based percentages for tax {TaxId}", request.TaxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while saving value-based tax percentages"
            });
        }
    }

    [HttpPost("bulk-apply")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkApply([FromBody] BulkApplyValueBasedTaxRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var affected = await _service.BulkApplyAsync(request, cancellationToken);
            return Ok(new ApiResponse<int> { Success = true, Items = affected, Message = $"Applied to {affected} row(s)" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error bulk-applying value-based percentages for tax {TaxId}", request.TaxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk-applying value-based percentages for tax {TaxId}", request.TaxId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while bulk-applying value-based tax percentages"
            });
        }
    }
}
