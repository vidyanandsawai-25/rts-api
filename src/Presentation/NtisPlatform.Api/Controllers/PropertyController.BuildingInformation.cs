using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.PropertyBuildingInformation;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property building-information API.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Searches building information by old ward number,
    /// optional old society name and optional map identifier.
    /// </summary>
    /// <param name="queryParameters">Building-information search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated list of matching building information.</returns>
    /// <response code="200">Returns matching building information.</response>
    /// <response code="400">Invalid search parameters.</response>
    [HttpGet("building-information/search")]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<PropertyBuildingInformationDto>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchBuildingInformation(
        [FromQuery] BuildingInformationQueryParameters queryParameters,
        CancellationToken ct)
    {
        var result = await _propertyService
            .SearchBuildingInformationAsync(queryParameters, ct);

        return Ok(new ApiResponse<PagedResult<PropertyBuildingInformationDto>>
        {
            Success = true,
            Message = result.TotalCount > 0
                ? $"{result.TotalCount} record(s) found"
                : "No records found matching the search criteria",
            Items = result
        });
    }
} 