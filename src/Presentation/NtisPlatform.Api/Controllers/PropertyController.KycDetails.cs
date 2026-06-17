using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property KYC Details Tab API — thin HTTP adapter.
/// Business logic lives in <c>PropertyKycService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves KYC details for a specific property including owner/occupier, address and contact information.
    /// </summary>
    /// <response code="200">Returns the property KYC details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/kyc-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyKycDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKycDetails(int propertyId, CancellationToken ct)
    {
        var result = await _propertyKycService.GetKycDetailsAsync(propertyId, ct);

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

    /// <summary>
    /// Updates KYC details for a specific property across multiple tables.
    /// </summary>
    /// <response code="200">Property KYC details updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data — FK constraint violation or validation error</response>
    [HttpPut("{propertyId}/kyc-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyKycDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateKycDetails(int propertyId, [FromBody] UpdatePropertyKycDetailsDto dto, CancellationToken ct)
    {
        var result = await _propertyKycService.UpdateKycDetailsAsync(propertyId, dto, ct);

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
}
