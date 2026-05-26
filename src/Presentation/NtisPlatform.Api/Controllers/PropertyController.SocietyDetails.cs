using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Society Details Tab API - Partial controller for segregated property endpoints
/// Handles the `{propertyId}/society-details` API endpoint which loads tab-specific data
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves society details for a specific property including joined data from related tables.
    /// This endpoint is used to populate the Society Details tab in the property form.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Property society details including wing information and contact details</returns>
    /// <response code="200">Returns the property society details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/society-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertySocietyDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSocietyDetails(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetSocietyDetailsAsync(propertyId, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
                return NotFound(new ApiResponse<PropertySocietyDetailsDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<PropertySocietyDetailsDto>
            {
                Success = true,
                Message = "Record fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving society details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertySocietyDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property society details"
                });
        }
    }


    [HttpGet("{SocietyDetailId}/society-Aminity-details")]
    [ProducesResponseType(typeof(ApiResponse<List<SocietyAminityDetailsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSocietyAminityListAsync(int SocietyDetailId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetSocietyAminityListAsync(SocietyDetailId, ct);

            if (result == null)
            {
                _logger.LogWarning("Society with ID {SocietyDetailId} not found", SocietyDetailId);
                return NotFound(new ApiResponse<SocietyAminityDetailsDto>
                {
                    Success = false,
                    Message = $"Society with ID {SocietyDetailId} not found"
                });
            }

            return Ok(new ApiResponse<List<SocietyAminityDetailsDto>>
            {
                Success = true,
                Message = "Record fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving society details for society detail {SocietyDetailId}", SocietyDetailId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<SocietyAminityDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property society details"
                });
        }
    }


    [HttpGet("{propertyId}/society-Wing-details")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertySocietyDetailsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSocietyWingListAsync(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetSocietyWingListAsync(propertyId, ct);

            if (result == null)
            {
                _logger.LogWarning("property with ID {propertyId} not found", propertyId);
                return NotFound(new ApiResponse<PropertySocietyDetailsDto>
                {
                    Success = false,
                    Message = $"property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<List<PropertySocietyDetailsDto>>
            {
                Success = true,
                Message = "Record fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving society details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertySocietyDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property society details"
                });
        }
    }





    /// <summary>
    /// Updates society details for a specific property.
    /// This endpoint is used to save the Society Details tab in the property form.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="dto">The update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success response with updated data</returns>
    /// <response code="200">Property society details updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data - Foreign key constraint violation</response>
    [HttpPut("{propertyId}/society-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertySocietyDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSocietyDetails(int propertyId, [FromBody] UpdatePropertySocietyDetailsDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.UpdateSocietyDetailsAsync(propertyId, dto, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found for update", propertyId);
                return NotFound(new ApiResponse<PropertySocietyDetailsDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<PropertySocietyDetailsDto>
            {
                Success = true,
                Message = "Record updated successfully",
                Items = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating society details for property {PropertyId}", propertyId);
            return BadRequest(new ApiResponse<PropertySocietyDetailsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating society details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertySocietyDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while updating property society details"
                });
        }
    }
}
