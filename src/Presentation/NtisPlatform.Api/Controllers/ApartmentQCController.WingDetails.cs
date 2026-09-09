using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

public partial class ApartmentQCController
{
    /// <summary>
    /// Updates the editable fields of a single wing in <c>PTIS.WingDetailsMast</c> by its WingDetail ID.
    /// Used by frontend wing cards to update specific wing details independently without affecting society or other wings.
    /// Supports partial updates (PATCH) and full updates (PUT).
    /// </summary>
    /// <param name="wingDetailId">WingDetailsMast ID (route parameter).</param>
    /// <param name="dto">Payload containing the wing fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Wing details updated successfully.</response>
    /// <response code="400">Request payload is missing or no fields were provided.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No active wing found for the given wingDetailId.</response>
    /// <response code="423">Associated property is locked and cannot be modified.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize]
    [HttpPatch("wing-details/{wingDetailId:int}")]
    [HttpPut("wing-details/{wingDetailId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status423Locked)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateWingDetails(
        [FromRoute] int wingDetailId,
        [FromBody] UpdateApartmentQcWingDetailsDto dto,
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

        var outcome = await _topSectionService.UpdateWingDetailsAsync(wingDetailId, dto, userId, ct);

        return outcome switch
        {
            TopSectionUpdateOutcome.WingNotFound => NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No active wing found for wingDetailId {wingDetailId}."
            }),
            TopSectionUpdateOutcome.PropertyNotFound => NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No active wing found for wingDetailId {wingDetailId}."
            }),
            TopSectionUpdateOutcome.PropertyLocked => StatusCode(StatusCodes.Status423Locked, new ApiResponse<object>
            {
                Success = false,
                Message = "Associated property is currently locked and cannot be modified."
            }),
            TopSectionUpdateOutcome.NoFieldsProvided => BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "At least one valid field must be provided for update."
            }),
            TopSectionUpdateOutcome.Success => Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Wing details updated successfully."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An unexpected error occurred while updating the wing details."
            })
        };
    }
}
