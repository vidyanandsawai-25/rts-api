using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.PropertyReassessment;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Serves the read-only "Property Re-Assessment" screen: for a single property it returns the
/// old-vs-new photos, floor details and tax-head summary.
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

    // GET api/PropertyReassessment?wardId=&propertyNo=&partitionNo=
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PropertyReassessmentDto>), StatusCodes.Status200OK)]
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
