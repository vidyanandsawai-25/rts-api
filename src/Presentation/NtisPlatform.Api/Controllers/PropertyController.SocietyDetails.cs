using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Society Details Tab API — thin HTTP adapter.
/// Business logic lives in <c>PropertySocietyService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves society details for a specific property including joined data from related tables.
    /// </summary>
    /// <response code="200">Returns the property society details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/society-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertySocietyDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSocietyDetails(int propertyId, CancellationToken ct)
    {
        var result = await _propertySocietyService.GetSocietyDetailsAsync(propertyId, ct);

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

    [HttpGet("{SocietyDetailId}/{isAmenity}/society-amenity-details")]
    [ProducesResponseType(typeof(ApiResponse<List<SocietyAminityDetailsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SocietyAminityDetailsDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSocietyAmenityDetailsAsync(
        int SocietyDetailId,
        bool isAmenity = false,
        CancellationToken ct = default)
    {
        var result = await _propertyService.GetSocietyAmenityDetailsAsync(SocietyDetailId, isAmenity, ct);

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

    [HttpGet("{propertyId}/society-Wing-details")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertySocietyDetailsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSocietyWingListAsync(int propertyId, CancellationToken ct)
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

    /// <summary>
    /// Updates society details for a specific property.
    /// </summary>
    /// <response code="200">Property society details updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data — FK constraint violation or validation error</response>
    [HttpPut("{propertyId}/society-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertySocietyDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSocietyDetails(int propertyId, [FromBody] UpdatePropertySocietyDetailsDto dto, CancellationToken ct)
    {
        var result = await _propertySocietyService.UpdateSocietyDetailsAsync(propertyId, dto, ct);

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
}
