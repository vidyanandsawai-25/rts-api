using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

public partial class ApartmentQCController
{
    /// <summary>
    /// "Add Certificate Record" -- creates one or more certificate rows scoped to Apartment
    /// (Society-wide), Wing, or a set of units under one wing. See
    /// <see cref="CreateCertificateRecordRequestDto"/> for the exact scope/collapsing rules.
    /// Document upload is separate: upload via <c>POST /api/documents/upload</c>
    /// (ReferenceTableName=PropertyCertificates, ReferenceTableId=each PropertyCertificateId
    /// returned here) once per created row, the same way the existing CC/OC/Electric Bill flow does.
    /// Certificate types are read dynamically from <c>GET /api/property-certificates/type-master</c> --
    /// whatever is seeded in PropertyCertificateTypeMaster, not a fixed list.
    /// </summary>
    /// <response code="200">Certificate record created.</response>
    /// <response code="400">Validation error (missing WingDetailId/UnitPropertyIds for the selected level, or a UnitPropertyId not under the given wing).</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize]
    [HttpPost("certificate-record")]
    [ProducesResponseType(typeof(ApiResponse<CreateCertificateRecordResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCertificateRecord(
        [FromBody] CreateCertificateRecordRequestDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid request data" });
        }

        try
        {
            var result = await _certificateApplicationService.CreateCertificateRecordAsync(request, GetCurrentUserId(), ct);

            return Ok(new ApiResponse<CreateCertificateRecordResponseDto>
            {
                Success = true,
                Message = $"Certificate record created successfully ({result.EffectiveScope} scope, {result.UnitCount} unit(s)).",
                Items = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
    }
}
