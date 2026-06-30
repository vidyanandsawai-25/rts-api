using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Tab Header Info API — thin HTTP adapter.
/// Business logic lives in <c>PropertyOldDetailsService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves the tab header info (StatusName and Old property details) for a specific property.
    /// </summary>
    /// <response code="200">Returns the tab header info</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/tab-header-info")]
    [ProducesResponseType(typeof(ApiResponse<PropertyTabHeaderInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTabHeaderInfo(int propertyId, CancellationToken ct)
    {
        var result = await _propertyOldDetailsService.GetTabHeaderInfoAsync(propertyId, ct);

        if (result == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
            return NotFound(new ApiResponse<PropertyTabHeaderInfoDto>
            {
                Success = false,
                Message = $"Property with ID {propertyId} not found"
            });
        }

        return Ok(new ApiResponse<PropertyTabHeaderInfoDto>
        {
            Success = true,
            Message = "Record fetched successfully",
            Items = result
        });
    }
}
