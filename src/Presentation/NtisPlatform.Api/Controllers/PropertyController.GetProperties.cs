using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

public partial class PropertyController
{
    /// <summary>
    /// Retrieves property records filtered by ward, property number range, optional partition number, merge status flag, and user survey visit info.
    /// </summary>
    /// <param name="queryParameters">Query parameters containing WardNo, FromPropertyNo, ToPropertyNo, PartitionNo, Flag, and UserId</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of property records matching the filter criteria</returns>
    /// <response code="200">Records fetched successfully</response>
    /// <response code="400">Invalid query parameters</response>
    [HttpGet("get-properties")]
    [ProducesResponseType(typeof(ApiResponse<List<GetPropertiesItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<GetPropertiesItemDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProperties(
        [FromQuery] GetPropertiesQueryParameters queryParameters,
        CancellationToken ct)
    {
        try
        {
            if (queryParameters == null)
            {
                return BadRequest(new ApiResponse<List<GetPropertiesItemDto>>
                {
                    Success = false,
                    Message = "Query parameters are required"
                });
            }

            if (string.IsNullOrWhiteSpace(queryParameters.WardNo))
            {
                return BadRequest(new ApiResponse<List<GetPropertiesItemDto>>
                {
                    Success = false,
                    Message = "wardNo is required"
                });
            }

            if (string.IsNullOrWhiteSpace(queryParameters.FromPropertyNo))
            {
                return BadRequest(new ApiResponse<List<GetPropertiesItemDto>>
                {
                    Success = false,
                    Message = "fromPropertyNo is required"
                });
            }

            if (string.IsNullOrWhiteSpace(queryParameters.ToPropertyNo))
            {
                return BadRequest(new ApiResponse<List<GetPropertiesItemDto>>
                {
                    Success = false,
                    Message = "toPropertyNo is required"
                });
            }

            if (queryParameters.UserId <= 0)
            {
                return BadRequest(new ApiResponse<List<GetPropertiesItemDto>>
                {
                    Success = false,
                    Message = "userId is required and must be greater than 0"
                });
            }

            if (string.IsNullOrWhiteSpace(queryParameters.Flag))
            {
                return BadRequest(new ApiResponse<List<GetPropertiesItemDto>>
                {
                    Success = false,
                    Message = "flag is required"
                });
            }

            var result = await _propertyService.GetPropertiesAsync(queryParameters, ct);

            return Ok(new ApiResponse<List<GetPropertiesItemDto>>
            {
                Success = true,
                Message = "Records fetched successfully",
                Items = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in get-properties endpoint");
            return BadRequest(new ApiResponse<List<GetPropertiesItemDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving properties for WardNo: {WardNo}, FromPropertyNo: {FromPropertyNo}, ToPropertyNo: {ToPropertyNo}",
                queryParameters?.WardNo, queryParameters?.FromPropertyNo, queryParameters?.ToPropertyNo);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<List<GetPropertiesItemDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property records"
                });
        }
    }
}
