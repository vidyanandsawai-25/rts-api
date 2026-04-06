using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Old Details Tab API - Partial controller for segregated property endpoints
/// Handles the `{propertyId}/old-details` API endpoint which loads tab-specific historical data
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves old property details including joined data from PropertyMastOld and PropertyDetailsOld tables.
    /// This endpoint is used to populate the Old Details tab in the property form with historical property information.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Property old details including historical ward, property numbers, plot details, tax information, and construction details</returns>
    /// <response code="200">Returns the property old details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/old-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyOldDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOldDetails(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetOldDetailsAsync(propertyId, ct);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving old details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyOldDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property old details"
                });
        }
    }

    /// <summary>
    /// Updates old property details across PropertyMastOld and PropertyDetailsOld tables.
    /// This endpoint is used to save the Old Details tab in the property form with historical property information.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="dto">The update data containing historical property information</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success response with updated data</returns>
    /// <response code="200">Property old details updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data - Validation error</response>
    [HttpPut("{propertyId}/old-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyOldDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOldDetails(int propertyId, [FromBody] UpdatePropertyOldDetailsDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.UpdateOldDetailsAsync(propertyId, dto, ct);

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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating old details for property {PropertyId}", propertyId);
            return BadRequest(new ApiResponse<PropertyOldDetailsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating old details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyOldDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while updating property old details"
                });
        }
    }
}
