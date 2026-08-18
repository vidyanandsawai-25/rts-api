using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using NtisPlatform.Api.Constants;
using NtisPlatform.Api.DTOs;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Interfaces;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Controller for CORE.Document operations ONLY
/// </summary>
[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly IDocumentApplicationService _documentService;
    private readonly IDocumentAuthorizationService _authorizationService;
    private readonly ILogger<DocumentController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly FileValidationHelper _fileValidationHelper;

    public DocumentController(
        IDocumentApplicationService documentService,
        IDocumentAuthorizationService authorizationService,
        ILogger<DocumentController> logger,
        IWebHostEnvironment environment,
        FileValidationHelper fileValidationHelper)
    {
        _documentService = documentService;
        _authorizationService = authorizationService;
        _logger = logger;
        _environment = environment;
        _fileValidationHelper = fileValidationHelper;
    }

    /// <summary>
    /// Upload a document.
    /// Max file size is configured in appsettings.json under FileStorage:MaxFileSizeBytes (default: 100MB).
    /// The file size limit is enforced at runtime through FormOptions configuration.
    /// Rate limited to prevent abuse - configured in appsettings.json under RateLimiting:FileUpload (default: 10 uploads per 5 minutes)
    /// </summary>
    [HttpPost("upload")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    [ProducesResponseType(typeof(ApiResponse<DocumentUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Upload(
        [FromForm] DocumentUploadFormDto formDto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (formDto.File == null || formDto.File.Length == 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "File is required" });

            if (!_fileValidationHelper.IsValidFileType(formDto.File.ContentType, formDto.File.FileName))
                return BadRequest(new ApiResponse<object> { Success = false, Message = _fileValidationHelper.GetInvalidFileTypeMessage() });

            var uploadDto = new DocumentUploadDto
            {
                OwnerUserId = formDto.OwnerUserId,
                DocumentType = formDto.DocumentType,
                DepartmentId = formDto.DepartmentId,
                ModuleId = formDto.ModuleId,
                ReferenceTableName = formDto.ReferenceTableName,
                ReferenceTableId = formDto.ReferenceTableId,
                ReferenceTableIdGuid = formDto.ReferenceTableIdGuid,
                ReferencePropertyName = formDto.ReferencePropertyName,
                BindingPurpose = formDto.BindingPurpose,
                IsPrimaryDocument = formDto.IsPrimaryDocument,
                AuthDepartmentId = formDto.AuthDepartmentId,
                AuthReferenceId = formDto.AuthReferenceId
            };

            using var stream = formDto.File.OpenReadStream();
            var result = await _documentService.UploadDocumentAsync(
                stream,
                formDto.File.FileName,
                formDto.File.ContentType,
                formDto.File.Length,
                uploadDto,
                GetUserId(allowAnonymous: true),
                cancellationToken);

            return Ok(new ApiResponse<DocumentUploadResponseDto>
            {
                Success = true,
                Message = "Document uploaded successfully",
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
            _logger.LogWarning(ex, "Validation error during document upload. CorrelationId: {CorrelationId}", correlationId);
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
            _logger.LogError(ex, "Error uploading document. CorrelationId: {CorrelationId}, FileName: {FileName}",
                correlationId, formDto.File?.FileName ?? "unknown");

            var errorMessage = _environment.IsDevelopment()
                ? $"An error occurred: {ex.Message}"
                : "An error occurred";

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = errorMessage,
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// Get document by GUID
    /// </summary>
    [HttpGet("{documentGuid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(Guid documentGuid, CancellationToken cancellationToken)
    {
        // Authorization check: verify user can access this document
        var userId = GetUserId(allowAnonymous: true);
        var canAccess = await _authorizationService.CanAccessDocumentAsync(documentGuid, userId, cancellationToken);

        if (!canAccess)
        {
            _logger.LogWarning("User {UserId} attempted unauthorized access to document {DocumentGuid}",
                userId, documentGuid);
            return Forbid();
        }

        var result = await _documentService.GetDocumentAsync(documentGuid, cancellationToken);
        if (result == null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Document not found" });

        return Ok(new ApiResponse<DocumentDto> { Success = true, Items = result });
    }

    /// <summary>
    /// View document (inline display)
    /// </summary>
    [HttpGet("{documentGuid}/view")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> View(Guid documentGuid, CancellationToken cancellationToken)
    {
        // Authorization check: verify user can access this document
        var userId = GetUserId(allowAnonymous: true);
        var canAccess = await _authorizationService.CanAccessDocumentAsync(documentGuid, userId, cancellationToken);

        if (!canAccess)
        {
            _logger.LogWarning("User {UserId} attempted unauthorized view of document {DocumentGuid}",
                userId, documentGuid);
            return Forbid();
        }

        var (fileStream, fileName, mimeType) = await _documentService.ViewDocumentAsync(documentGuid, cancellationToken);
        if (fileStream == null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Document not found" });

        var contentDisposition = new ContentDispositionHeaderValue("inline");
        contentDisposition.SetHttpFileName(fileName);
        Response.Headers.ContentDisposition = contentDisposition.ToString();
        return File(fileStream, mimeType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Download document
    /// </summary>
    [HttpGet("{documentGuid}/download")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Download(Guid documentGuid, CancellationToken cancellationToken)
    {
        // Authorization check: verify user can access this document
        var userId = GetUserId(allowAnonymous: true);
        var canAccess = await _authorizationService.CanAccessDocumentAsync(documentGuid, userId, cancellationToken);

        if (!canAccess)
        {
            _logger.LogWarning("User {UserId} attempted unauthorized download of document {DocumentGuid}",
                userId, documentGuid);
            return Forbid();
        }

        var (fileStream, fileName, mimeType) = await _documentService.DownloadDocumentAsync(documentGuid, userId, cancellationToken);
        if (fileStream == null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Document not found" });

        return File(fileStream, mimeType, fileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// Delete document (soft delete)
    /// </summary>
    [HttpDelete("{documentGuid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid documentGuid, CancellationToken cancellationToken)
    {
        // Authorization check: verify user can modify (delete) this document
        var userId = GetUserId();
        var canModify = await _authorizationService.CanModifyDocumentAsync(documentGuid, userId, cancellationToken);

        if (!canModify)
        {
            _logger.LogWarning("User {UserId} attempted unauthorized deletion of document {DocumentGuid}",
                userId, documentGuid);
            return Forbid();
        }

        var result = await _documentService.DeleteDocumentAsync(documentGuid, userId, cancellationToken);
        if (!result)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Document not found" });

        return Ok(new ApiResponse<object> { Success = true, Message = "Document deleted" });
    }

    /// <summary>
    /// Update DocumentBinding.ReferenceTableId
    /// </summary>
    [HttpPut("binding/{documentBindingId}/reference/{referenceTableId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBindingReference(int documentBindingId, int referenceTableId, CancellationToken cancellationToken)
    {
        // Authorization check: verify user can access this document binding
        var userId = GetUserId();
        var canAccess = await _authorizationService.CanAccessDocumentBindingAsync(documentBindingId, userId, cancellationToken);

        if (!canAccess)
        {
            _logger.LogWarning("User {UserId} attempted unauthorized update of binding {BindingId}",
                userId, documentBindingId);
            return Forbid();
        }

        await _documentService.UpdateDocumentBindingReferenceAsync(documentBindingId, referenceTableId, userId, cancellationToken);
        return Ok(new ApiResponse<object> { Success = true, Message = "Binding updated" });
    }

    /// <summary>
    /// Get document by DocumentBindingId (O(1) access via binding).
    /// Returns document metadata for the document bound to this binding.
    /// </summary>
    [HttpGet("by-binding/{bindingId}")]
    public async Task<IActionResult> GetByBinding(int bindingId, CancellationToken cancellationToken)
    {
        try
        {
            if (bindingId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid binding ID" });

            var userId = GetUserId();

            var canAccess = await _authorizationService.CanAccessDocumentBindingAsync(bindingId, userId, cancellationToken);
            if (!canAccess)
            {
                _logger.LogWarning(
                    "User {UserId} attempted unauthorized access to document binding {BindingId}",
                    userId,
                    bindingId);
                return NotFound(new ApiResponse<object> { Success = false, Message = "Document binding not found" });
            }

            var result = await _documentService.GetDocumentByBindingAsync(bindingId, cancellationToken);
            if (result == null)
                return NotFound(new ApiResponse<object> { Success = false, Message = "Document binding not found" });

            return Ok(new ApiResponse<DocumentDto> { Success = true, Items = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized access to document binding {BindingId}. CorrelationId: {CorrelationId}", bindingId, correlationId);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Valid user identification is required.",
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting document by binding. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred", CorrelationId = correlationId });
        }
    }

    /// <summary>
    /// Get document by reference (department, module, reference table, reference ID).
    /// Resolves document via DocumentBinding lookup.
    /// </summary>
    [HttpGet("by-reference")]
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByReference(
        [FromQuery] int departmentId,
        [FromQuery] int moduleId,
        [FromQuery] string referenceTableName,
        [FromQuery] int referenceTableId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (departmentId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid department ID" });

            if (moduleId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid module ID" });

            if (string.IsNullOrWhiteSpace(referenceTableName))
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Reference table name is required" });

            if (referenceTableId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid reference table ID" });

            var userId = GetUserId();
            var result = await _documentService.GetDocumentByReferenceAsync(
                departmentId, moduleId, referenceTableName, referenceTableId, cancellationToken);

            if (result == null)
                return NotFound(new ApiResponse<object> { Success = false, Message = "Document not found for the given reference" });

            var canAccess = await _authorizationService.CanAccessDocumentAsync(result.DocumentGuid, userId, cancellationToken);
            if (!canAccess)
            {
                _logger.LogWarning("User {UserId} attempted unauthorized access to document {DocumentGuid} via by-reference lookup", userId, result.DocumentGuid);
                return NotFound(new ApiResponse<object> { Success = false, Message = "Document not found for the given reference" });
            }

            return Ok(new ApiResponse<DocumentDto> { Success = true, Items = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to document by reference");
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Valid user identification is required." });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting document by reference. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred", CorrelationId = correlationId });
        }
    }

    /// <summary>
    /// Get document metadata only (no file stream).
    /// Safe for bulk metadata calls and listing operations.
    /// </summary>
    [HttpGet("{documentGuid}/metadata")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMetadata(Guid documentGuid, CancellationToken cancellationToken)
    {
        try
        {
            if (documentGuid == Guid.Empty)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid document GUID" });

            var userId = GetUserId(allowAnonymous: true);
            var canAccess = await _authorizationService.CanAccessDocumentAsync(documentGuid, userId, cancellationToken);

            if (!canAccess)
            {
                _logger.LogWarning("User {UserId} attempted unauthorized metadata access to document {DocumentGuid}", userId, documentGuid);
                return NotFound(new ApiResponse<object> { Success = false, Message = "Document not found" });
            }

            var result = await _documentService.GetDocumentMetadataAsync(documentGuid, cancellationToken);
            if (result == null)
                return NotFound(new ApiResponse<object> { Success = false, Message = "Document not found" });

            return Ok(new ApiResponse<DocumentMetadataDto> { Success = true, Items = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized metadata access to document {DocumentGuid}. CorrelationId: {CorrelationId}", documentGuid, correlationId);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Valid user identification is required.",
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting document metadata. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred", CorrelationId = correlationId });
        }
    }

    private int GetUserId(bool allowAnonymous = false)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
        {
            if (allowAnonymous)
            {
                return 0; // Return 0 (Citizen) for anonymous uploads
            }
            throw new UnauthorizedAccessException("Valid user identification is required.");
        }
        return id;
    }
}