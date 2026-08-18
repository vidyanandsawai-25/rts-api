using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Api.Controllers.RetrospectiveTax;

[ApiController]
[Route("api/[controller]")]
public class RetrospectiveRuleEvidenceConditionController : ControllerBase
{
    private readonly IRetrospectiveRuleEvidenceConditionService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<RetrospectiveRuleEvidenceConditionController> _logger;

    public RetrospectiveRuleEvidenceConditionController(
        IRetrospectiveRuleEvidenceConditionService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<RetrospectiveRuleEvidenceConditionController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RetrospectiveRuleEvidenceConditionQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Everything the "Available evidence" / "Unavailable evidence" checkbox screen needs for one
    /// rule, in a single call: every active evidence type (OC, CC, Electricity, Change Detection,
    /// Construction Year — ordered for display), each tagged with its current selection.
    /// SelectedState is "AVAILABLE" (checked in the green panel), "UNAVAILABLE" (checked in the
    /// red panel), or null (unchecked in both).
    /// </summary>
    [HttpGet("rule/{ruleId}/evidence-state")]
    [ProducesResponseType(typeof(ApiResponse<List<RetrospectiveRuleEvidenceConditionStateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvidenceState(int ruleId, CancellationToken ct)
    {
        var state = await _service.GetEvidenceStateForRuleAsync(ruleId, ct);
        return Ok(new ApiResponse<List<RetrospectiveRuleEvidenceConditionStateDto>> { Success = true, Items = state });
    }

    /// <summary>
    /// Saves both checkbox panels for this rule in one call — pass the EvidenceTypeMaster.Id
    /// values checked in each panel; anything left out of both lists is unchecked in both panels.
    /// Returns the resulting state (same shape as GET) so the UI can refresh from the response
    /// without a second round trip.
    /// </summary>
    [HttpPut("rule/{ruleId}/evidence-state")]
    [ProducesResponseType(typeof(ApiResponse<List<RetrospectiveRuleEvidenceConditionStateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetEvidenceState(int ruleId, [FromBody] SetRetrospectiveRuleEvidenceConditionStateDto request, CancellationToken ct)
    {
        var state = await _service.SetEvidenceStateForRuleAsync(ruleId, request, ct);
        return Ok(new ApiResponse<List<RetrospectiveRuleEvidenceConditionStateDto>> { Success = true, Items = state });
    }

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRetrospectiveRuleEvidenceConditionDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRetrospectiveRuleEvidenceConditionDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRetrospectiveRuleEvidenceConditionDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRetrospectiveRuleEvidenceConditionDto>[] items, CancellationToken ct)
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
        => this.ExecuteForceDelete<RetrospectiveRuleEvidenceConditionEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<RetrospectiveRuleEvidenceConditionEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
