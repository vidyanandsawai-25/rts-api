using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculation;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers.RetrospectiveTax;

[ApiController]
[Route("api/[controller]")]
public class RetrospectiveTaxCalculationController : ControllerBase
{
    private readonly IRetrospectiveTaxCalculationService _service;
    private readonly IRetrospectiveTaxCalculationEngineService _engineService;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<RetrospectiveTaxCalculationController> _logger;

    public RetrospectiveTaxCalculationController(
        IRetrospectiveTaxCalculationService service,
        IRetrospectiveTaxCalculationEngineService engineService,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<RetrospectiveTaxCalculationController> logger)
    {
        _service = service;
        _engineService = engineService;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RetrospectiveTaxCalculationQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// "Calculate Retrospective Tax" action for a property: reads the Retrospective Rule
    /// Builder's active/published rules, matches the one whose evidence/date conditions fit this
    /// property's actual OC/CC/Electricity/Change Detection certificates, and calculates + saves
    /// the year-wise retrospective tax breakup per that matched rule. Returns 404 if no rule
    /// (including no fallback rule) matches this property's evidence.
    /// </summary>
    [Authorize]
    [HttpPost("calculate/{propertyId}")]
    [ProducesResponseType(typeof(ApiResponse<RetrospectiveTaxEngineResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calculate(int propertyId, CancellationToken ct)
    {
        var result = await _engineService.CalculateAndSaveAsync(propertyId, GetCurrentUserId(), ct);
        if (result is null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No active retrospective rule matched PropertyId={propertyId}'s evidence."
            });
        }

        return Ok(new ApiResponse<RetrospectiveTaxEngineResultDto> { Success = true, Items = result });
    }

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRetrospectiveTaxCalculationDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRetrospectiveTaxCalculationDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRetrospectiveTaxCalculationDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRetrospectiveTaxCalculationDto>[] items, CancellationToken ct)
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
        => this.ExecuteForceDelete<RetrospectiveTaxCalculationEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<RetrospectiveTaxCalculationEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
        {
            throw new UnauthorizedAccessException("Valid user identification is required.");
        }
        return id;
    }
}
