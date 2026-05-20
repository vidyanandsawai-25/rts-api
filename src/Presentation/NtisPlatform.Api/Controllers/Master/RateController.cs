using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]

public class RateController : ControllerBase
{
    private readonly IRateService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<RateController> _logger;
    private readonly IReferenceValidationService _referenceValidationService;

    public RateController(IRateService service, IHardDeleteCleanupService cleanupService, IReferenceValidationService referenceValidationService, ILogger<RateController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
        _referenceValidationService = referenceValidationService;
    }

    // Read operations
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RateQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedAll([FromQuery] RateQueryParameters queryParameters, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetDetailedAllAsync(queryParameters, ct);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FilterValidationException ex)
        {
            _logger.LogWarning(ex, "Filter validation failed: {Message}", ex.Message);
            return BadRequest(new
            {
                message = ex.Message,
                errors = ex.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetDetailedAll rates");
            return StatusCode(500, new { message = "An error occurred while fetching detailed rates" });
        }
    }

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    // Create operations
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRateDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);
    /// Creates multiple records in a single Bulk.
    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRateDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    // Update operations
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRateDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// Updates multiple records in a single Bulk.
    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRateDto>[] items, CancellationToken ct)
        => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    // Delete operations
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<RateEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    /// Deletes multiple records by IDs.
    [HttpDelete("Bulk")]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_service, ids, _logger, ct);

    /// Permanently deletes multiple records by IDs. This is an irreversible operation.
    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<RateEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
