using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.CertificateTaxGuideline;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CertificateTaxGuidelineController : ControllerBase
{
    private readonly ICertificateTaxGuidelineService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<CertificateTaxGuidelineController> _logger;

    public CertificateTaxGuidelineController(
        ICertificateTaxGuidelineService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<CertificateTaxGuidelineController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    // ─── Standard CRUD ──────────────────────────────────────────────────────

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] CertificateTaxGuidelineQueryParameters queryParameters, CancellationToken ct)
        =>this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateCertificateTaxGuidelineDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Updates a single certificate tax guideline row by its primary key Id.
    /// Use <c>PUT /bulk</c> when saving the full list from the UI form.
    /// </summary>
    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateCertificateTaxGuidelineDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Bulk-upserts a list of certificate tax guideline rows in a single transaction.
    /// Each row is matched by <c>GuidelineCode</c>; existing rows are updated,
    /// missing rows are created. This is the correct endpoint for the UI settings form.
    /// </summary>
    [HttpPut("bulk")]
    public async Task<IActionResult> BulkUpdate(
        [FromBody] List<UpdateCertificateTaxGuidelineDto> items,
        CancellationToken ct)
    {
        if (items == null || items.Count == 0)
            return BadRequest(new ApiResponse<IReadOnlyList<CertificateTaxGuidelineDto>>
            {
                Success = false,
                Message = "Request body must contain at least one guideline item."
            });

        try
        {
            var result = await _service.BulkUpsertAsync(items, ct);
            return Ok(new ApiResponse<IReadOnlyList<CertificateTaxGuidelineDto>>
            {
                Success = true,
                Message = $"Successfully upserted {result.Count} guideline(s).",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk upsert of {Count} guideline(s)", items.Count);
            return StatusCode(500, new ApiResponse<IReadOnlyList<CertificateTaxGuidelineDto>>
            {
                Success = false,
                Message = "An error occurred while saving the guidelines."
            });
        }
    }

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [HttpDelete("{id:int}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<CertificateTaxGuidelineEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    // ─── Guideline Value Lookup ──────────────────────────────────────────────

    /// <summary>Returns the typed value for a single guideline by its code.</summary>
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetGuidelineValue(string code, CancellationToken ct)
    {
        try
        {
            var value = await _service.GetGuidelineValueAsync(code, ct);
            return Ok(new { Code = code, Value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving guideline value for code {Code}", code);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>Returns all active guideline values for a given group, ordered by DisplayOrder.</summary>
    [HttpGet("group/{group}")]
    public async Task<IActionResult> GetGuidelineValuesByGroup(string group, CancellationToken ct)
    {
        try
        {
            var values = await _service.GetGuidelineValuesByGroupAsync(group, ct);
            return Ok(values);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving guideline values for group {Group}", group);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }
}
