using NtisPlatform.Application.DTOs.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

/// <summary>
/// Application service for AMS.InventoryDocument operations.
/// </summary>
public interface IInventoryDocumentApplicationService
{
    Task<InventoryDocumentBulkSaveResponseDto> BulkSaveAsync(
        InventoryDocumentBulkSaveDto bulkDto,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task<List<InventoryDocumentDto>> GetDocumentsByInventoryBatchAsync(
        int inventoryBatchId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<int, List<InventoryDocumentDto>>> GetDocumentsByInventoryBatchesAsync(
        IReadOnlyCollection<int> inventoryBatchIds,
        CancellationToken cancellationToken = default);
}
