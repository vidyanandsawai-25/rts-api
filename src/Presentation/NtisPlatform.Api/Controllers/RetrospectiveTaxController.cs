using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RetrospectiveTax;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Serves the read-only "Retrospective Tax Details" screen: for a single property it returns the
/// year-wise pending tax amounts per tax head.
/// Clean-architecture replacement for the legacy dynamic-PIVOT retrospective tax SQL script.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RetrospectiveTaxController : ControllerBase
{
    private readonly IRetrospectiveTaxService _service;
    private readonly ILogger<RetrospectiveTaxController> _logger;
    private readonly IWebHostEnvironment _environment;

    public RetrospectiveTaxController(
        IRetrospectiveTaxService service,
        ILogger<RetrospectiveTaxController> logger,
        IWebHostEnvironment environment)
    {
        _service = service;
        _logger = logger;
        _environment = environment;
    }

    // GET api/RetrospectiveTax?wardId=&propertyNo=&partitionNo=
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<RetrospectiveTaxDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] RetrospectiveTaxQueryParameters query, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.GetRetrospectiveTaxAsync(query, ct);
            return Ok(new ApiResponse<RetrospectiveTaxDto>
            {
                Success = true,
                Message = "Retrospective tax details retrieved.",
                Items = result
            });
        }
        catch (ArgumentException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Retrospective tax lookup rejected. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, CorrelationId = correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error fetching retrospective tax details. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = _environment.IsDevelopment() ? $"An error occurred: {ex.Message}" : "An error occurred",
                CorrelationId = correlationId
            });
        }
    }
}
