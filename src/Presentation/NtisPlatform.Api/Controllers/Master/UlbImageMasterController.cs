using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UlbImageMasterController : ControllerBase
{
    private readonly IUlbImageMasterService _service;
    private readonly IDocumentApplicationService _documentService;
    private readonly ILogger<UlbImageMasterController> _logger;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;

    public UlbImageMasterController(
        IUlbImageMasterService service,
        IDocumentApplicationService documentService,
        ILogger<UlbImageMasterController> logger,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService)
    {
        _service = service;
        _documentService = documentService;
        _logger = logger;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
    }

    [HttpGet]
    [AllowAnonymous]
    public Task<IActionResult> GetAll([FromQuery] UlbImageMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    [AllowAnonymous]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpGet("{documentGuid}/view")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> View(Guid documentGuid, CancellationToken cancellationToken)
    {
        var isValid = await _service.IsUlbImageDocumentAsync(documentGuid, cancellationToken);
        if (!isValid)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Document not found" });

        var (fileStream, fileName, mimeType) = await _documentService.ViewDocumentAsync(documentGuid, cancellationToken);
        if (fileStream == null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Document not found" });

        var contentDisposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline");
        contentDisposition.SetHttpFileName(fileName);
        Response.Headers.ContentDisposition = contentDisposition.ToString();
        return File(fileStream, mimeType, enableRangeProcessing: true);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUlbImageMasterDto createDto, CancellationToken ct)
    {
        createDto.CreatedBy = GetUserId();
        return await this.ExecuteCreate(_service, createDto, _logger, ct);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUlbImageMasterDto updateDto, CancellationToken ct)
    {
        updateDto.UpdatedBy = GetUserId();
        return await this.ExecuteUpdate(_service, id, updateDto, _logger, ct);
    }

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<UlbImageMasterEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

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
