using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Basic Details Tab API - Partial controller for segregated property endpoints
/// Handles the `{propertyId}/basic-details` API endpoint which loads tab-specific data
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves basic details for a specific property including joined data from related tables.
    /// This endpoint is used to populate the Basic Details tab in the property form.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Property basic details including ward, zone, tax zone, category, and assessment information</returns>
    /// <response code="200">Returns the property basic details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/basic-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyBasicDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBasicDetails(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetBasicDetailsAsync(propertyId, ct);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving basic details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyBasicDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property basic details"
                });
        }
    }

    /// <summary>
    /// Updates basic details for a specific property across multiple tables.
    /// This endpoint is used to save the Basic Details tab in the property form.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="dto">The update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Property basic details updated successfully</response>
    /// <response code="404">Property not found</response>
    [HttpPut("{propertyId}/basic-details")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBasicDetails(int propertyId, [FromBody] UpdatePropertyBasicDetailsDto dto, CancellationToken ct)
    {
        try
        {
            var success = await _propertyService.UpdateBasicDetailsAsync(propertyId, dto, ct);

            if (!success)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found for update", propertyId);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating basic details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating property basic details"
                });
        }
    }
}
