using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RTSAppeal;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// API Controller governing Right to Services (RTS) citizen appeal submissions and appellate officer adjudication
/// in accordance with the Maharashtra Right to Public Services Act (MRTSA 2015).
/// </summary>
[Route("api/[controller]")]
[Route("api/rts/appeal")]
[Route("api/rts-appeal")]
[ApiController]
public class RTSAppealController : ControllerBase
{
    private readonly IRTSAppealService _service;
    private readonly ILogger<RTSAppealController> _logger;

    public RTSAppealController(IRTSAppealService service, ILogger<RTSAppealController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all active statutory grounds and categories for filing an appeal.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Standard API response containing list of appeal types.</returns>
    [AllowAnonymous]
    [HttpGet("types")]
    [ProducesResponseType(typeof(ApiResponse<List<RTSAppealTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAppealTypes(CancellationToken ct)
    {
        var types = await _service.GetAppealTypesAsync(ct);
        return Ok(new ApiResponse<List<RTSAppealTypeDto>>
        {
            Success = true,
            Message = "Appeal types retrieved successfully",
            Items = types
        });
    }


    [AllowAnonymous]
    [HttpGet("check-level/{applicationNo}")]
    [ProducesResponseType(typeof(ApiResponse<RTSAppealLevelCheckResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckAppealLevel(string applicationNo, CancellationToken ct)
    {
        var result = await _service.CheckAppealLevelAsync(applicationNo, ct);
        return Ok(new ApiResponse<RTSAppealLevelCheckResultDto>
        {
            Success = true,
            Message = "Appeal level evaluated successfully",
            Items = result
        });
    }

    [AllowAnonymous]
    [HttpGet("summary/{applicationNo}")]
    [HttpGet("application-summary/{applicationNo}")]
    [ProducesResponseType(typeof(ApiResponse<RTSAppealApplicationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSummary(string applicationNo, CancellationToken ct)
    {
        var summary = await _service.GetApplicationSummaryForAppealAsync(applicationNo, ct);
        if (summary == null)
        {
            return NotFound(new ApiResponse<RTSAppealApplicationSummaryDto>
            {
                Success = false,
                Message = $"Application #{applicationNo} not found"
            });
        }

        return Ok(new ApiResponse<RTSAppealApplicationSummaryDto>
        {
            Success = true,
            Message = "Application summary retrieved successfully",
            Items = summary
        });
    }

    [AllowAnonymous]
    [HttpPost("submit")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RTSAppealApplicationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitAppeal([FromBody] CreateRTSAppealApplicationDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new ApiResponse<RTSAppealApplicationResultDto>
            {
                Success = false,
                Message = "Invalid request validation data",
                Errors = errors
            });
        }

        try
        {
            var result = await _service.SubmitAppealAsync(dto, ct);
            return Ok(new ApiResponse<RTSAppealApplicationResultDto>
            {
                Success = result.Success,
                Message = result.Message,
                Items = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error during appeal submission: {Message}", ex.Message);
            return BadRequest(new ApiResponse<RTSAppealApplicationResultDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation during appeal submission: {Message}", ex.Message);
            return BadRequest(new ApiResponse<RTSAppealApplicationResultDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during appeal submission");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<RTSAppealApplicationResultDto>
            {
                Success = false,
                Message = "An unexpected error occurred while processing the appeal submission."
            });
        }
    }

    [HttpGet("dashboard-cards")]
    [ProducesResponseType(typeof(ApiResponse<RTSAppealDashboardCardsCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardCards(CancellationToken ct)
    {
        var result = await _service.GetDashboardCardsDataAsync(ct);
        return Ok(new ApiResponse<RTSAppealDashboardCardsCountDto>
        {
            Success = true,
            Message = "Dashboard Cards Retrieved Successfully",
            Items = result
        });
    }

    [HttpGet]
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RTSAppealDashboardDetailsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardGrid([FromQuery] RTSAppealQueryParameters query, CancellationToken ct)
    {
        var result = await _service.GetAllDashboardAppealsAsync(query, ct);
        return Ok(new ApiResponse<PagedResult<RTSAppealDashboardDetailsDto>>
        {
            Success = true,
            Message = "Appeal Details Retrieved Successfully",
            Items = result
        });
    }

    [HttpGet("{appealId}/details")]
    [HttpGet("{appealId}/process-details")]
    [ProducesResponseType(typeof(ApiResponse<RTSAppealProcessDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProcessDetails(int appealId, CancellationToken ct)
    {
        var result = await _service.GetAppealProcessDetailsAsync(appealId, ct);
        if (result == null)
        {
            return NotFound(new ApiResponse<RTSAppealProcessDetailsDto>
            {
                Success = false,
                Message = $"Appeal #{appealId} not found"
            });
        }

        return Ok(new ApiResponse<RTSAppealProcessDetailsDto>
        {
            Success = true,
            Message = "Appeal process details retrieved successfully",
            Items = result
        });
    }

    [HttpPost("process-action")]
    [HttpPost("process-appeal")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessAction([FromBody] ProcessRTSAppealActionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid request validation data",
                Errors = errors
            });
        }

        try
        {
            var result = await _service.ProcessAppealOfficerActionAsync(dto, ct);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Appeal processed successfully",
                Items = new { success = result }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error during appeal processing: {Message}", ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to process appeal: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid state transition during appeal processing: {Message}", ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing appeal action");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An unexpected error occurred while processing the appeal action."
            });
        }
    }
}
