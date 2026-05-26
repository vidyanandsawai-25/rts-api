using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Search API - Partial controller for property search functionality
/// Handles the search endpoint which supports both Quick Search and KYC Search tabs
/// Also provides dashboard statistics for the property search screen
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Gets property dashboard statistics for the property search screen.
    /// Shows counts like Registered, Geo-Sequencing, Assessed, Unassessed, Survey properties.
    /// These statistics are displayed at the top of the property search screen.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dashboard statistics with property counts</returns>
    /// <response code="200">Returns the dashboard statistics</response>
    [HttpGet("search/dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        try
        {
            var stats = await _propertyService.GetPropertyDashboardStatsAsync(ct);

            return Ok(new ApiResponse<PropertyDashboardStatsDto>
            {
                Success = true,
                Message = "Dashboard statistics retrieved successfully",
                Items = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property dashboard statistics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyDashboardStatsDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving dashboard statistics"
                });
        }
    }

    /// <summary>
    /// Searches properties based on Quick Search or KYC Search criteria.
    /// Supports filtering by Zone, Ward, Property Number Range, Old Property Number,
    /// UPIC ID, CSN, Sub Zone No, Plot No, Property Assessment Status, Owner Name,
    /// Occupier Name, Mobile No, Shop/Building Name, Society Name, and Address.
    /// Supports pagination with PageNumber and PageSize.
    /// </summary>
    /// <param name="queryParameters">Query parameters including supported search filters such as ZoneId, WardId, PropertyNoFrom, PropertyNoTo, OldPropertyNo, UpicId, Csn, SubZoneNo, PlotNo, PropertyAssessmentStatusId, OwnerName, OccupierName, MobileNo, ShopOrBuildingName, SocietyName, Address, and pagination options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paged list of properties matching the search criteria</returns>
    /// <response code="200">Returns the paged list of properties matching search criteria</response>
    /// <response code="400">Invalid search parameters</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PropertySearchResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchProperties([FromQuery] PropertySearchQueryParameters queryParameters, CancellationToken ct)
    {
        try
        {
            if (queryParameters == null)
            {
                return BadRequest(new ApiResponse<PagedResult<PropertySearchResponseDto>>
                {
                    Success = false,
                    Message = "Query parameters cannot be null"
                });
            }

            var result = await _propertyService.SearchPropertiesAsync(queryParameters, ct);

            return Ok(new ApiResponse<PagedResult<PropertySearchResponseDto>>
            {
                Success = true,
                Message = result.TotalCount > 0 
                    ? $"{result.TotalCount} record(s) found" 
                    : "No records found matching the search criteria",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching properties with filters: {@Filters}", new 
            { 
                queryParameters.ZoneId, 
                queryParameters.WardId, 
                queryParameters.PropertyNoFrom, 
                queryParameters.PropertyNoTo,
                queryParameters.PageNumber,
                queryParameters.PageSize
            });
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PagedResult<PropertySearchResponseDto>>
                {
                    Success = false,
                    Message = "An error occurred while searching properties"
                });
        }
    }
}
