using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Exceptions;
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
                    Message = ex is NtisPlatformException ? ex.Message : "An error occurred while retrieving property tax details"
                });
        }
    }

    /// <summary>
    /// Retrieves aggregated tax details for multiple properties filtered by query parameters.
    /// Returns a <see cref="PropertyTaxApartmentDetailsDto"/> response containing aggregate information
    /// and tax amounts as a collection, not a pivoted structure with dynamic tax-name columns.
    /// Only returns records where IsActive=true and MarkedForDeletion=false.
    /// Taxes are ordered by DisplayOrder from TaxMaster table.
    /// Filtering is performed by mapping <see cref="PropertyQueryParameters"/> to <see cref="PropertyApartmentTaxRequestDto"/>,
    /// and applying the repository's current predicate logic for the supplied filter values (exact match for PropertyNo, PartType, etc.).
    /// </summary>
    /// <param name="query">Query parameters for filtering properties, including WardId, PropertyNo, PartType, Type, and Id.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Aggregated property tax details in <see cref="PropertyTaxApartmentDetailsDto"/> format</returns>
    /// <response code="200">Returns the aggregated property tax details in the DTO response format</response>
    /// <response code="404">No properties found or no tax details available</response>
    [HttpGet("apartment-property-tax-details-rv")]
    [ProducesResponseType(typeof(ApiResponse<PropertyTaxApartmentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApartmentPropertyTaxDetailsRV([FromQuery] PropertyQueryParameters query, CancellationToken ct)
    {
        try
        {
            var dto = new PropertyApartmentTaxRequestDto
            {
                WardId = query.WardId,
                PropertyNo = query.PropertyNo,
                PartType = query.PartType,
                Type = query.Type,
                PropertyId = query.Id,
                PartitionNo = query.PartitionNo
            };

            var result = await _propertyService.GetAggregatedPropertyTaxDetailsAsync(dto, ct);

            if (result == null)
            {
                _logger.LogWarning("No tax details found for the filtered properties");
                return NotFound(new ApiResponse<PropertyTaxApartmentDetailsDto>
                {
                    Success = false,
                    Message = $"No tax details found for the filtered properties"
                });
            }

            return Ok(new ApiResponse<PropertyTaxApartmentDetailsDto>
            {
                Success = true,
                Message = $"Aggregated tax details for {result.PropertyCount} {(result.PropertyCount == 1 ? "property" : "properties")} fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving aggregated tax details");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyTaxApartmentDetailsDto>
                {
                    Success = false,
                    Message = ex is NtisPlatformException ? ex.Message : "An error occurred while retrieving aggregated property tax details"
                });
        }
    }

    /// <summary>
    /// Retrieves CV tax details for a specific property from TransMastCV joined with TaxMaster and YearMaster.
    /// This endpoint returns pivoted data where TaxName becomes column headers and TaxAmount are values.
    /// Only returns records where IsActive=true, MarkedForDeletion=false, and for the active financial year.
    /// Taxes are ordered by DisplayOrder from TaxMaster table.
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
                    Message = ex is NtisPlatformException ? ex.Message : "An error occurred while retrieving property CV tax details"
                });
        }
    }

    /// <summary>
    /// Retrieves aggregated CV tax details for multiple properties filtered by query parameters.
    /// Returns a <see cref="PropertyTaxApartmentDetailsCVDto"/> response containing aggregate information
    /// and tax amounts as a collection, not a pivoted structure with dynamic tax-name columns.
    /// Only returns records where IsActive=true, MarkedForDeletion=false, and for the active financial year.
    /// Taxes are ordered by DisplayOrder from TaxMaster table.
    /// Filtering is applied using the provided property query parameter values (WardId, PropertyNo, PartType, Type, Id, etc.).
    /// </summary>
    /// <param name="query">Query parameters for filtering properties, such as WardId, PropertyNo, PartType, Type, and Id</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Aggregated property CV tax details in <see cref="PropertyTaxApartmentDetailsCVDto"/> format</returns>
    /// <response code="200">Returns the aggregated property CV tax details in the DTO response format</response>
    /// <response code="404">No properties found or no CV tax details available</response>
    [HttpGet("apartment-property-tax-details-cv")]
    [ProducesResponseType(typeof(ApiResponse<PropertyTaxApartmentDetailsCVDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApartmentPropertyTaxDetailsCV([FromQuery] PropertyQueryParameters query, CancellationToken ct)
    {
        try
        {
            var dto = new PropertyApartmentTaxRequestDto
            {
                WardId = query.WardId,
                PropertyNo = query.PropertyNo,
                PartType = query.PartType,
                Type = query.Type,
                PropertyId = query.Id,
                PartitionNo = query.PartitionNo
            };

            var result = await _propertyService.GetAggregatedPropertyTaxDetailsCVAsync(dto, ct);

            if (result == null)
            {
                _logger.LogWarning("No CV tax details found for the filtered properties");
                return NotFound(new ApiResponse<PropertyTaxApartmentDetailsCVDto>
                {
                    Success = false,
                    Message = $"No CV tax details found for the filtered properties"
                });
            }

            return Ok(new ApiResponse<PropertyTaxApartmentDetailsCVDto>
            {
                Success = true,
                Message = $"Aggregated CV tax details for {result.PropertyCount} {(result.PropertyCount == 1 ? "property" : "properties")} fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving aggregated CV tax details");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyTaxApartmentDetailsCVDto>
                {
                    Success = false,
                    Message = ex is NtisPlatformException ? ex.Message : "An error occurred while retrieving aggregated property CV tax details"
                });
        }
    }
}
