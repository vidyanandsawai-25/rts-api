using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Floor Details Old API — thin HTTP adapter.
/// Business logic lives in <c>PropertyOldDetailsService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves historical floor details for a property (PropertyDetailsOld table records).
    /// </summary>
    /// <response code="200">Returns the property historical floor details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/floor-details-old")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDetailsOldListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFloorDetailsOld(int propertyId, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.GetFloorDetailsOldAsync(propertyId, ct);

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

    /// <summary>
    /// Retrieves paginated historical floor details for a property (PropertyDetailsOld table records).
    /// </summary>
    /// <response code="200">Returns the paginated property historical floor details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/floor-details-old/paged")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PropertyDetailsOldDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFloorDetailsOldPaged(int propertyId, [FromQuery] FloorDetailsOldQueryParameters queryParameters, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.GetFloorDetailsOldPagedAsync(propertyId, queryParameters, ct);

        if (result == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
            return NotFound(new ApiResponse<PagedResult<PropertyDetailsOldDto>>
            {
                Success = false,
                Message = $"Property with ID {propertyId} not found"
            });
        }

        return Ok(new ApiResponse<PagedResult<PropertyDetailsOldDto>>
        {
            Success = true,
            Message = "Records fetched successfully",
            Items = result
        });
    }

    /// <summary>
    /// Retrieves a single historical floor detail record by ID.
    /// </summary>
    /// <response code="200">Returns the floor record</response>
    /// <response code="404">Property or floor not found</response>
    [HttpGet("{propertyId}/floor-details-old/{floorId}")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDetailsOldDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFloorDetailsOldById(int propertyId, int floorId, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.GetFloorDetailsOldByIdAsync(propertyId, floorId, ct);

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

    /// <summary>
    /// Adds a new historical floor detail record for a property (PropertyDetailsOld table).
    /// </summary>
    /// <response code="201">Floor record created successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data — validation error</response>
    [HttpPost("{propertyId}/floor-details-old")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDetailsOldDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddFloorDetailsOld(int propertyId, [FromBody] AddPropertyDetailsOldDto dto, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.AddFloorDetailsOldAsync(propertyId, dto, ct);

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

    /// <summary>
    /// Updates an existing historical floor detail record for a property (PropertyDetailsOld table).
    /// </summary>
    /// <response code="200">Floor record updated successfully</response>
    /// <response code="404">Property or floor record not found</response>
    /// <response code="400">Invalid data — validation error</response>
    [HttpPut("{propertyId}/floor-details-old/{floorId}")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDetailsOldDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateFloorDetailsOld(int propertyId, int floorId, [FromBody] UpdatePropertyDetailsOldDto dto, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.UpdateFloorDetailsOldAsync(propertyId, floorId, dto, ct);

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

    /// <summary>
    /// Deletes a historical floor detail record for a property (soft delete — sets MarkedForDeletion flag).
    /// </summary>
    /// <response code="200">Floor record deleted successfully</response>
    /// <response code="404">Property or floor record not found</response>
    [HttpDelete("{propertyId}/floor-details-old/{floorId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFloorDetailsOld(int propertyId, int floorId, CancellationToken ct)
    {
        var deleted = await _propertyOldDetailsService.DeleteFloorDetailsOldAsync(propertyId, floorId, ct);

        if (!deleted)
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
}
