using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Combine API - Partial controller for property combination operations
/// Handles property combining, uncombining, and related property lookup functionality
/// Uses [FromServices] for dependency injection to avoid polluting main constructor
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Gets all combined property records with pagination and filtering support.
    /// This endpoint is used to view the history and list of combined properties.
    /// </summary>
    /// <param name="combinePropertyService">Injected service for combine operations (scoped to this endpoint)</param>
    /// <param name="queryParams">Query parameters for filtering, sorting, and pagination</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of combined property records</returns>
    /// <response code="200">Returns the paginated list of combined properties</response>
    [HttpGet("combine-properties")]
    [ProducesResponseType(typeof(PagedResult<CombinePropertyDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAllCombineProperties(
        [FromServices] ICombinePropertyService combinePropertyService,
        [FromQuery] CombinePropertyQueryParameters queryParams, 
        CancellationToken ct)
        => this.ExecuteGetAllPaged(combinePropertyService, queryParams, _logger, ct);

    /// <summary>
    /// Get property details by WardId, PropertyNo, and comma-separated PartitionNo.
    /// This endpoint is used to search and validate properties before combining them.
    /// Returns detailed information including owner, occupier, and financial status.
    /// </summary>
    /// <param name="combinePropertyService">Injected service for combine operations (scoped to this endpoint)</param>
    /// <param name="queryParams">Query parameters containing WardId, PropertyNo, and PartitionNo</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of property details with WardNo, PropertyNo, PartitionNo, OwnerName, and OccupierName</returns>
    /// <response code="200">Returns the list of property combine details</response>
    [HttpGet("combine-properties-details")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyCombineDetailsDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPropertyCombineDetails(
        [FromServices] ICombinePropertyService combinePropertyService,
        [FromQuery] PropertyCombineDetailsQueryParameters queryParams,
        CancellationToken ct)
    {
        try
        {
            var result = await combinePropertyService.GetPropertyCombineDetailsAsync(queryParams, ct);
            
            return Ok(new ApiResponse<List<PropertyCombineDetailsDto>>
            {
                Success = true,
                Message = "Property details fetched successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property combine details for WardId: {WardId}, PropertyNo: {PropertyNo}", 
                queryParams.WardId, queryParams.PropertyNo);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<List<PropertyCombineDetailsDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property combine details"
                });
        }
    }

    /// <summary>
    /// Combine multiple properties into a main property.
    /// This operation creates history records and updates property relationships.
    /// The main property retains its identity while combined properties are linked to it.
    /// </summary>
    /// <param name="combinePropertyService">Injected service for combine operations (scoped to this endpoint)</param>
    /// <param name="request">Request containing MainPropertyId and comma-separated CombinePropertyIds</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with success status and combined property IDs</returns>
    /// <response code="200">Properties combined successfully</response>
    /// <response code="400">Invalid request - validation error</response>
    [HttpPost("combine-properties")]
    [ProducesResponseType(typeof(ApiResponse<CombinePropertiesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CombineProperties(
        [FromServices] ICombinePropertyService combinePropertyService,
        [FromBody] CombinePropertiesRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var result = await combinePropertyService.CombinePropertiesAsync(request, ct);

            if (result.Success)
            {
                return Ok(new ApiResponse<CombinePropertiesResponseDto>
                {
                    Success = true,
                    Message = result.Message,
                    Items = result
                });
            }

            return BadRequest(new ApiResponse<CombinePropertiesResponseDto>
            {
                Success = false,
                Message = result.Message,
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error combining properties for MainPropertyId: {MainPropertyId}", request.MainPropertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<CombinePropertiesResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while combining properties"
                });
        }
    }
}
