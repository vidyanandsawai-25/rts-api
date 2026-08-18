using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.Constants.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Api.Controllers.RetrospectiveTax;

[ApiController]
[Route("api/[controller]")]
public class RetrospectiveRuleActionController : ControllerBase
{
    private readonly IRetrospectiveRuleActionService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<RetrospectiveRuleActionController> _logger;

    public RetrospectiveRuleActionController(
        IRetrospectiveRuleActionService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<RetrospectiveRuleActionController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RetrospectiveRuleActionQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Dropdown options for the "Tax starts from" field. Send the returned Code back in
    /// CreateRetrospectiveRuleActionDto.TaxStartMode / Update.../TaxStartMode. RequiredInput
    /// tells the UI which extra field(s) to show for the selected option ("EVIDENCE_TYPE" ->
    /// StartEvidenceTypeId / "EVIDENCE_TYPE_AND_MONTHS" -> StartEvidenceTypeId + OffsetMonths /
    /// "CUTOFF_DATE" -> CutoffDate / "NONE" -> nothing extra). Static list (not a DB-backed
    /// lookup table) mirroring RetrospectiveRuleActionEntity.TaxStartMode.
    /// </summary>
    [HttpGet("tax-start-modes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RetrospectiveRuleActionOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetTaxStartModes()
        => Ok(new ApiResponse<IReadOnlyList<RetrospectiveRuleActionOptionDto>>
        {
            Success = true,
            Items = RetrospectiveRuleActionOptions.TaxStartModes
        });

    /// <summary>
    /// Dropdown options for the "Use date" field. Send the returned EvidenceTypeId back as
    /// StartEvidenceTypeId; when the selected option has IsCutoffDate = true, send
    /// StartEvidenceTypeId = null instead and set TaxStartMode to "FIXED_CUTOFF". DB-driven
    /// (queries EvidenceTypeMaster), so it always reflects the current active evidence types.
    /// </summary>
    [HttpGet("use-date-options")]
    [ProducesResponseType(typeof(ApiResponse<List<RetrospectiveRuleActionUseDateOptionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUseDateOptions(CancellationToken ct)
    {
        var options = await _service.GetUseDateOptionsAsync(ct);
        return Ok(new ApiResponse<List<RetrospectiveRuleActionUseDateOptionDto>> { Success = true, Items = options });
    }

    /// <summary>
    /// Dropdown options for the "Retrospective limit" field. Send the returned Code back in
    /// CreateRetrospectiveRuleActionDto.RetrospectiveLimitType / Update.../RetrospectiveLimitType.
    /// RequiredInput tells the UI which extra field to show ("MAXIMUM_YEARS" -> MaximumYears /
    /// "CUTOFF_DATE" -> CutoffDate / "NONE" -> nothing extra). Static list (not a DB-backed lookup
    /// table) mirroring RetrospectiveRuleActionEntity.RetrospectiveLimitType.
    /// </summary>
    [HttpGet("retrospective-limit-types")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RetrospectiveRuleActionOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetRetrospectiveLimitTypes()
        => Ok(new ApiResponse<IReadOnlyList<RetrospectiveRuleActionOptionDto>>
        {
            Success = true,
            Items = RetrospectiveRuleActionOptions.RetrospectiveLimitTypes
        });

    /// <summary>
    /// Dropdown options for the "Tax calculation" field. Send the returned Code back in
    /// CreateRetrospectiveRuleActionDto.TaxCalculationMode / Update.../TaxCalculationMode.
    /// RequiredInput tells the UI which extra field(s) to show ("SINGLE_MULTIPLIER" ->
    /// TaxMultiplier / "SPLIT_MULTIPLIER" -> SplitStartEvidenceTypeId, SplitEndEvidenceTypeId,
    /// SplitMultiplier, AfterSplitMultiplier). Static list (not a DB-backed lookup table)
    /// mirroring RetrospectiveRuleActionEntity.TaxCalculationMode.
    /// </summary>
    [HttpGet("tax-calculation-modes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RetrospectiveRuleActionOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetTaxCalculationModes()
        => Ok(new ApiResponse<IReadOnlyList<RetrospectiveRuleActionOptionDto>>
        {
            Success = true,
            Items = RetrospectiveRuleActionOptions.TaxCalculationModes
        });

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRetrospectiveRuleActionDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRetrospectiveRuleActionDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRetrospectiveRuleActionDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRetrospectiveRuleActionDto>[] items, CancellationToken ct)
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
        => this.ExecuteForceDelete<RetrospectiveRuleActionEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<RetrospectiveRuleActionEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
