using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Old Taxes Details API — thin HTTP adapter.
/// Business logic lives in <c>PropertyOldDetailsService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// The filter maps <c>InvalidOperationException</c> with "already exist" → 409 Conflict,
/// and other <c>InvalidOperationException</c> → 400 BadRequest.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves old taxes details for a property including historical tax data across finance years.
    /// </summary>
    /// <response code="200">Returns the property old taxes details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/old-taxes-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyOldTaxesDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOldTaxesDetails(int propertyId, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.GetOldTaxesDetailsAsync(propertyId, ct);

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

    /// <summary>
    /// Updates old taxes details for a property across multiple finance years.
    /// </summary>
    /// <response code="200">Property old taxes details updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data — validation error</response>
    [HttpPut("{propertyId}/old-taxes-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyOldTaxesDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOldTaxesDetails(int propertyId, [FromBody] UpdatePropertyOldTaxesDetailsDto dto, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.UpdateOldTaxesDetailsAsync(propertyId, dto, ct);

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

    /// <summary>
    /// Creates old taxes details for a property across multiple finance years.
    /// Returns 409 Conflict if records already exist for the specified year-tax combinations.
    /// </summary>
    /// <response code="201">Property old taxes details created successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="409">Conflict — records already exist for the specified year-tax combinations</response>
    /// <response code="400">Invalid data — validation error</response>
    [HttpPost("{propertyId}/old-taxes-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyOldTaxesDetailsDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOldTaxesDetails(int propertyId, [FromBody] UpdatePropertyOldTaxesDetailsDto dto, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.CreateOldTaxesDetailsAsync(propertyId, dto, ct);

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
}
