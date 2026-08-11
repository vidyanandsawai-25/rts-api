using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Core.Interfaces.Asset_Management;

/// <summary>
/// Service for AMS.InventoryDocument operations.
/// </summary>
public interface IInventoryDocumentService
{
    Task<int> CreateAsync(
        int inventoryBatchId,
        int documentTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int id,
        int? displayOrder,
        string? remarks,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task ToggleEnabledAsync(
        int id,
        bool isEnabled,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task UpdateDocumentBindingAsync(
        int id,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task<InventoryDocumentEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<InventoryDocumentEntity>> GetLatestByInventoryBatchIdAsync(
        int inventoryBatchId,
        CancellationToken cancellationToken = default);

    Task<List<InventoryDocumentEntity>> GetLatestByInventoryBatchIdsAsync(
    IReadOnlyCollection<int> inventoryBatchIds,
    CancellationToken cancellationToken = default);

    Task MarkAsSupersededAsync(
        int id,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default);
}
