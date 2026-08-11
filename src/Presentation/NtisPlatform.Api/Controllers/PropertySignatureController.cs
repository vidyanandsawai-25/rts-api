using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Application.DTOs.PropertySignature;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Controller for property sign-off operations.
/// Handles Clerk, Tax Inspector, Assistant Commissioner, and Additional Commissioner approvals.
/// All endpoints require authentication. UserId is extracted from the JWT token.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PropertySignatureController : ControllerBase
{
    private readonly IPropertySignatureService _signatureService;
    private readonly FileValidationHelper _fileValidationHelper;
    private readonly ILogger<PropertySignatureController> _logger;

    public PropertySignatureController(
        ILogger<PropertySignatureController> logger,
        IPropertySignatureService signatureService,
        FileValidationHelper fileValidationHelper)
    {
        _logger           = logger;
        _signatureService = signatureService;
        _fileValidationHelper = fileValidationHelper;
    }

    // ─────────────────────────────────────────────────────
    // Helper: extract UserId from JWT
    // ─────────────────────────────────────────────────────

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst(ClaimTypes.Name)?.Value;

        return int.TryParse(claim, out var id) ? id : 0;
    }

    // ─────────────────────────────────────────────────────
    // 1. Get All Sign Authorities
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns all active signing authorities in sequential order:
    /// Clerk → Tax Inspector → Assistant Commissioner → Additional Commissioner.
    /// </summary>
    [HttpGet("authorities")]
    public async Task<ActionResult<ApiResponse<List<SignAuthorityDto>>>> GetAuthorities(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.GetAuthoritiesAsync(cancellationToken);
            return Ok(new ApiResponse<List<SignAuthorityDto>>
            {
                Success = true,
                Message = "Sign authorities retrieved successfully.",
                Items   = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sign authorities.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving sign authorities."
            });
        }
    }

    // ─────────────────────────────────────────────────────
    // 2. Get Eligible Properties for Signing
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns properties eligible to be signed by the specified authority.
    /// For Clerk: all active properties.
    /// For others: only properties already approved by the previous authority
    ///             and not yet approved by the current authority.
    /// </summary>
    [HttpGet("eligible-properties")]
    public async Task<ActionResult<ApiResponse<List<EligiblePropertyDto>>>> GetEligibleProperties(
        [FromQuery] int signAuthorityId,
        [FromQuery] int? zoneId         = null,
        [FromQuery] int? wardId         = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.GetEligiblePropertiesAsync(
                signAuthorityId, zoneId, wardId, cancellationToken);

            return Ok(new ApiResponse<List<EligiblePropertyDto>>
            {
                Success = true,
                Message = $"{result.Count} eligible property(ies) found.",
                Items   = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving eligible properties for signAuthorityId={Id}.", signAuthorityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving eligible properties."
            });
        }
    }

    // ─────────────────────────────────────────────────────
    // 3. Submit Approvals
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Submits property approvals for a signing authority.
    /// Business rules enforced:
    ///   - Sequential: property must be approved by the previous authority first.
    ///   - Duplicate: property not already approved by the same authority.
    /// Properties that fail validation are returned in RejectedProperties.
    /// </summary>
    /// <summary>
    /// Returns export-ready rows for properties pending at the selected signing authority.
    /// </summary>
    [HttpGet("pending-export")]
    public async Task<ActionResult<ApiResponse<List<PropertySignaturePendingExportDto>>>> GetPendingExportData(
        [FromQuery] int signAuthorityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (signAuthorityId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "SignAuthorityId parameter is required."
                });
            }

            var result = await _signatureService.GetPendingExportDataAsync(signAuthorityId, cancellationToken);

            return Ok(new ApiResponse<List<PropertySignaturePendingExportDto>>
            {
                Success = true,
                Message = $"{result.Count} pending signature record(s) found.",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending export data for signAuthorityId={Id}.", signAuthorityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving pending signature export data."
            });
        }
    }

    [HttpPost("approve")]
    public async Task<ActionResult<ApiResponse<SubmitSignatureResponseDto>>> SubmitApprovals(
        [FromBody] SubmitSignatureRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.PropertyApprovals == null || !request.PropertyApprovals.Any())
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "At least one property approval must be provided."
                });

            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Unable to determine the current user from the token."
                });

            var result = await _signatureService.SubmitApprovalsAsync(userId, request, cancellationToken);

            return Ok(new ApiResponse<SubmitSignatureResponseDto>
            {
                Success = result.ApprovedCount > 0,
                Message = result.Message,
                Items   = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting property approvals.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while submitting approvals."
            });
        }
    }

    /// <summary>
    /// Imports PropertySignatureDetails records from Excel.
    /// SignAuthorityId is supplied separately, while UserId/CreatedBy come from the current session.
    /// Expected Excel columns: PropertyId, Remarks(optional).
    /// </summary>
    [HttpPost("approve/import-excel")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    public async Task<ActionResult<ApiResponse<PropertySignatureExcelUploadResultDto>>> ImportApprovalsFromExcel(
        [FromForm] PropertySignatureExcelUploadFormDto form,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (form.SignAuthorityId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "SignAuthorityId is required."
                });
            }

            if (form.File is null || form.File.Length == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "File is required."
                });
            }

            if (!_fileValidationHelper.IsValidFileType(form.File.ContentType, form.File.FileName))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = _fileValidationHelper.GetInvalidFileTypeMessage()
                });
            }

            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Unable to determine the current user from the token."
                });
            }

            await using var stream = form.File.OpenReadStream();
            var result = await _signatureService.UploadApprovalsFromExcelAsync(
                userId,
                form.SignAuthorityId,
                stream,
                cancellationToken);

            return Ok(new ApiResponse<PropertySignatureExcelUploadResultDto>
            {
                Success = result.ApprovedCount > 0,
                Message = result.Message,
                Items = result,
                Errors = result.RejectedProperties.Select(r =>
                    r.PropertyId > 0
                        ? $"PropertyId {r.PropertyId}: {r.Reason}"
                        : r.Reason).ToList()
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing property approvals from Excel.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while importing approvals from Excel."
            });
        }
    }

    /// <summary>
    /// Downloads the Excel template for PropertySignature upload.
    /// Required columns: PropertyId. Optional column: Remarks.
    /// </summary>
    [HttpGet("approve/template-excel")]
    public async Task<IActionResult> DownloadApprovalTemplate(CancellationToken cancellationToken = default)
    {
        var bytes = await _signatureService.GetApprovalUploadTemplateAsync(cancellationToken);
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        const string fileName = "PropertySignatureUploadTemplate.xlsx";
        return File(bytes, contentType, fileName);
    }

    // ─────────────────────────────────────────────────────
    // 4. Get My Approvals
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns all property approvals submitted by the currently logged-in user
    /// for the specified sign authority and optional zone.
    /// </summary>
    [HttpGet("my-approvals")]
    public async Task<ActionResult<ApiResponse<List<SignatureApprovalDto>>>> GetMyApprovals(
        [FromQuery] int signAuthorityId,
        [FromQuery] int? zoneId         = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Unable to determine the current user from the token."
                });

            var result = await _signatureService.GetMyApprovalsAsync(
                userId, signAuthorityId, zoneId, cancellationToken);

            return Ok(new ApiResponse<List<SignatureApprovalDto>>
            {
                Success = true,
                Message = $"{result.Count} approval record(s) retrieved.",
                Items   = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving approvals for current user.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving your approvals."
            });
        }
    }

    // ─────────────────────────────────────────────────────
    // 5. Get Property Approval Status
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the complete sign-off chain status for a single property.
    /// Shows which authorities have signed and which are still pending.
    /// </summary>
    [HttpGet("TrackStatus")]
    public async Task<ActionResult<ApiResponse<PropertySignatureStatusDto>>> GetPropertyStatus(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.GetPropertySignatureStatusAsync(propertyId, cancellationToken);

            if (result == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Property with Id {propertyId} not found."
                });

            return Ok(new ApiResponse<PropertySignatureStatusDto>
            {
                Success = true,
                Message = "Property signature status retrieved successfully.",
                Items   = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving signature status for propertyId={Id}.", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving property signature status."
            });
        }
    }

    // ─────────────────────────────────────────────────────
    // 6. Revoke Approval
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes (revokes) an approval for a property by a specific authority.
    /// Sets IsActive = false on the approval record.
    /// </summary>
    [HttpDelete("approve")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeApproval(
        [FromQuery] int propertyId,
        [FromQuery] int signAuthorityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();

            var revoked = await _signatureService.RevokeApprovalAsync(
                propertyId, signAuthorityId, userId, cancellationToken);

            if (!revoked)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"No active approval found for PropertyId={propertyId} and SignAuthorityId={signAuthorityId}."
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Approval revoked successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking approval for propertyId={PId}, signAuthorityId={AId}.",
                propertyId, signAuthorityId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while revoking the approval."
            });
        }
    }

    /// <summary>
    /// Returns zone-wise / division-wise sign-off grid data.
    /// Shows Clerk, Tax Inspector, Assistant Commissioner, and Additional Commissioner
    /// signed property counts (structure & unit) and total demands.
    /// </summary>
    [HttpGet("dashboard/sign-grid")]
    public async Task<ActionResult<ApiResponse<SignAuthorityGridResponseDto>>> GetSignAuthorityGrid(
        [FromQuery] int? zoneId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchRequest = zoneId.HasValue 
                ? new PropertySearchRequestDto { ZoneId = zoneId.Value } 
                : null;

            var result = await _signatureService.GetSignAuthorityGridDataAsync(searchRequest, cancellationToken);

            return Ok(new ApiResponse<SignAuthorityGridResponseDto>
            {
                Success = true,
                Message = "Sign-off grid statistics retrieved successfully.",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sign-off grid statistics.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving sign-off grid statistics."
            });
        }
    }

    /// <summary>
    /// Returns ward-wise sign-off grid data for a specific zone.
    /// Shows Clerk, Tax Inspector, Assistant Commissioner, and Additional Commissioner
    /// signed property counts (structure & unit) and total demands for each ward in the zone.
    /// </summary>
    [HttpGet("dashboard/sign-grid/zone/{zoneId}")]
    public async Task<ActionResult<ApiResponse<SignAuthorityGridResponseDto>>> GetSignAuthorityWardGrid(
        int zoneId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _signatureService.GetSignAuthorityWardGridDataAsync(zoneId, cancellationToken);

            return Ok(new ApiResponse<SignAuthorityGridResponseDto>
            {
                Success = true,
                Message = "Ward-wise sign-off grid statistics retrieved successfully.",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ward-wise sign-off grid statistics for zoneId={ZoneId}.", zoneId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving ward-wise sign-off grid statistics."
            });
        }
    }

    [HttpGet("GetBuildingWiseData")]
    [ProducesResponseType(typeof(ApiResponse<PropertySignaturePagedResultDto<PropertySignatureSubGridDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PropertySignaturePagedResultDto<PropertySignatureSubGridDto>>>> GetBuildingWiseData(
      [FromQuery] int wardId,
      [FromQuery] int workflowStageId,
      [FromQuery] int pageNumber = 1,
      [FromQuery] int pageSize = 10,
      CancellationToken cancellationToken = default)
    {
        try
        {
            if (wardId <= 0 || workflowStageId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "WardId and WorkFlowStageId parameters are required."
                });
            }

            var result = await _signatureService.GetSubGridAsync(wardId, workflowStageId, pageNumber, pageSize, cancellationToken);

            return Ok(new ApiResponse<PropertySignaturePagedResultDto<PropertySignatureSubGridDto>>
            {
                Success = true,
                Message = "Property signature sub-grid data retrieved successfully.",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving property signature sub-grid data for WardId {WardId} and WorkFlowStageId {WorkFlowStageId}",
                wardId,
                workflowStageId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving property signature sub-grid data."
            });
        }
    }

    [HttpGet("GetPropertyWiseData")]
    [ProducesResponseType(typeof(ApiResponse<PropertySignaturePagedResultDto<PropertySignaturePropertyWiseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PropertySignaturePagedResultDto<PropertySignaturePropertyWiseDto>>>> GetPropertyWiseData(
        [FromQuery] string propertyNo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(propertyNo))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "PropertyNo parameter is required."
                });
            }

            var result = await _signatureService.GetPropertyWiseDataAsync(propertyNo, pageNumber, pageSize, cancellationToken);

            return Ok(new ApiResponse<PropertySignaturePagedResultDto<PropertySignaturePropertyWiseDto>>
            {
                Success = true,
                Message = "Property-wise signature data retrieved successfully.",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property-wise signature data for PropertyNo {PropertyNo}", propertyNo);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving property-wise signature data."
            });
        }
    }


}
