using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Dynamic Tax Register — read-only grid, hero stats, and General-tab settings save.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DynamicTaxRegisterController : ControllerBase
{
    private readonly IDynamicTaxRegisterService _service;
    private readonly ILogger<DynamicTaxRegisterController> _logger;

    public DynamicTaxRegisterController(
        IDynamicTaxRegisterService service,
        ILogger<DynamicTaxRegisterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DynamicTaxRegisterRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRegister([FromQuery] DynamicTaxRegisterQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetRegisterAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error retrieving dynamic tax register"))
        {
            throw; // unreachable — the filter above never returns true; GlobalExceptionHandlerMiddleware handles it
        }
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(DynamicTaxRegisterStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetStatsAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error retrieving dynamic tax register stats"))
        {
            throw;
        }
    }

    [HttpGet("tax-categories")]
    [ProducesResponseType(typeof(List<TaxCategoryOptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaxCategories(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetTaxCategoriesAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error retrieving tax categories"))
        {
            throw;
        }
    }

    [HttpGet("config-overview")]
    [ProducesResponseType(typeof(ConfigOverviewPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfigOverview([FromQuery] ConfigOverviewQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetConfigOverviewAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error retrieving dynamic tax config overview"))
        {
            throw;
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTaxRegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var newTaxId = await _service.CreateAsync(request, cancellationToken);
            return Ok(new ApiResponse<int> { Success = true, Items = newTaxId, Message = "Tax created" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new ApiResponse<object> { Success = false, Message = $"A tax with code '{request.TaxCode}' already exists." });
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error creating tax"))
        {
            throw;
        }
    }

    /// <summary>
    /// Active calculation modes from PTIS.TaxCalculationModeMaster — the source for the Rule Type
    /// dropdown, replacing the previously hardcoded list. Each row carries its capability flags so
    /// the UI can decide which configuration tabs apply without branching on the mode's code.
    /// </summary>
    [HttpGet("calculation-modes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaxCalculationModeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCalculationModes(CancellationToken cancellationToken)
    {
        try
        {
            var modes = await _service.GetCalculationModesAsync(cancellationToken);
            return Ok(new ApiResponse<IReadOnlyList<TaxCalculationModeDto>> { Success = true, Items = modes });
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error retrieving calculation modes"))
        {
            throw;
        }
    }

    /// <summary>
    /// Configuration row counts for one tax — lets the UI name exactly what a CalculationMode
    /// change would delete before asking the admin to confirm it. Fetched on demand (at confirm
    /// time), so the numbers can never be stale.
    /// </summary>
    [HttpGet("{id}/config-summary")]
    [ProducesResponseType(typeof(ApiResponse<TaxConfigSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfigSummary(int id, CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _service.GetConfigSummaryAsync(id, cancellationToken);
            if (summary is null)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = $"Tax {id} not found" });
            }
            return Ok(new ApiResponse<TaxConfigSummaryDto> { Success = true, Items = summary });
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error retrieving config summary for tax {TaxId}", id))
        {
            throw;
        }
    }

    [HttpPut("{id}/settings")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSettings(int id, [FromBody] UpdateTaxRegisterSettingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.UpdateSettingsAsync(id, request, cancellationToken);
            if (!updated)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = $"Tax {id} not found" });
            }
            return Ok(new ApiResponse<object> { Success = true, Message = "Settings saved" });
        }
        catch (TaxModeChangeConflictException ex)
        {
            // Nothing was written or deleted — the caller must confirm the cleanup (or reload a
            // stale view) and retry.
            return Conflict(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error saving settings for tax {TaxId}", id))
        {
            throw;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627);

    /// <summary>
    /// Exception filter used as `catch (Exception ex) when (LogUnhandled(ex, "..."))` — logs with
    /// this action's context and always returns false, so the exception is never actually caught
    /// here and propagates to GlobalExceptionHandlerMiddleware, which classifies it correctly
    /// (ArgumentException → 400, KeyNotFoundException → 404, etc.) instead of every uncategorized
    /// exception in this controller flattening to a generic 500 the way a bare `catch (Exception)`
    /// would. Specific catches above (ArgumentException, DbUpdateException, TaxModeChangeConflictException)
    /// stay local because they return an ApiResponse-shaped message the global handler can't produce.
    /// </summary>
    private bool LogUnhandled(Exception ex, string message, params object?[] args)
    {
        _logger.LogError(ex, message, args);
        return false;
    }
}
