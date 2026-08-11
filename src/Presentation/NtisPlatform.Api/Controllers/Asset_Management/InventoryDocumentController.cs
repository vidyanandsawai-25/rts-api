using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers.Asset_Management;

/// <summary>
/// Controller for AMS.InventoryDocument operations.
/// </summary>
[ApiController]
[Route("api/inventory-documents")]
[Authorize]
public class InventoryDocumentController : ControllerBase
{
    private readonly IInventoryDocumentApplicationService _service;
    private readonly ILogger<InventoryDocumentController> _logger;

    public InventoryDocumentController(
        IInventoryDocumentApplicationService service,
        ILogger<InventoryDocumentController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// POST - Bulk-saves inventory document slots for a batch.
    /// For each item in the list: creates a new slot in AMS.InventoryDocuments (or keeps existing).
    /// Returns the generated IDs — each SavedDocument.InventoryDocumentId must be passed as
    /// ReferenceTableId when uploading the actual file via POST /api/documents/upload
    /// (ReferenceTableName = "InventoryDocument").
    /// Mirrors POST /api/property-certificates/bulk-save.
    /// </summary>
    [HttpPost("bulk-save")]
    [ProducesResponseType(typeof(ApiResponse<InventoryDocumentBulkSaveResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BulkSave(
        [FromBody] InventoryDocumentBulkSaveDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.InventoryBatchId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid InventoryBatchId" });
            if (request.Documents == null || request.Documents.Count == 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Documents list cannot be empty" });

            var userId = GetUserId();
            var result = await _service.BulkSaveAsync(request, userId, cancellationToken);

            var hasErrors = result.Errors.Any();

            return Ok(new ApiResponse<InventoryDocumentBulkSaveResponseDto>
            {
                Success = !hasErrors,
                Message = hasErrors
                    ? $"Saved with {result.Errors.Count} error(s). Enabled: {result.EnabledCount}, Disabled: {result.DisabledCount}"
                    : $"All inventory document slots saved. Enabled: {result.EnabledCount}, Disabled: {result.DisabledCount}",
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
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error in bulk-save for inventory documents. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred while saving inventory document slots", CorrelationId = correlationId });
        }
    }

    [HttpGet("inventory-batch/{inventoryBatchId}")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryDocumentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDocumentsByInventoryItem(int inventoryBatchId, CancellationToken cancellationToken)
    {
        try
        {
            if (inventoryBatchId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid inventoryBatchId" });

            var result = await _service.GetDocumentsByInventoryBatchAsync(inventoryBatchId, cancellationToken);
            return Ok(new ApiResponse<List<InventoryDocumentDto>> { Success = true, Message = "Inventory documents retrieved successfully", Items = result });
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
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting documents for InventoryBatchId={InventoryBatchId}. CorrelationId: {CorrelationId}", inventoryBatchId, correlationId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An error occurred while retrieving inventory documents", CorrelationId = correlationId });
        }
    }



    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
            throw new UnauthorizedAccessException("Valid user identification is required.");
        return id;
    }
}
