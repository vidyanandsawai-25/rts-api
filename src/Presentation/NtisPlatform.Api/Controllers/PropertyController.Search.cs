using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Search API — thin HTTP adapter.
/// Business logic lives in <c>PropertySearchService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// <c>PropertyValidationException</c> (a subtype of <c>InvalidOperationException</c>)
/// thrown by the service is translated to 400 BadRequest by the filter.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Gets property dashboard statistics for the property search screen.
    /// </summary>
    /// <response code="200">Returns the dashboard statistics</response>
    [HttpGet("search/dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var stats = await _propertySearchService.GetPropertyDashboardStatsAsync(ct);

        return Ok(new ApiResponse<PropertyDashboardStatsDto>
        {
            Success = true,
            Message = "Dashboard statistics retrieved successfully",
            Items = stats
        });
    }

    /// <summary>
    /// Searches properties based on Quick Search or KYC Search criteria.
    /// Supports filtering by Zone, Ward, Property Number Range, Old Property Number,
    /// UPIC ID, CSN, Sub Zone No, Plot No, Property Assessment Status, Owner Name,
    /// Occupier Name, Mobile No, Shop/Building Name, Society Name, and Address.
    /// Supports pagination with PageNumber and PageSize.
    /// </summary>
    /// <response code="200">Returns the paged list of properties matching search criteria</response>
    /// <response code="400">Invalid search parameters</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PropertySearchResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchProperties([FromQuery] PropertySearchQueryParameters queryParameters, CancellationToken ct)
    {
        if (queryParameters == null)
        {
            return BadRequest(new ApiResponse<PagedResult<PropertySearchResponseDto>>
            {
                Success = false,
                Message = "Query parameters cannot be null"
            });
        }

        var result = await _propertySearchService.SearchPropertiesAsync(queryParameters, ct);

        return Ok(new ApiResponse<PagedResult<PropertySearchResponseDto>>
        {
            Success = true,
            Message = result.TotalCount > 0
                ? $"{result.TotalCount} record(s) found"
                : "No records found matching the search criteria",
            Items = result
        });
    }
}
