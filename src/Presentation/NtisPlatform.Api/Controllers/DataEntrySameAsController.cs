using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.DataEntrySameAs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Makes one or more destination properties' data-entry the SAME AS a source property.
/// Clean-architecture replacement for the legacy [PTIS].[DataEntrySameAS] stored procedure.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataEntrySameAsController : ControllerBase
{
    private readonly IDataEntrySameAsService _service;
    private readonly ILogger<DataEntrySameAsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public DataEntrySameAsController(
        IDataEntrySameAsService service,
        ILogger<DataEntrySameAsController> logger,
        IWebHostEnvironment environment)
    {
        _service = service;
        _logger = logger;
        _environment = environment;
    }

    // GET api/DataEntrySameAs
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DataEntrySameAsPropertyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSiblings([FromQuery] DataEntrySameAsQueryParameters query, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.GetSiblingPropertiesAsync(query, ct);
            return Ok(new ApiResponse<List<DataEntrySameAsPropertyDto>>
            {
                Success = true,
                Message = $"{result.Count} property(ies) found.",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error fetching sibling properties. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = _environment.IsDevelopment() ? $"An error occurred: {ex.Message}" : "An error occurred",
                CorrelationId = correlationId
            });
        }
    }

    // POST api/DataEntrySameAs
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DataEntrySameAsResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Execute([FromBody] DataEntrySameAsRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetUserId();
            var result = await _service.ExecuteAsync(request, userId, ct);

            return Ok(new ApiResponse<DataEntrySameAsResultDto>
            {
                Success = true,
                Message = $"Data entry copied to {result.ProcessedDestinations} destination(s).",
                Items = result,
                Errors = result.Warnings.Count > 0 ? result.Warnings : null
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized access. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Valid user identification is required.", CorrelationId = correlationId });
        }
        catch (ArgumentException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Validation error during DataEntrySameAs. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message, CorrelationId = correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error during DataEntrySameAs. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = _environment.IsDevelopment() ? $"An error occurred: {ex.Message}" : "An error occurred",
                CorrelationId = correlationId
            });
        }
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
            throw new UnauthorizedAccessException("Valid user identification is required.");
        return id;
    }
}
