using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Api.Controllers.RetrospectiveTax;

[ApiController]
[Route("api/[controller]")]
public class RetrospectiveRuleSummaryController : ControllerBase
{
    private readonly IRetrospectiveRuleSummaryService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<RetrospectiveRuleSummaryController> _logger;

    public RetrospectiveRuleSummaryController(
        IRetrospectiveRuleSummaryService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<RetrospectiveRuleSummaryController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RetrospectiveRuleSummaryQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Everything the "Rule Summary" screen needs for one rule, in a single call: RuleCode (for
    /// the badge, e.g. "THA-01") plus the When/Tax/Penalty summary lines. Returns 404 if the rule
    /// doesn't exist. WhenSummary/TaxSummary/PenaltySummary come back null if the rule has no
    /// active summary row yet (e.g. it hasn't been generated/published) — show a placeholder
    /// rather than blank text in that case.
    /// </summary>
    [HttpGet("rule/{ruleId}")]
    [ProducesResponseType(typeof(ApiResponse<RetrospectiveRuleSummaryViewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForRule(int ruleId, CancellationToken ct)
    {
        var summary = await _service.GetForRuleAsync(ruleId, ct);
        if (summary is null)
        {
            return NotFound(new ApiResponse<RetrospectiveRuleSummaryViewDto> { Success = false, Message = $"Rule {ruleId} not found" });
        }

        return Ok(new ApiResponse<RetrospectiveRuleSummaryViewDto> { Success = true, Items = summary });
    }

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRetrospectiveRuleSummaryDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRetrospectiveRuleSummaryDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRetrospectiveRuleSummaryDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRetrospectiveRuleSummaryDto>[] items, CancellationToken ct)
        => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [HttpDelete("Bulk")]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_service, ids, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<RetrospectiveRuleSummaryEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<RetrospectiveRuleSummaryEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
