using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.Constants.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Api.Controllers.RetrospectiveTax;

[ApiController]
[Route("api/[controller]")]
public class RetrospectivePenaltyRuleController : ControllerBase
{
    private readonly IRetrospectivePenaltyRuleService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<RetrospectivePenaltyRuleController> _logger;

    public RetrospectivePenaltyRuleController(
        IRetrospectivePenaltyRuleService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<RetrospectivePenaltyRuleController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RetrospectivePenaltyRuleQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Dropdown options for the "Penalty rule" field. Send the returned Code back in
    /// CreateRetrospectivePenaltyRuleDto.PenaltyMode / Update.../PenaltyMode. RequiredInput tells
    /// the UI which extra field(s) to show ("OPTIONAL_PERCENT" -> optional PenaltyPercent /
    /// "DATE_CONDITION" -> the nested date-condition builder, see penalty-date-source-types and
    /// penalty-date-conditions below / "NONE" -> nothing extra). This whole "Unauthorized
    /// Construction Penalty" section should only be shown when both OC and CC evidence are
    /// UNAVAILABLE for the rule (check GET
    /// api/RetrospectiveRuleEvidenceCondition/rule/{ruleId}/evidence-state). Static list (not a
    /// DB-backed lookup table) mirroring RetrospectivePenaltyRuleEntity.PenaltyMode.
    /// </summary>
    [HttpGet("penalty-modes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RetrospectivePenaltyRuleOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPenaltyModes()
        => Ok(new ApiResponse<IReadOnlyList<RetrospectivePenaltyRuleOptionDto>>
        {
            Success = true,
            Items = RetrospectivePenaltyRuleOptions.PenaltyModes
        });

    /// <summary>
    /// Dropdown options for "which date to check", shown only when PenaltyMode = DATE_VALIDATION.
    /// Send the returned Code back in PenaltyDateSourceType. RequiredInput: "EVIDENCE_TYPE" ->
    /// show the evidence-type picker bound to PenaltyDateEvidenceTypeId (options from
    /// GET api/EvidenceTypeMaster) / "COMPARE_DATE" -> show a date picker bound to CompareDate /
    /// "NONE" -> nothing extra (uses the calculation's own assessment date).
    /// </summary>
    [HttpGet("penalty-date-source-types")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RetrospectivePenaltyRuleOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPenaltyDateSourceTypes()
        => Ok(new ApiResponse<IReadOnlyList<RetrospectivePenaltyRuleOptionDto>>
        {
            Success = true,
            Items = RetrospectivePenaltyRuleOptions.PenaltyDateSourceTypes
        });

    /// <summary>
    /// Dropdown options for "how to compare the date", shown only when PenaltyMode =
    /// DATE_VALIDATION. Send the returned Code back in PenaltyDateCondition. RequiredInput:
    /// "COMPARE_DATE" -> show a single date picker bound to CompareDate / "COMPARE_DATE_RANGE" ->
    /// show two date pickers bound to CompareDate (from) and CompareDateTo (to).
    /// </summary>
    [HttpGet("penalty-date-conditions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RetrospectivePenaltyRuleOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPenaltyDateConditions()
        => Ok(new ApiResponse<IReadOnlyList<RetrospectivePenaltyRuleOptionDto>>
        {
            Success = true,
            Items = RetrospectivePenaltyRuleOptions.PenaltyDateConditions
        });

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRetrospectivePenaltyRuleDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRetrospectivePenaltyRuleDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRetrospectivePenaltyRuleDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRetrospectivePenaltyRuleDto>[] items, CancellationToken ct)
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
        => this.ExecuteForceDelete<RetrospectivePenaltyRuleEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<RetrospectivePenaltyRuleEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
