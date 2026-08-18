using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.Constants.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Api.Controllers.RetrospectiveTax;

[ApiController]
[Route("api/[controller]")]
public class RetrospectiveRuleDateConditionController : ControllerBase
{
    private readonly IRetrospectiveRuleDateConditionService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<RetrospectiveRuleDateConditionController> _logger;

    public RetrospectiveRuleDateConditionController(
        IRetrospectiveRuleDateConditionService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<RetrospectiveRuleDateConditionController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RetrospectiveRuleDateConditionQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Dropdown options for the "Compare evidence dates" field. Send the returned Code back in
    /// CreateRetrospectiveRuleDateConditionDto.ComparatorCode / Update.../ComparatorCode.
    /// RequiredInput tells the UI which extra field to show for the selected option
    /// ("NONE" / "CUTOFF_DATE" -> CompareDate / "YEARS" -> CompareYears). Static list (not a
    /// DB-backed lookup table) mirroring RetrospectiveRuleDateConditionEntity.ComparatorCode.
    /// </summary>
    [HttpGet("comparator-codes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RetrospectiveRuleDateConditionOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetComparatorCodes()
        => Ok(new ApiResponse<IReadOnlyList<RetrospectiveRuleDateConditionOptionDto>>
        {
            Success = true,
            Items = RetrospectiveRuleDateConditionOptions.ComparatorCodes
        });

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRetrospectiveRuleDateConditionDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRetrospectiveRuleDateConditionDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRetrospectiveRuleDateConditionDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRetrospectiveRuleDateConditionDto>[] items, CancellationToken ct)
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
        => this.ExecuteForceDelete<RetrospectiveRuleDateConditionEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<RetrospectiveRuleDateConditionEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
