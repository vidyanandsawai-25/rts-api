using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Old Taxes Details API - Partial controller for segregated property endpoints
/// Handles the `{propertyId}/old-taxes-details` API endpoint which loads historical tax data
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves old taxes details for a property including historical tax data across finance years.
    /// This endpoint is used to populate the Old Taxes Details section in the Old Details tab.
    /// Returns dynamic tax columns based on TaxMaster entries where OldTaxStatus = true.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Property old taxes details with calculated Tax Total, Interest, and Net Total</returns>
    /// <response code="200">Returns the property old taxes details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/old-taxes-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyOldTaxesDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOldTaxesDetails(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetOldTaxesDetailsAsync(propertyId, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
                return NotFound(new ApiResponse<PropertyOldTaxesDetailsDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<PropertyOldTaxesDetailsDto>
            {
                Success = true,
                Message = "Record fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving old taxes details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyOldTaxesDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property old taxes details"
                });
        }
    }

    /// <summary>
    /// Updates old taxes details for a property across multiple finance years.
    /// This endpoint is used to save the Old Taxes Details section in the Old Details tab.
    /// Supports bulk update of tax amounts for multiple years and taxes.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="dto">The update data containing tax information for multiple years</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success response with updated data</returns>
    /// <response code="200">Property old taxes details updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data - Validation error</response>
    [HttpPut("{propertyId}/old-taxes-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyOldTaxesDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOldTaxesDetails(int propertyId, [FromBody] UpdatePropertyOldTaxesDetailsDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.UpdateOldTaxesDetailsAsync(propertyId, dto, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found for update", propertyId);
                return NotFound(new ApiResponse<PropertyOldTaxesDetailsDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<PropertyOldTaxesDetailsDto>
            {
                Success = true,
                Message = "Record updated successfully",
                Items = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating old taxes details for property {PropertyId}", propertyId);
            return BadRequest(new ApiResponse<PropertyOldTaxesDetailsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating old taxes details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyOldTaxesDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while updating property old taxes details"
                });
        }
    }

    /// <summary>
    /// Creates or inserts old taxes details for a property across multiple finance years.
    /// This endpoint is used to create new Old Taxes Details records.
    /// This is a create-only operation that will return 409 Conflict if records already exist.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="dto">The data containing tax information for multiple years to insert</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success response with created data</returns>
    /// <response code="201">Property old taxes details created successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="409">Conflict - Records already exist for the specified year-tax combinations</response>
    /// <response code="400">Invalid data - Validation error</response>
    [HttpPost("{propertyId}/old-taxes-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyOldTaxesDetailsDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOldTaxesDetails(int propertyId, [FromBody] UpdatePropertyOldTaxesDetailsDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.CreateOldTaxesDetailsAsync(propertyId, dto, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found for creation", propertyId);
                return NotFound(new ApiResponse<PropertyOldTaxesDetailsDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return CreatedAtAction(
                nameof(GetOldTaxesDetails),
                new { propertyId },
                new ApiResponse<PropertyOldTaxesDetailsDto>
                {
                    Success = true,
                    Message = "Record created successfully",
                    Items = result
                });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exist"))
        {
            _logger.LogWarning(ex, "Conflict creating old taxes details for property {PropertyId}", propertyId);
            return Conflict(new ApiResponse<PropertyOldTaxesDetailsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating old taxes details for property {PropertyId}", propertyId);
            return BadRequest(new ApiResponse<PropertyOldTaxesDetailsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating old taxes details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyOldTaxesDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while creating property old taxes details"
                });
        }
    }
}
