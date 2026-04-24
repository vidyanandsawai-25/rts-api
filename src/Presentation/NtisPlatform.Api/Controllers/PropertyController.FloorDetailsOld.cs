using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Floor Details Old API - Partial controller for segregated property endpoints
/// Handles the `{propertyId}/floor-details-old` API endpoint which loads historical floor information
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves historical floor details for a property (PropertyDetailsOld table records).
    /// This endpoint is used to populate the Historical Floor Information section in the Old Details tab.
    /// Returns a list of floor records with descriptive values from related master tables where available,
    /// while construction year and assessment year values are sourced directly from PropertyDetailsOld.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of historical floor details with related descriptive fields and year values from PropertyDetailsOld</returns>
    /// <response code="200">Returns the property historical floor details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/floor-details-old")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDetailsOldListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFloorDetailsOld(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetFloorDetailsOldAsync(propertyId, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
                return NotFound(new ApiResponse<PropertyDetailsOldListDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<PropertyDetailsOldListDto>
            {
                Success = true,
                Message = "Records fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving floor details old for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyDetailsOldListDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property floor details old"
                });
        }
    }

    /// <summary>
    /// Updates historical floor details for a property (PropertyDetailsOld table records).
    /// This endpoint is used to save the Historical Floor Information section in the Old Details tab.
    /// Supports automatic UPSERT logic:
    /// - If Id is 0 or null, a new record will be created (INSERT)
    /// - If Id > 0, the existing record will be updated (UPDATE)
    /// - Records not included in the list will be soft-deleted automatically
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="dto">The update data containing floor records to upsert</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success response with updated data</returns>
    /// <response code="200">Property floor details old updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data - Validation error</response>
    [HttpPut("{propertyId}/floor-details-old")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDetailsOldListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateFloorDetailsOld(int propertyId, [FromBody] UpdatePropertyDetailsOldListDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.UpdateFloorDetailsOldAsync(propertyId, dto, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found for update", propertyId);
                return NotFound(new ApiResponse<PropertyDetailsOldListDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<PropertyDetailsOldListDto>
            {
                Success = true,
                Message = "Records updated successfully",
                Items = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating floor details old for property {PropertyId}", propertyId);
            return BadRequest(new ApiResponse<PropertyDetailsOldListDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating floor details old for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyDetailsOldListDto>
                {
                    Success = false,
                    Message = "An error occurred while updating property floor details old"
                });
        }
    }
}
