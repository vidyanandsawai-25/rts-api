using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Old Details Tab API — thin HTTP adapter.
/// Business logic lives in <c>PropertyOldDetailsService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves old property details including joined data from PropertyMastOld and PropertyDetailsOld tables.
    /// </summary>
    /// <response code="200">Returns the property old details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/old-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyOldDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOldDetails(int propertyId, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.GetOldDetailsAsync(propertyId, ct);

        if (result == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
            return NotFound(new ApiResponse<PropertyOldDetailsDto>
            {
                Success = false,
                Message = $"Property with ID {propertyId} not found"
            });
        }

        return Ok(new ApiResponse<PropertyOldDetailsDto>
        {
            Success = true,
            Message = "Record fetched successfully",
            Items = result
        });
    }

    /// <summary>
    /// Updates old property details across PropertyMastOld and PropertyDetailsOld tables.
    /// </summary>
    /// <response code="200">Property old details updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data — validation error</response>
    [HttpPut("{propertyId}/old-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyOldDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOldDetails(int propertyId, [FromBody] UpdatePropertyOldDetailsDto dto, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.UpdateOldDetailsAsync(propertyId, dto, ct);

        if (result == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found for update", propertyId);
            return NotFound(new ApiResponse<PropertyOldDetailsDto>
            {
                Success = false,
                Message = $"Property with ID {propertyId} not found"
            });
        }

        return Ok(new ApiResponse<PropertyOldDetailsDto>
        {
            Success = true,
            Message = "Record updated successfully",
            Items = result
        });
    }
}
