using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RuleLibrary;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.RetrospectiveTax;

/// <summary>
/// "Corporation Rule Library" grid — read-only composite view over the rule engine tables.
/// Row-level edits go through each table's own CRUD controller (RetrospectiveRuleMaster,
/// RetrospectiveRuleAction, RetrospectivePenaltyRule, etc.); this controller only reads.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RuleLibraryController : ControllerBase
{
    private readonly IRuleLibraryService _service;
    private readonly ILogger<RuleLibraryController> _logger;

    public RuleLibraryController(IRuleLibraryService service, ILogger<RuleLibraryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Returns the Rule Library grid: a shared "Common Taxation" badge (from the single active
    /// RetrospectiveTaxPolicy) plus a paged, filterable/searchable/sortable list of rows — one per
    /// RetrospectiveRuleMaster row — with the RULE, CONDITION, START LOGIC, and UNAUTHORIZED
    /// PENALTY columns already composed into display strings. STATUS is RuleStatus itself (color
    /// the dot client-side: Active = green, Review = amber, NeedsClarification = red, Draft =
    /// gray). ACTIONS need no new API: "View" -> GET api/RetrospectiveRuleMaster/{id} (plus
    /// GET api/RetrospectiveRuleSummary/rule/{id} for the detail panel), "Edit" -> the normal
    /// GET/PUT endpoints on each sub-table for that rule.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<RuleLibraryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLibrary([FromQuery] RuleLibraryQueryParameters queryParameters, CancellationToken ct)
    {
        try
        {
            var library = await _service.GetLibraryAsync(queryParameters, ct);
            return Ok(new ApiResponse<RuleLibraryDto> { Success = true, Items = library });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load rule library");
            return StatusCode(500, new ApiResponse<RuleLibraryDto>
            {
                Success = false,
                Message = "An error occurred while loading the rule library"
            });
        }
    }
}
