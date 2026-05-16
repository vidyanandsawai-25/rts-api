using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using NtisPlatform.Api.Constants;
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
                ModuleCode = formDto.ModuleCode,
                ReferenceTableName = formDto.ReferenceTableName,
                ReferenceTableId = formDto.ReferenceTableId,
                ReferenceTableIdGuid = formDto.ReferenceTableIdGuid,
                BindingPurpose = formDto.BindingPurpose,
                IsPrimaryDocument = formDto.IsPrimaryDocument,
                AuthModuleCode = formDto.AuthModuleCode,
                AuthReferenceId = formDto.AuthReferenceId
            };

            using var stream = formDto.File.OpenReadStream();
            var result = await _documentService.UploadDocumentAsync(
                stream,
                formDto.File.FileName,
                formDto.File.ContentType,
                formDto.File.Length,
                uploadDto,
                GetUserId(),
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
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(Guid documentGuid, CancellationToken cancellationToken)
    {
        // Authorization check: verify user can access this document
        var userId = GetUserId();
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> View(Guid documentGuid, CancellationToken cancellationToken)
    {
        // Authorization check: verify user can access this document
        var userId = GetUserId();
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Download(Guid documentGuid, CancellationToken cancellationToken)
    {
        // Authorization check: verify user can access this document
        var userId = GetUserId();
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
