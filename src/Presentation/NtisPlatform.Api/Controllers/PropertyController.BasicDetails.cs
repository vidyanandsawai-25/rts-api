using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Basic Details Tab API — thin HTTP adapter.
/// Business logic lives in <c>PropertyBasicDetailsService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves basic details for a specific property including joined data from related tables.
    /// </summary>
    /// <response code="200">Returns the property basic details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/basic-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyBasicDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBasicDetails(int propertyId, CancellationToken ct)
    {
        var result = await _propertyBasicDetailsService.GetBasicDetailsAsync(propertyId, ct);

        if (result == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
            return NotFound(new ApiResponse<PropertyBasicDetailsDto>
            {
                Success = false,
                Message = $"Property with ID {propertyId} not found"
            });
        }

        return Ok(new ApiResponse<PropertyBasicDetailsDto>
        {
            Success = true,
            Message = "Record fetched successfully",
            Items = result
        });
    }

    /// <summary>
    /// Updates basic details for a specific property across multiple tables.
    /// </summary>
    /// <response code="200">Property basic details updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data — FK constraint violation or validation error</response>
    [HttpPut("{propertyId}/basic-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyBasicDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateBasicDetails(int propertyId, [FromBody] UpdatePropertyBasicDetailsDto dto, CancellationToken ct)
    {
        var result = await _propertyBasicDetailsService.UpdateBasicDetailsAsync(propertyId, dto, ct);

        if (result == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found for update", propertyId);
            return NotFound(new ApiResponse<PropertyBasicDetailsDto>
            {
                Success = false,
                Message = $"Property with ID {propertyId} not found"
            });
        }

        return Ok(new ApiResponse<PropertyBasicDetailsDto>
        {
            Success = true,
            Message = "Record updated successfully",
            Items = result
        });
    }
}
