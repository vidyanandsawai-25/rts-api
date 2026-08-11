using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.PropertyComparison;
using NtisPlatform.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertyComparisonController : ControllerBase
{
    private readonly IPropertyComparisonService _comparisonService;
    private readonly ILogger<PropertyComparisonController> _logger;

    public PropertyComparisonController(
        IPropertyComparisonService comparisonService,
        ILogger<PropertyComparisonController> logger)
    {
        _comparisonService = comparisonService;
        _logger = logger;
    }

    /// <summary>
    /// Compare old and new property data
    /// </summary>
    /// <param name="newPropertyId">The new property ID (old property ID is fetched from PropertyMapDetail)</param>
    /// <returns>Property comparison details with latest tax data</returns>
    [HttpGet("compare")]
    public async Task<ActionResult<PropertyComparisonDto>> CompareProperties(
        [FromQuery] int newPropertyId)
    {
        if (newPropertyId <= 0)
            return BadRequest("newPropertyId must be a positive integer.");

        try
        {
            var result = await _comparisonService.ComparePropertiesAsync(newPropertyId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Property comparison failed for NewPropertyId={NewPropertyId}", newPropertyId);
            return NotFound(new { error = "Property comparison could not be completed for the requested property." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error comparing properties: NewPropertyId={NewPropertyId}", newPropertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred while comparing properties." });
        }
    }
}
