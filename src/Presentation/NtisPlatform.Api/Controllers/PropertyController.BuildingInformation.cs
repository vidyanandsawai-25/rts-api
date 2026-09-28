using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Building3DView;
using NtisPlatform.Application.DTOs.Property;
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


    /// <summary>
    /// Retrieves 3D building view representation for a given property.
    /// </summary>
    /// <param name="queryParams">Building 3D view query parameters containing PropertyId and optional WingdetailsId.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>3D building view data structure.</returns>
    /// <response code="200">Returns the 3D building view data.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="404">Property not found.</response>
    [HttpGet("building-3D-view")]
    [ProducesResponseType(typeof(ApiResponse<Building3DViewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Building3DViewDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<Building3DViewDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBuilding3DView([FromQuery] Building3DViewQueryParameters queryParams, CancellationToken ct)
    {
        var result = await _building3DViewService.GetBuilding3DViewAsync(queryParams, ct);

        if (result == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found for 3D Building View", queryParams.PropertyId);
            return NotFound(new ApiResponse<Building3DViewDto>
            {
                Success = false,
                Message = $"Property with ID {queryParams.PropertyId} not found"
            });
        }

        return Ok(new ApiResponse<Building3DViewDto>
        {
            Success = true,
            Message = "Record fetched successfully",
            Items = result
        });
    }
} 
