using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

public partial class ApartmentQCController
{
    /// <summary>
    /// Returns every configured workflow stage and certificate type for a property resolved from
    /// any one of the supplied identifiers, in the same precedence as
    /// <c>GET api/ApartmentQC/top-section</c>: <paramref name="propertyId"/>, <paramref name="upic"/>,
    /// <paramref name="ward"/> + <paramref name="propertyNo"/> (optionally narrowed by
    /// <paramref name="partitionNo"/>), <paramref name="wingDetailsId"/>, then
    /// <paramref name="societyId"/>. Requires authentication -- exposes who created/updated each
    /// workflow stage and certificate record, so this cannot be [AllowAnonymous].
    /// </summary>
    /// <response code="200">Below-flex status data for the resolved property.</response>
    /// <response code="400">No identifier was supplied.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No property found for the given identifiers.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize]
    [HttpGet("below-flex")]
    [ProducesResponseType(typeof(ApiResponse<ApartmentQcBelowFlexDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTopSectionBelowFlex(
        [FromQuery] int? propertyId,
        [FromQuery] string? upic,
        [FromQuery] int? ward,
        [FromQuery] string? propertyNo,
        [FromQuery] string? partitionNo,
        [FromQuery] int? wingDetailsId,
        [FromQuery] int? societyId,
        CancellationToken ct)
    {
        var query = new ApartmentQcTopSectionQueryParameters
        {
            PropertyId = propertyId,
            Upic = upic,
            WardId = ward,
            PropertyNo = propertyNo,
            PartitionNo = partitionNo,
            WingDetailsId = wingDetailsId,
            SocietyId = societyId,
        };

        if (!query.HasAnyIdentifier())
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Provide at least one of: propertyId, upic, ward + propertyNo, wingDetailsId, societyId."
            });
        }

        var result = await _belowFlexService.GetBelowFlexAsync(query, ct);

        if (result is null)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "No property found for the given identifiers."
            });

        return Ok(new ApiResponse<ApartmentQcBelowFlexDto>
        {
            Success = true,
            Message = "Record found successfully",
            Items = result
        });
    }
}
