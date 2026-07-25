using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.PropertyReassessment;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Serves the read-only "Property Re-Assessment" screen with support for complex property mapping scenarios.
///
/// For a single new (current) property, resolves all mapped old properties via PropertyMapMaster/PropertyMapDetail
/// and returns old-vs-new photos, floor details, and tax-head summary.
///
/// Supports all mapping categories:
/// - ONE_TO_ONE: 1 old property ↔ 1 new property
/// - SPLIT: 1 old property → multiple new properties (sibling new properties visible)
/// - MERGE: multiple old properties → 1 new property (old data aggregated)
/// - MAP: general/manual mappings (0, 1, or many old properties)
///
/// Clean-architecture replacement for the legacy single-property re-assessment SQL script.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertyReassessmentController : ControllerBase
{
    private readonly IPropertyReassessmentService _service;
    private readonly ILogger<PropertyReassessmentController> _logger;
    private readonly IWebHostEnvironment _environment;

    public PropertyReassessmentController(
        IPropertyReassessmentService service,
        ILogger<PropertyReassessmentController> logger,
        IWebHostEnvironment environment)
    {
        _service = service;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// GET api/PropertyReassessment
    ///
    /// Retrieve complete property re-assessment data (old vs new) for a single new property.
    /// The search always starts with a new (current) property identified by Ward + PropertyNo (+ optional PartitionNo).
    /// Old properties are discovered via PropertyMapMaster/PropertyMapDetail mappings.
    ///
    /// Response includes:
    /// - PropertyId: the new (current) property ID
    /// - OldPropertyIds: list of mapped old property IDs (0 to many depending on mapping category)
    /// - SiblingNewPropertyIds: other new properties in the same mapping group (populated for SPLIT scenarios)
    /// - Mappings: the full mapping group details for client context (includes mapping category, tax/area share %, status)
    /// - Photos: latest new and old plan/property photos
    /// - NewFloorDetails: current survey floor-wise data
    /// - OldFloorDetails: historical floor-wise data from all mapped old properties (each row tagged with PropertyIdOld)
    /// - TaxSummary: per-tax-head old vs new amounts (old aggregated across all mapped properties)
    /// </summary>
    /// <param name="query">Search parameters: WardId (required), PropertyNo (required), PartitionNo (optional)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>PropertyReassessmentDto with all old-vs-new comparison data</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PropertyReassessmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromQuery] PropertyReassessmentQueryParameters query, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.GetReassessmentAsync(query, ct);
            return Ok(new ApiResponse<PropertyReassessmentDto>
            {
                Success = true,
                Message = "Property re-assessment details retrieved.",
                Items = result
            });
        }
        catch (ArgumentException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Property re-assessment lookup rejected. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, CorrelationId = correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error fetching property re-assessment details. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = _environment.IsDevelopment() ? $"An error occurred: {ex.Message}" : "An error occurred",
                CorrelationId = correlationId
            });
        }
    }
}
