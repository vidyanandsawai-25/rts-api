using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// PropertySearchByCategory API — thin HTTP adapter.
/// Business logic and per-category validation live in <c>PropertySearchService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Searches properties scoped by SearchCategory: 1=ZoneWise, 2=WardWise, 3=BuildingWise, 4=FromToProperty.
    /// Additionally supports optional filtering by PartType, PropertyCategoryName, and
    /// PropertyAssessmentStatusId regardless of the selected category.
    /// Supports pagination with PageNumber and PageSize.
    /// </summary>
    /// <response code="200">Returns the paged list of properties matching the category scope</response>
    /// <response code="400">Invalid or missing parameters for the selected SearchCategory</response>
    [HttpGet("search-by-category")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PropertySearchByCategoryResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchPropertiesByCategory([FromQuery] PropertySearchByCategoryQueryParameters queryParameters, CancellationToken ct)
    {
        var result = await _propertySearchService.SearchByCategoryAsync(queryParameters, ct);

        return Ok(new ApiResponse<PagedResult<PropertySearchByCategoryResponseDto>>
        {
            Success = true,
            Message = result.TotalCount > 0
                ? $"{result.TotalCount} record(s) found"
                : "No records found matching the search criteria",
            Items = result
        });
    }
}
