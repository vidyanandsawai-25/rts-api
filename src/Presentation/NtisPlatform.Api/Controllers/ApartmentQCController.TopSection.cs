using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

public partial class ApartmentQCController
{
    /// <summary>
    /// Returns the property overview strip, property-info panel, and additional-revenue details
    /// for a property resolved from any one of the supplied identifiers. Checked in this order —
    /// the first one satisfied wins: <paramref name="propertyId"/>, <paramref name="upic"/>,
    /// <paramref name="ward"/> + <paramref name="propertyNo"/> (optionally narrowed by
    /// <paramref name="partitionNo"/>), <paramref name="wingDetailsId"/>, then
    /// <paramref name="societyId"/>. Requires authentication -- the property-info panel includes
    /// owner PII (mobile number, Aadhar number), so this cannot be [AllowAnonymous].
    /// </summary>
    /// <response code="200">Top-section data for the resolved property.</response>
    /// <response code="400">No identifier was supplied.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No property found for the given identifiers.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize]
    [HttpGet("top-section")]
    [ProducesResponseType(typeof(ApiResponse<PropertyTopSectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTopSection(
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

        var result = await _topSectionService.GetTopSectionAsync(query, ct);

        if (result is null)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "No property found for the given identifiers."
            });

        return Ok(new ApiResponse<PropertyTopSectionDto>
        {
            Success = true,
            Message = "Record found successfully",
            Items = result
        });
    }

    /// <summary>
    /// Updates the editable fields of the ApartmentQC top section for a given property.
    /// Supports partial updates (PATCH) and full updates (PUT).
    /// </summary>
    /// <param name="propertyId">PropertyMast Id (route).</param>
    /// <param name="dto">Payload containing the fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Top section details updated successfully.</response>
    /// <response code="400">Request payload is missing or no fields were provided.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No active property found for the given propertyId.</response>
    /// <response code="423">Property is locked and cannot be modified.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize]
    [HttpPatch("top-section/{propertyId:int}")]
    [HttpPut("top-section/{propertyId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status423Locked)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        [FromRoute] int propertyId,
        [FromBody] UpdateApartmentQcTopSectionDto dto,
        CancellationToken ct)
    {
        if (dto is null || !dto.HasAnyField())
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Request body is required and must contain at least one field to update."
            });
        }

        int userId;
        try
        {
            userId = GetCurrentUserId();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Valid user authorization claim is required."
            });
        }

        var outcome = await _topSectionService.UpdateTopSectionAsync(propertyId, dto, userId, ct);

        return outcome switch
        {
            TopSectionUpdateOutcome.PropertyNotFound => NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No active property found for propertyId {propertyId}."
            }),
            TopSectionUpdateOutcome.PropertyLocked => StatusCode(StatusCodes.Status423Locked, new ApiResponse<object>
            {
                Success = false,
                Message = "Property is currently locked and cannot be modified."
            }),
            TopSectionUpdateOutcome.NoFieldsProvided => BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "At least one valid field must be provided for update."
            }),
            TopSectionUpdateOutcome.Success => Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Apartment QC top section details updated successfully."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An unexpected error occurred while updating the property top section."
            })
        };
    }

}
