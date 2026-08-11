using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace NtisPlatform.Application.Services.Asset_Management;

/// <summary>
/// Application service for InventoryDocument upload/download operations.
/// </summary>
public class InventoryDocumentApplicationService : IInventoryDocumentApplicationService
{
    private readonly IInventoryDocumentService _inventoryDocumentService;
    private readonly ILogger<InventoryDocumentApplicationService> _logger;

    public InventoryDocumentApplicationService(
        IInventoryDocumentService inventoryDocumentService,
        ILogger<InventoryDocumentApplicationService> logger)
    {
        _inventoryDocumentService = inventoryDocumentService;
        _logger = logger;
    }

    public async Task<List<InventoryDocumentDto>> GetDocumentsByInventoryBatchAsync(
        int inventoryBatchId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(inventoryBatchId, nameof(inventoryBatchId));
        var records = await _inventoryDocumentService.GetLatestByInventoryBatchIdAsync(inventoryBatchId, cancellationToken);
        return records.Select(MapToDto).ToList();
    }

    public async Task<Dictionary<int, List<InventoryDocumentDto>>> GetDocumentsByInventoryBatchesAsync(
    IReadOnlyCollection<int> inventoryBatchIds,
    CancellationToken cancellationToken = default)
    {
        if (inventoryBatchIds == null || inventoryBatchIds.Count == 0)
        {
            return new Dictionary<int, List<InventoryDocumentDto>>();
        }

        var records = await _inventoryDocumentService.GetLatestByInventoryBatchIdsAsync(inventoryBatchIds, cancellationToken);
        return records
            .GroupBy(r => r.InventoryBatchId)
            .ToDictionary(g => g.Key, g => g.Select(MapToDto).ToList());
    }

    private static InventoryDocumentDto MapToDto(InventoryDocumentEntity p) => new()
    {
        InventoryDocumentId = p.Id,
        InventoryBatchId = p.InventoryBatchId,
        DocumentTypeId = p.DocumentTypeId,
        DocumentTypeCode = p.DocumentType?.DocumentTypeCode ?? string.Empty,
        DocumentTypeName = p.DocumentType?.DocumentTypeName ?? string.Empty,
        DisplayOrder = p.DisplayOrder,
        Remarks = p.Remarks,
        DocumentBindingId = p.DocumentBindingId,
        DocumentGuid = GetSafeDocumentGuid(p.DocumentBinding),
        FileName = GetSafeFileName(p.DocumentBinding),
        MimeType = GetSafeMimeType(p.DocumentBinding)
    };

    private static Guid? GetSafeDocumentGuid(DocumentBindingEntity? documentBinding)
    {
        var doc = documentBinding?.Document;
        return (doc == null || !doc.IsActive || doc.MarkedForDeletion) ? null : doc.DocumentGuid;
    }

    private static string? GetSafeFileName(DocumentBindingEntity? documentBinding)
    {
        var doc = documentBinding?.Document;
        return (doc == null || !doc.IsActive || doc.MarkedForDeletion) ? null : doc.OriginalFileName;
    }

    private static string? GetSafeMimeType(DocumentBindingEntity? documentBinding)
    {
        var doc = documentBinding?.Document;
        return (doc == null || !doc.IsActive || doc.MarkedForDeletion) ? null : doc.MimeType;
    }


    /// <summary>
    /// Bulk-saves inventory document slots for a batch.
    /// For each item: creates a new AMS.InventoryDocuments row (or updates existing).
    /// Returns generated IDs — pass InventoryDocumentId as ReferenceTableId
    /// when uploading the actual file via POST /api/documents/upload
    /// (ReferenceTableName = "InventoryDocument").
    /// Mirrors the PropertyCertificate bulk-save pattern.
    /// </summary>
    public async Task<InventoryDocumentBulkSaveResponseDto> BulkSaveAsync(
        InventoryDocumentBulkSaveDto bulkDto,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(bulkDto.InventoryBatchId, nameof(bulkDto.InventoryBatchId));
        Guard.AgainstNegativeOrZero(createdBy, nameof(createdBy));

        _logger.LogInformation(
            "BulkSaveAsync: Processing {Count} inventory document slots for InventoryBatchId={InventoryBatchId}, User={UserId}",
            bulkDto.Documents.Count, bulkDto.InventoryBatchId, createdBy);

        var response = new InventoryDocumentBulkSaveResponseDto
        {
            InventoryBatchId = bulkDto.InventoryBatchId
        };

        foreach (var item in bulkDto.Documents)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(item.Remarks))
                    Guard.AgainstExceedingLength(item.Remarks, 500, nameof(item.Remarks));

                int savedId;

                if (item.InventoryDocumentId is > 0)
                {
                    var existing = await _inventoryDocumentService.GetByIdAsync(item.InventoryDocumentId.Value, cancellationToken);
                    if (existing == null)
                        throw new ArgumentException($"InventoryDocument with ID {item.InventoryDocumentId.Value} not found.", nameof(item.InventoryDocumentId));

                    if (existing.InventoryBatchId != bulkDto.InventoryBatchId || existing.DocumentTypeId != item.DocumentTypeId)
                        throw new ArgumentException("InventoryDocumentId does not match the provided InventoryBatchId/DocumentTypeId.", nameof(item.InventoryDocumentId));

                    savedId = existing.Id;

                    // Update remarks and display order on existing slot
                    await _inventoryDocumentService.UpdateAsync(
                        savedId,
                        item.DisplayOrder,
                        item.Remarks,
                        createdBy,
                        cancellationToken);
                }
                else
                {
                    // New slot — create the row in AMS.InventoryDocuments
                    savedId = await _inventoryDocumentService.CreateAsync(
                        bulkDto.InventoryBatchId,
                        item.DocumentTypeId,
                        item.DisplayOrder,
                        item.Remarks,
                        createdBy,
                        cancellationToken);
                }

                // Enable / Disable slot
                await _inventoryDocumentService.ToggleEnabledAsync(
                    savedId,
                    item.IsEnabled,
                    createdBy,
                    cancellationToken);

                var entity = await _inventoryDocumentService.GetByIdAsync(savedId, cancellationToken);

                if (item.IsEnabled)
                    response.EnabledCount++;
                else
                    response.DisabledCount++;

                if (entity is not null)
                    response.SavedDocuments.Add(MapToDto(entity));
            }
            catch (Exception ex)
            {
                var msg = $"Error processing DocumentTypeId={item.DocumentTypeId}: {ex.Message}";
                _logger.LogError(ex, "BulkSaveAsync: {Message}", msg);
                response.Errors.Add(msg);
            }
        }

        response.TotalProcessed = response.SavedDocuments.Count;

        _logger.LogInformation(
            "BulkSaveAsync: Completed for InventoryBatchId={InventoryBatchId}. Processed={Total}, Errors={Errors}",
            bulkDto.InventoryBatchId, response.TotalProcessed, response.Errors.Count);

        return response;
    }
}

