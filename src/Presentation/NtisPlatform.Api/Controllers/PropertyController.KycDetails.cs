using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property KYC Details Tab API - Partial controller for segregated property endpoints
/// Handles the `{propertyId}/kyc-details` API endpoint which loads tab-specific data
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves KYC details for a specific property including joined data from related tables.
    /// This endpoint is used to populate the KYC Details tab in the property form.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Property KYC details including owner type, aadhar, owner/occupier, address, and contact information</returns>
    /// <response code="200">Returns the property KYC details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/kyc-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyKycDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKycDetails(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetKycDetailsAsync(propertyId, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
                return NotFound(new ApiResponse<PropertyKycDetailsDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<PropertyKycDetailsDto>
            {
                Success = true,
                Message = "Record fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KYC details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyKycDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property KYC details"
                });
        }
    }

    /// <summary>
    /// Updates KYC details for a specific property across multiple tables.
    /// This endpoint is used to save the KYC Details tab in the property form.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="dto">The update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success response with updated data</returns>
    /// <response code="200">Property KYC details updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data - Foreign key constraint violation</response>
    [HttpPut("{propertyId}/kyc-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyKycDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateKycDetails(int propertyId, [FromBody] UpdatePropertyKycDetailsDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.UpdateKycDetailsAsync(propertyId, dto, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found for update", propertyId);
                return NotFound(new ApiResponse<PropertyKycDetailsDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<PropertyKycDetailsDto>
            {
                Success = true,
                Message = "Record updated successfully",
                Items = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating KYC details for property {PropertyId}", propertyId);
            return BadRequest(new ApiResponse<PropertyKycDetailsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating KYC details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyKycDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while updating property KYC details"
                });
        }
    }
}
