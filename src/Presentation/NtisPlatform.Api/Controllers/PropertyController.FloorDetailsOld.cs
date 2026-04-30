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
    /// Retrieves a single historical floor detail record by ID.
    /// This endpoint returns a specific floor record for use in CreatedAtAction location headers.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="floorId">The unique identifier of the floor record</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Single floor record with joined master data</returns>
    /// <response code="200">Returns the floor record</response>
    /// <response code="404">Property or floor not found</response>
    [HttpGet("{propertyId}/floor-details-old/{floorId}")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDetailsOldDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFloorDetailsOldById(int propertyId, int floorId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetFloorDetailsOldByIdAsync(propertyId, floorId, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} or Floor with ID {FloorId} not found", propertyId, floorId);
                return NotFound(new ApiResponse<PropertyDetailsOldDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} or Floor with ID {floorId} not found"
                });
            }

            return Ok(new ApiResponse<PropertyDetailsOldDto>
            {
                Success = true,
                Message = "Record fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving floor details old by ID for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyDetailsOldDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property floor details old"
                });
        }
    }

    /// <summary>
    /// Adds a new historical floor detail record for a property (PropertyDetailsOld table).
    /// This endpoint is used to add a new floor record in the Historical Floor Information section.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="dto">The floor data to add</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success response with the newly created floor record</returns>
    /// <response code="201">Floor record created successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data - Validation error</response>
    [HttpPost("{propertyId}/floor-details-old")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDetailsOldDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddFloorDetailsOld(int propertyId, [FromBody] AddPropertyDetailsOldDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.AddFloorDetailsOldAsync(propertyId, dto, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found for add", propertyId);
                return NotFound(new ApiResponse<PropertyDetailsOldDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return CreatedAtAction(
                nameof(GetFloorDetailsOldById),
                new { propertyId, floorId = result.Id },
                new ApiResponse<PropertyDetailsOldDto>
                {
                    Success = true,
                    Message = "Record added successfully",
                    Items = result
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error adding floor details old for property {PropertyId}", propertyId);
            return BadRequest(new ApiResponse<PropertyDetailsOldDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding floor details old for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyDetailsOldDto>
                {
                    Success = false,
                    Message = "An error occurred while adding property floor details old"
                });
        }
    }

    /// <summary>
    /// Updates an existing historical floor detail record for a property (PropertyDetailsOld table).
    /// This endpoint is used to update an existing floor record in the Historical Floor Information section.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="floorId">The unique identifier of the floor record to update</param>
    /// <param name="dto">The updated floor data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success response with updated data</returns>
    /// <response code="200">Floor record updated successfully</response>
    /// <response code="404">Property or floor record not found</response>
    /// <response code="400">Invalid data - Validation error</response>
    [HttpPut("{propertyId}/floor-details-old/{floorId}")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDetailsOldDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateFloorDetailsOld(int propertyId, int floorId, [FromBody] UpdatePropertyDetailsOldDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.UpdateFloorDetailsOldAsync(propertyId, floorId, dto, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} or Floor with ID {FloorId} not found for update", propertyId, floorId);
                return NotFound(new ApiResponse<PropertyDetailsOldDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} or Floor with ID {floorId} not found"
                });
            }

            return Ok(new ApiResponse<PropertyDetailsOldDto>
            {
                Success = true,
                Message = "Record updated successfully",
                Items = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating floor details old for property {PropertyId}", propertyId);
            return BadRequest(new ApiResponse<PropertyDetailsOldDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating floor details old for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyDetailsOldDto>
                {
                    Success = false,
                    Message = "An error occurred while updating property floor details old"
                });
        }
    }

    /// <summary>
    /// Deletes a historical floor detail record for a property (soft delete - sets MarkedForDeletion flag).
    /// This endpoint is used to delete a floor record in the Historical Floor Information section.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="floorId">The unique identifier of the floor record to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success response</returns>
    /// <response code="200">Floor record deleted successfully</response>
    /// <response code="404">Property or floor record not found</response>
    [HttpDelete("{propertyId}/floor-details-old/{floorId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFloorDetailsOld(int propertyId, int floorId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.DeleteFloorDetailsOldAsync(propertyId, floorId, ct);

            if (!result)
            {
                _logger.LogWarning("Property with ID {PropertyId} or Floor with ID {FloorId} not found for delete", propertyId, floorId);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} or Floor with ID {floorId} not found"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Record deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting floor details old for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting property floor details old"
                });
        }
    }
}
