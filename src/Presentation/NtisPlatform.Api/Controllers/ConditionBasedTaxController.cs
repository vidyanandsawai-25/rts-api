using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Condition-based tax configuration — priority-ordered condition rows (each a flat,
/// left-to-right AND/OR chain, no parentheses/precedence, FIXED/PERCENT result per row) for
/// the CONDITION_BASED calculation mode of the Dynamic Tax Register, authored entirely
/// within this screen (no dependency on the standalone Rule Engine feature).
/// <see cref="Evaluate"/> tests a tax's saved rows against a real property; it never
/// affects live tax billing. <see cref="DeleteRule"/> is a real, immediate SQL DELETE.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConditionBasedTaxController : ControllerBase
{
    private readonly ITaxConditionRuleService _service;
    private readonly ILogger<ConditionBasedTaxController> _logger;

    public ConditionBasedTaxController(
        ITaxConditionRuleService service,
        ILogger<ConditionBasedTaxController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("rules")]
    [ProducesResponseType(typeof(PagedResult<TaxConditionRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules(
        [FromQuery] int taxId,
        [FromQuery] int? ruleDefinitionId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.GetByTaxAsync(taxId, ruleDefinitionId, pageNumber, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error retrieving condition rules for tax {TaxId}", taxId))
        {
            throw;
        }
    }

    [HttpPost("rules/save")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveRules([FromBody] SaveTaxConditionRuleRequest request, CancellationToken cancellationToken)
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
        catch (Exception ex) when (LogUnhandled(ex, "Error saving condition rules for tax {TaxId}", request.TaxId))
        {
            throw;
        }
    }

    /// <summary>
    /// Permanently deletes one condition row as soon as the admin confirms in the UI — a real
    /// SQL DELETE, not deferred to the next "Save Configuration" (unlike the upsert-only
    /// <see cref="SaveRules"/>, which never removes rows omitted from a resend).
    /// </summary>
    [HttpDelete("rules/{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRule(int id, [FromQuery] int taxId, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteAsync(id, taxId, cancellationToken);
            return Ok(new ApiResponse<object> { Success = true, Message = "Condition rule deleted" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error deleting condition rule {Id} for tax {TaxId}", id, taxId))
        {
            throw;
        }
    }

    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(ApiResponse<EvaluateTaxConditionRuleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateTaxConditionRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.EvaluateAsync(request, cancellationToken);
            return Ok(new ApiResponse<EvaluateTaxConditionRuleResponseDto> { Success = true, Items = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex) when (LogUnhandled(ex, "Error evaluating condition rules for tax {TaxId}, property {PropertyId}", request.TaxId, request.PropertyId))
        {
            throw;
        }
    }

    /// <summary>
    /// Exception filter used as `catch (Exception ex) when (LogUnhandled(ex, "..."))` — logs with
    /// this action's context and always returns false, so the exception is never actually caught
    /// here and propagates to GlobalExceptionHandlerMiddleware, which classifies it correctly
    /// instead of every uncategorized exception in this controller flattening to a generic 500.
    /// The ArgumentException catches above stay local because they return an ApiResponse-shaped
    /// message the global handler can't produce (it hides the specific message in production).
    /// </summary>
    private bool LogUnhandled(Exception ex, string message, params object?[] args)
    {
        _logger.LogError(ex, message, args);
        return false;
    }
}
