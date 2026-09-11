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
    /// <param name="dtos">Building-information search parameters list.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of matching building information.</returns>
    /// <response code="200">Returns matching building information.</response>
    /// <response code="400">Invalid search parameters.</response>
    [HttpPost("building-information/search")]
    [ProducesResponseType(
        typeof(ApiResponse<List<PropertyBuildingInformationDto>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchBuildingInformation(
        [FromBody] List<SearchBuildingInformationDto>? dtos,
        CancellationToken ct)
    {
        var result = await _propertyService
            .SearchBuildingInformationAsync(dtos!, ct);

        return Ok(new ApiResponse<List<PropertyBuildingInformationDto>>
        {
            Success = true,
            Message = result.Count > 0
                ? $"{result.Count} record(s) found"
                : "No records found matching the search criteria",
            Items = result
        });
    }
}