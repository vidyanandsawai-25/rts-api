using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Core Property Aggregate API - Provides property search and lookup functionality.
/// Used across multiple features (ApplyTaxes, BillGeneration, Reports, etc.).
/// </summary>
/// <remarks>
/// Unlike other simple master data controllers (e.g. BankMaster, Ward, Zone) that live under
/// Controllers/Master, the Property aggregate is a core, cross-cutting domain concept used
/// by multiple bounded contexts and workflows. For this reason, it is intentionally exposed
/// as a root-level API at route <c>/api/Property</c> rather than being grouped under the
/// Master controllers folder.
/// </remarks>
[ApiController]
[Route("api/[controller]")]

public partial class PropertyController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly ILogger<PropertyController> _logger;

    /// <summary>
    /// Constructor follows codebase convention: Service dependencies first, then infrastructure.
    /// </summary>
    public PropertyController(
        IPropertyService propertyService,
        ILogger<PropertyController> logger)
    {
        _propertyService = propertyService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyQueryParameters query, CancellationToken ct)
        => this.ExecuteGetAllPaged(_propertyService, query, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_propertyService, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_propertyService, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_propertyService, id, updateDto, _logger, ct);

    [Authorize]
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_propertyService, id, _logger, ct);

    /// <summary>
    /// Deletes multiple property records by their IDs with transactional consistency.
    /// Properties are soft-deleted by setting MarkedForDeletion=true and IsActive=false.
    /// </summary>
    /// <param name="ids">Array of property IDs to delete. Must not be null or empty.</param>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <returns>
    /// 200 OK with BulkResult containing success count and any errors,
    /// 400 Bad Request if ids array is null or empty,
    /// 500 Internal Server Error if a critical failure occurs
    /// </returns>
    /// <response code="200">Returns bulk delete result with success/failure details for each property</response>
    /// <response code="400">If the ids array is null, empty, or contains invalid values</response>
    /// <response code="500">If a critical error occurs during the deletion process</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /api/Property/Bulk
    ///     [1, 2, 3, 4, 5]
    ///     
    /// **Transaction Behavior:**
    /// - All database changes occur within a single database transaction
    /// - If any property passes validation and is deleted, all those successful deletions are committed together
    /// - If a critical error occurs (database error, system failure), ALL changes are rolled back
    /// 
    /// **Partial Success:**
    /// This endpoint supports partial success where individual properties may be skipped (not deleted) due to:
    /// - Property not found (404)
    /// - Property already deleted
    /// - Validation failures
    /// - Business rule violations
    /// 
    /// Successfully deleted properties are committed even if others fail validation.
    /// 
    /// **Related Data:**
    /// All related entities (PropertyDetails, PlotDetails, SocietyDetails, etc.) are also soft-deleted.
    /// </remarks>
    [Authorize]
    [HttpDelete("Bulk")]
    [ProducesResponseType(typeof(BulkResult<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_propertyService, ids, _logger, ct);
        
    [HttpPost("Range")]
    public async Task<IActionResult> CreateFromRange([FromBody] RangeCreateRequest<CreateNewPropertyDto> request, CancellationToken ct)
    {
        try 
        {
            var result = await _propertyService.CreatePropertiesFromRangeAsync(request, ct);    
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating properties from range");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Success = false,
                Message = "An unexpected error occurred while processing your request.",
            });
        }
    }
}
