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

    public RateController(IRateService service, IHardDeleteCleanupService cleanupService, ILogger<RateController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
    }

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

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRateDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRateDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<RateEntity, int>(_cleanupService, id, _logger, ct);

    /// <summary>
    /// Creates multiple records in a single Bulk.
    /// </summary>
    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRateDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    /// <summary>
    /// Updates multiple records in a single Bulk.
    /// </summary>
    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRateDto>[] items, CancellationToken ct)
        => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    /// <summary>
    /// Deletes multiple records by IDs.
    /// </summary>
    [HttpDelete("Bulk")]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_service, ids, _logger, ct);
}
