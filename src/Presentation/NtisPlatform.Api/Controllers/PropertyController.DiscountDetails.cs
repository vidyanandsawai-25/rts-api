using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Discount Information Tab API — thin HTTP adapter.
/// Business logic lives in <c>PropertyDiscountService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// Upload methods retain their own try/catch because they handle
/// <c>UnauthorizedAccessException</c> and <c>ArgumentException</c> distinctly,
/// and <c>ReplaceDiscountDocument</c> maps <c>InvalidOperationException</c> to 404
/// (which would conflict with the filter's 400 mapping).
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves discount information for a specific property including all social attributes where IsDiscountApplicable=1.
    /// </summary>
    /// <response code="200">Returns the property discount details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/discount-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDiscountInfoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiscountDetails(int propertyId, CancellationToken ct)
    {
        var result = await _propertyDiscountService.GetDiscountDetailsAsync(propertyId, ct);

        if (result == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
            return NotFound(new ApiResponse<PropertyDiscountInfoResponseDto>
            {
                Success = false,
                Message = $"Property with ID {propertyId} not found"
            });
        }

        return Ok(new ApiResponse<PropertyDiscountInfoResponseDto>
        {
            Success = true,
            Message = "Discount information retrieved successfully",
            Items = result
        });
    }

    /// <summary>
    /// Updates discount information for a specific property by upserting PropertySocialDetails records.
    /// </summary>
    /// <response code="200">Discount information updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data — validation error</response>
    [HttpPut("{propertyId}/discount-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDiscountInfoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDiscountDetails(int propertyId, [FromBody] UpsertPropertyDiscountInfoDto dto, CancellationToken ct)
    {
        if (dto.PropertyId != propertyId)
        {
            return BadRequest(new ApiResponse<PropertyDiscountInfoResponseDto>
            {
                Success = false,
                Message = "PropertyId in URL does not match PropertyId in request body"
            });
        }

        var result = await _propertyDiscountService.UpdateDiscountDetailsAsync(propertyId, dto, ct);

        if (result == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found for update", propertyId);
            return NotFound(new ApiResponse<PropertyDiscountInfoResponseDto>
            {
                Success = false,
                Message = $"Property with ID {propertyId} not found"
            });
        }

        return Ok(new ApiResponse<PropertyDiscountInfoResponseDto>
        {
            Success = true,
            Message = "Discount information updated successfully",
            Items = result
        });
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
        {
            throw new UnauthorizedAccessException("Valid user identification is required.");
        }
        return id;
    }
}
