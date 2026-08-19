using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Api.Controllers.RetrospectiveTax;

[ApiController]
[Route("api/[controller]")]
public class RetrospectiveRuleMasterController : ControllerBase
{
    private readonly IRetrospectiveRuleMasterService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<RetrospectiveRuleMasterController> _logger;

    public RetrospectiveRuleMasterController(
        IRetrospectiveRuleMasterService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<RetrospectiveRuleMasterController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RetrospectiveRuleMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// "View" action for the Rule Library grid: the rule header plus every builder section
    /// (evidence conditions, date condition, action, penalty rule, summary) in a single call —
    /// avoids the UI having to call all 6 sub-table endpoints separately just to render one
    /// read-only detail panel. "Edit" reuses the same sections' own GET/PUT endpoints (this
    /// endpoint pre-fills the form; each section still saves through its own controller —
    /// RetrospectiveRuleEvidenceCondition, RetrospectiveRuleDateCondition,
    /// RetrospectiveRuleAction, RetrospectivePenaltyRule). Returns 404 if the rule doesn't exist.
    /// </summary>
    [HttpGet("{id}/detail")]
    [ProducesResponseType(typeof(ApiResponse<RetrospectiveRuleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var detail = await _service.GetDetailAsync(id, ct);
        if (detail is null)
        {
            return NotFound(new ApiResponse<RetrospectiveRuleDetailDto> { Success = false, Message = $"Rule {id} not found" });
        }

        return Ok(new ApiResponse<RetrospectiveRuleDetailDto> { Success = true, Items = detail });
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRetrospectiveRuleMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// "Save" button on the Rule Builder screen: rule name + evidence conditions + date
    /// condition + retrospective tax action + penalty rule, upserted together in one call. Pass
    /// Id = null in the body to create a new (Draft) rule, or an existing rule's Id to update it
    /// in place. This does not publish the rule (RuleStatus is left as Draft on create / unchanged
    /// on update) — use POST {id}/publish separately for that. Returns 404 if Id is set but no
    /// such rule exists.
    /// </summary>
    [HttpPost("save")]
    [ProducesResponseType(typeof(ApiResponse<RetrospectiveRuleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Save([FromBody] SaveRetrospectiveRuleDto request, CancellationToken ct)
    {
        var detail = await _service.SaveAsync(request, ct);
        if (detail is null)
        {
            return NotFound(new ApiResponse<RetrospectiveRuleDetailDto> { Success = false, Message = $"Rule {request.Id} not found" });
        }

        return Ok(new ApiResponse<RetrospectiveRuleDetailDto> { Success = true, Message = "Rule saved successfully", Items = detail });
    }

    [HttpPost("Range")]
    public Task<IActionResult> CreateFromRange([FromBody] RangeCreateRequest<CreateRetrospectiveRuleMasterDto> request, CancellationToken ct)
        => this.ExecuteCreateFromRange(_service, request, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRetrospectiveRuleMasterDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRetrospectiveRuleMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRetrospectiveRuleMasterDto>[] items, CancellationToken ct)
        => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    /// <summary>
    /// "Publish Rule" button. Moves RuleStatus from Draft/Review/NeedsClarification to Active and
    /// writes a PUBLISH row to RetrospectiveRuleAuditLog (viewable via
    /// GET api/RetrospectiveRuleAuditLog?ruleId={id}). Returns 404 if the rule doesn't exist, 400
    /// (via the standard validation-error response) if it's already Active.
    /// </summary>
    [HttpPost("{id}/publish")]
    [ProducesResponseType(typeof(ApiResponse<RetrospectiveRuleMasterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(int id, [FromBody] PublishRetrospectiveRuleDto request, CancellationToken ct)
    {
        var rule = await _service.PublishAsync(id, request, ct);
        if (rule is null)
        {
            return NotFound(new ApiResponse<RetrospectiveRuleMasterDto> { Success = false, Message = $"Rule {id} not found" });
        }

        return Ok(new ApiResponse<RetrospectiveRuleMasterDto> { Success = true, Message = "Rule published successfully", Items = rule });
    }

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [HttpDelete("Bulk")]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_service, ids, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<RetrospectiveRuleMasterEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<RetrospectiveRuleMasterEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
