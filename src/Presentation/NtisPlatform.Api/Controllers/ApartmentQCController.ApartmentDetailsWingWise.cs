using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

public partial class ApartmentQCController
{
    /// <summary>
    /// Returns a paginated list of apartment details filtered wing-wise using WingDetailId.
    /// </summary>
    /// <param name="query">Filter parameters including WingDetailId.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Filtered apartment QC records (empty list when no matches).</response>
    /// <response code="400">Validation error.</response>
    [HttpGet("apartment-details-wing-wise")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ApartmentQCComparisonDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetApartmentDetailsWingWise(
        [FromQuery] GetApartmentDetailsWingWiseQueryParameters query,
        CancellationToken ct)
    {
        var result = await _getApartmentDetailsWingWiseService.GetApartmentDetailsWingWiseAsync(query, ct);
        return Ok(new ApiResponse<PagedResult<ApartmentQCComparisonDto>>
        {
            Success = true,
            Message = result.TotalCount > 0 ? "Record found successfully" : "No records found",
            Items = result
        });
    }
}
