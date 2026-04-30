using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Tax Details Tab API - Partial controller for segregated property endpoints
/// Handles the `{propertyId}/tax-details` API endpoint which loads tax-specific data
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves tax details for a specific property from PolicyTaxDetails joined with TaxMaster.
    /// This endpoint returns pivoted data where TaxName becomes column headers and TaxAmount are values.
    /// Only returns records where IsActive=true and MarkedForDeletion=false.
    /// Tax values are returned in a pivoted structure based on TaxMaster data.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Property tax details in pivoted format</returns>
    /// <response code="200">Returns the property tax details with pivoted structure</response>
    /// <response code="404">Property not found or no tax details available</response>
    [HttpGet("{propertyId}/tax-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyTaxDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaxDetails(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetTaxDetailsAsync(propertyId, ct);

            if (result == null)
            {
                _logger.LogWarning("No tax details found for Property ID {PropertyId}", propertyId);
                return NotFound(new ApiResponse<PropertyTaxDetailsDto>
                {
                    Success = false,
                    Message = $"No tax details found for Property ID {propertyId}"
                });
            }

            return Ok(new ApiResponse<PropertyTaxDetailsDto>
            {
                Success = true,
                Message = "Record fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyTaxDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property tax details"
                });
        }
    }

    /// <summary>
    /// Retrieves CV tax details for a specific property.
    /// This endpoint returns pivoted data where tax names become column headers and tax amounts are values.
    /// The data is retrieved via the property service for the specified property identifier.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Property CV tax details in pivoted format</returns>
    /// <response code="200">Returns the property CV tax details with pivoted structure</response>
    /// <response code="404">Property not found or no CV tax details available</response>
    [HttpGet("{propertyId}/tax-details-cv")]
    [ProducesResponseType(typeof(ApiResponse<PropertyTaxDetailsCVDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaxDetailsCV(int propertyId, CancellationToken ct)
    {
        try 
        {
            var result = await _propertyService.GetTaxDetailsCVAsync(propertyId, ct);

            if (result == null)
            {
                _logger.LogWarning("No CV tax details found for Property ID {PropertyId}", propertyId);
                return NotFound(new ApiResponse<PropertyTaxDetailsCVDto>
                {
                    Success = false,
                    Message = $"No CV tax details found for Property ID {propertyId}"
                });
            }

            return Ok(new ApiResponse<PropertyTaxDetailsCVDto>
            {
                Success = true,
                Message = "Record fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CV tax details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyTaxDetailsCVDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property CV tax details"
                });
        }
    }
}
