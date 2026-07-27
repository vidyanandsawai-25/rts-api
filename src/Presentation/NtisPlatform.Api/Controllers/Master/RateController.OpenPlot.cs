using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Master;

public partial class RateController
{
    [HttpGet("typeofuse")]
    public async Task<IActionResult> GetTypeOfUseDetails([FromQuery] TypeOfUseQueryParameters queryParameters, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetTypeOfUseDetailsAsync(queryParameters, ct);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FilterValidationException ex)
        {
            _logger.LogWarning(ex, "Filter validation failed: {Message}", ex.Message);
            return BadRequest(new
            {
                message = ex.Message,
                errors = ex.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTypeOfUseDetails");
            return StatusCode(500, new { message = "An error occurred while fetching type of use details" });
        }
    }

    [HttpGet("openplot/typeofuse")]
    public async Task<IActionResult> GetOpenPlotTypeOfUseDetails([FromQuery] TypeOfUseQueryParameters queryParameters, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetOpenPlotTypeOfUseDetailsAsync(queryParameters, ct);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FilterValidationException ex)
        {
            _logger.LogWarning(ex, "Filter validation failed: {Message}", ex.Message);
            return BadRequest(new
            {
                message = ex.Message,
                errors = ex.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOpenPlotTypeOfUseDetails");
            return StatusCode(500, new { message = "An error occurred while fetching open plot type of use details" });
        }
    }

    [HttpPost("openplot")]
    public async Task<IActionResult> CreateOpenPlot([FromBody] CreateOpenPlotRateDto createDto, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateOpenPlotAsync(createDto, ct);
            return Ok(new ApiResponse<RateDto>
            {
                Success = true,
                Message = "Record inserted successfully",
                Items = result
            });
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "CreateOpenPlot operation failed");
            var errorMessage = ex.InnerException?.Message ?? ex.Message;
            if (errorMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new ApiResponse<RateDto>
                {
                    Success = false,
                    Message = "A record with the same details already exists."
                });
            }
            return StatusCode(500, new ApiResponse<RateDto>
            {
                Success = false,
                Message = "An error occurred while creating the record",
                Items = default
            });
        }
    }

    /// Creates multiple open plot records in a single Bulk.
    [HttpPost("openplot/Bulk")]
    public async Task<IActionResult> BulkCreateOpenPlot([FromBody] CreateOpenPlotRateDto[] items, CancellationToken ct)
    {
        if (items == null || items.Length == 0)
        {
            return BadRequest(new ApiResponse<BulkResult<RateDto>>
            {
                Success = false,
                Message = "No items provided for Bulk create."
            });
        }

        try
        {
            var result = await _service.BulkCreateOpenPlotAsync(items, ct);
            return Ok(new ApiResponse<BulkResult<RateDto>>
            {
                Success = result.AllSucceeded,
                Message = result.HasFailures
                    ? $"{result.SuccessCount} records created, {result.FailedCount} failed"
                    : $"{result.SuccessCount} records created successfully",
                Items = result,
                Errors = result.Errors?.ToList()
            });
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "BulkCreateOpenPlot operation failed for {Count} items", items.Length);
            var errorMessage = ex.InnerException?.Message ?? ex.Message;
            if (errorMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new ApiResponse<BulkResult<RateDto>>
                {
                    Success = false,
                    Message = "A record with the same details already exists."
                });
            }
            return StatusCode(500, new ApiResponse<BulkResult<RateDto>>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            });
        }
    }
}
