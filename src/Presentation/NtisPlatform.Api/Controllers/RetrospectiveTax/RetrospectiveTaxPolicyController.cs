using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.Constants.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Api.Controllers.RetrospectiveTax;

[ApiController]
[Route("api/[controller]")]
public class RetrospectiveTaxPolicyController : ControllerBase
{
    private readonly IRetrospectiveTaxPolicyService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<RetrospectiveTaxPolicyController> _logger;

    public RetrospectiveTaxPolicyController(
        IRetrospectiveTaxPolicyService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<RetrospectiveTaxPolicyController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RetrospectiveTaxPolicyQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Dropdown options for the "Taxation rate" field. Send the returned Code back in
    /// CreateRetrospectiveTaxPolicyDto.RateMode / UpdateRetrospectiveTaxPolicyDto.RateMode;
    /// show Label to the user. Static list (not a DB-backed lookup table) mirroring the
    /// CK_RetrospectiveTaxPolicy_RateMode constraint.
    /// </summary>
    [HttpGet("rate-modes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RetrospectiveTaxPolicyOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetRateModes()
        => Ok(new ApiResponse<IReadOnlyList<RetrospectiveTaxPolicyOptionDto>>
        {
            Success = true,
            Items = RetrospectiveTaxPolicyOptions.RateModes
        });

    /// <summary>
    /// Dropdown options for the "Taxation percentage" field. Send the returned Code back in
    /// CreateRetrospectiveTaxPolicyDto.PercentageMode / UpdateRetrospectiveTaxPolicyDto.PercentageMode;
    /// show Label to the user. When Code is "FIXED_PERCENTAGE", the UI must also collect
    /// FixedPercentage (required by CK_RetrospectiveTaxPolicy_FixedPercentage).
    /// </summary>
    [HttpGet("percentage-modes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RetrospectiveTaxPolicyOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPercentageModes()
        => Ok(new ApiResponse<IReadOnlyList<RetrospectiveTaxPolicyOptionDto>>
        {
            Success = true,
            Items = RetrospectiveTaxPolicyOptions.PercentageModes
        });

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRetrospectiveTaxPolicyDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPost("Range")]
    public Task<IActionResult> CreateFromRange([FromBody] RangeCreateRequest<CreateRetrospectiveTaxPolicyDto> request, CancellationToken ct)
        => this.ExecuteCreateFromRange(_service, request, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRetrospectiveTaxPolicyDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRetrospectiveTaxPolicyDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRetrospectiveTaxPolicyDto>[] items, CancellationToken ct)
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
        => this.ExecuteForceDelete<RetrospectiveTaxPolicyEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<RetrospectiveTaxPolicyEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
