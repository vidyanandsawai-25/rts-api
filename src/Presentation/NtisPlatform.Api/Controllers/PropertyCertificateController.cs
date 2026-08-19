using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Constants;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Exceptions;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Controller for Building Permissions and Certificates.
/// Aligned with the Property Quick Data Entry UI.
/// Handles: Loading certificate types with status, uploading documents, replacing documents, and bulk saving all changes.
/// </summary>
[ApiController]
[Route("api/property-certificates")]
[Authorize]
public class PropertyCertificateController : ControllerBase
{
    private readonly IPropertyCertificateApplicationService _service;
    private readonly ILogger<PropertyCertificateController> _logger;
    private readonly IWebHostEnvironment _environment;

    public PropertyCertificateController(
        IPropertyCertificateApplicationService service,
        ILogger<PropertyCertificateController> logger,
        IWebHostEnvironment environment)
    {
        _service = service;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 1. GET - Load all certificate types with their current status for a property.
    /// Called when the page opens to populate all certificate cards.
    /// Shows which certificates exist (with data) and which are empty.
    /// Pass propertyDetailsId to scope to one floor's certificates; omit for property-wise
    /// (PropertyDetailsId IS NULL) certificates only.
    /// </summary>
    [HttpGet("types-with-status/{propertyId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyCertificateWithStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCertificateTypesWithStatus(
        int propertyId,
        [FromQuery] int? propertyDetailsId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (propertyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid PropertyId" });

            var result = await _service.GetCertificateTypesWithStatusAsync(propertyId, cancellationToken, propertyDetailsId);

            return Ok(new ApiResponse<List<PropertyCertificateWithStatusDto>>
            {
                Success = true,
                Message = "Certificate types retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting certificate types with status for PropertyId={PropertyId}. CorrelationId: {CorrelationId}",
                propertyId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving certificate types",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// 4. POST - Save all certificate changes with a single button.
    /// Called when user clicks the "Save Changes" button at the bottom of the page.
    /// Saves all metadata for all certificates at once:
    /// - Certificate numbers
    /// - Certificate dates
    /// - Enable/disable status (toggle switches)
    /// Note: Documents are uploaded separately via the upload/replace-document endpoints.
    /// </summary>
    [HttpPost("bulk-save")]
    [ProducesResponseType(typeof(ApiResponse<PropertyCertificateBulkSaveResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkSaveAll(
        [FromBody] PropertyCertificateBulkSaveDto bulkDto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid request data" });

            var result = await _service.BulkSaveAllAsync(bulkDto, GetUserId(), cancellationToken);

            var hasErrors = result.Errors.Any();

            return Ok(new ApiResponse<PropertyCertificateBulkSaveResponseDto>
            {
                Success = !hasErrors,
                Message = hasErrors 
                    ? $"Saved with {result.Errors.Count} error(s). Enabled: {result.EnabledCount}, Disabled: {result.DisabledCount}"
                    : $"All certificates saved successfully. Enabled: {result.EnabledCount}, Disabled: {result.DisabledCount}",
                Items = result,
                Errors = hasErrors ? result.Errors : null
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized access attempt. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Valid user identification is required.",
                CorrelationId = correlationId
            });
        }
        catch (ArgumentException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Validation error in bulk save. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (InvalidOperationException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Bulk save operation failed. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (NtisPlatformException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Domain exception in bulk save. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error in bulk save. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while saving certificates",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// 5. GET - Floor-wise certificate display for the Building Permission tab.
    /// When selectedPropertyDetailsId is passed and matches a floor on this property, that floor is
    /// returned separately as SelectedFloor (so the UI can highlight/auto-open it without scanning
    /// a flat list); every other floor is in OtherFloors. If selectedPropertyDetailsId isn't passed
    /// or doesn't match any floor, SelectedFloor is null and all floors are in OtherFloors. Also
    /// returns property-wise certificates (PropertyDetailsId IS NULL) for the "Apply to Property" scope.
    /// </summary>
    [HttpGet("floor-certificates")]
    [ProducesResponseType(typeof(ApiResponse<FloorCertificatesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFloorCertificates(
        [FromQuery] int propertyId,
        [FromQuery] int? selectedPropertyDetailsId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (propertyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid PropertyId" });

            var result = await _service.GetFloorCertificatesAsync(propertyId, selectedPropertyDetailsId, cancellationToken);

            return Ok(new ApiResponse<FloorCertificatesResponseDto>
            {
                Success = true,
                Message = "Floor certificate details retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting floor certificates for PropertyId={PropertyId}. CorrelationId: {CorrelationId}",
                propertyId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving floor certificate details",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// 6. POST - Building Permission tab "Save" button. Saves/updates certificate metadata only.
    /// Document upload always goes through the Global Document endpoint
    /// (POST /api/documents/upload, with ReferenceTableName=PropertyCertificates and
    /// ReferenceTableId=the PropertyCertificateId this call returns) — never here. For taxable
    /// certificate types (IsTaxable=1), triggers Occupation Tax recalculation.
    /// </summary>
    [HttpPost("save-certificate")]
    [ProducesResponseType(typeof(ApiResponse<SaveCertificateResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveCertificate(
        [FromBody] SaveCertificateRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid request data" });

            var result = await _service.SaveCertificateAsync(request, GetUserId(), cancellationToken);

            return Ok(new ApiResponse<SaveCertificateResponseDto>
            {
                Success = true,
                Message = "Certificate saved successfully",
                Items = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized access attempt. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Valid user identification is required.",
                CorrelationId = correlationId
            });
        }
        catch (ArgumentException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Validation error saving certificate. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (InvalidOperationException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Save certificate failed. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (NtisPlatformException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Domain exception saving certificate. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error saving certificate. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "An error occurred while saving the certificate",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// 7. DELETE - Deletes a certificate's metadata row (soft-delete) by PropertyId + CertificateTypeId,
    /// for callers that only know the property/type/floor (not the internal PropertyCertificateId --
    /// there is no by-id delete endpoint). Pass propertyDetailsId to target a floor-wise certificate;
    /// omit it for the property-wise one (PropertyDetailsId IS NULL). If the certificate has an
    /// attached document, it is cascade-deleted first (unlinked and soft-deleted) so this never
    /// leaves an orphaned, still-active document behind -- callers do not need to call the Global
    /// Document endpoint (DELETE /api/documents/{guid}) separately first. Re-triggers Occupation Tax
    /// recalculation when the certificate type is taxable, since removing a certificate can change
    /// which policy applies to the property.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCertificateByType(
        [FromQuery] int propertyId,
        [FromQuery] int certificateTypeId,
        [FromQuery] int? propertyDetailsId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (propertyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid propertyId" });

            if (certificateTypeId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid certificateTypeId" });

            await _service.DeleteCertificateByTypeAsync(propertyId, certificateTypeId, propertyDetailsId, GetUserId(), cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Certificate deleted successfully"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized access attempt. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Valid user identification is required.",
                CorrelationId = correlationId
            });
        }
        catch (PropertyCertificateNotFoundException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex,
                "Certificate not found for deletion. PropertyId={PropertyId}, CertificateTypeId={CertificateTypeId}, " +
                "PropertyDetailsId={PropertyDetailsId}. CorrelationId: {CorrelationId}",
                propertyId, certificateTypeId, propertyDetailsId, correlationId);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex,
                "Error deleting certificate. PropertyId={PropertyId}, CertificateTypeId={CertificateTypeId}. CorrelationId: {CorrelationId}",
                propertyId, certificateTypeId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the certificate",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// 8. POST - Moves a certificate from one floor/property-wide scope to another (e.g. re-scoping
    /// an OC certificate to a different floor) in one atomic call: the old row is deleted and the
    /// replacement created, then Occupation Tax recalculates exactly once against the final state --
    /// never against the momentarily-certificate-less state a separate DELETE-then-SAVE would expose.
    /// If the scope (PropertyDetailsId) isn't changing, use POST save-certificate instead; it updates
    /// the existing row in place and needs no delete step at all.
    /// </summary>
    [HttpPost("replace-certificate")]
    [ProducesResponseType(typeof(ApiResponse<ReplaceCertificateResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceCertificate(
        [FromBody] ReplaceCertificateRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid request data" });

            var newCertificateId = await _service.ReplaceCertificateByTypeAsync(
                request.PropertyId,
                request.CertificateTypeId,
                request.OldPropertyDetailsId,
                request.NewPropertyDetailsId,
                request.NewCertificateNo,
                request.NewIssueDate,
                GetUserId(),
                cancellationToken);

            return Ok(new ApiResponse<ReplaceCertificateResponseDto>
            {
                Success = true,
                Message = "Certificate replaced successfully",
                Items = new ReplaceCertificateResponseDto { PropertyCertificateId = newCertificateId }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized access attempt. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Valid user identification is required.",
                CorrelationId = correlationId
            });
        }
        catch (PropertyCertificateNotFoundException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex,
                "Certificate not found for replacement. PropertyId={PropertyId}, CertificateTypeId={CertificateTypeId}. " +
                "CorrelationId: {CorrelationId}",
                request.PropertyId, request.CertificateTypeId, correlationId);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex,
                "Error replacing certificate. PropertyId={PropertyId}, CertificateTypeId={CertificateTypeId}. CorrelationId: {CorrelationId}",
                request.PropertyId, request.CertificateTypeId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while replacing the certificate",
                CorrelationId = correlationId
            });
        }
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
        {
            throw new UnauthorizedAccessException("Valid user identification is required.");
        }
        return id;
    }
}
