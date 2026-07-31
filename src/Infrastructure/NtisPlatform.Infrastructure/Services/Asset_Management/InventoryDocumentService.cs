using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services.Asset_Management;

/// <summary>
/// Service for AMS.InventoryDocument operations.
/// </summary>
public class InventoryDocumentService : IInventoryDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryDocumentService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateAsync(
        int inventoryBatchId,
        int documentTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var documentTypeExists = await _context.InventoryDocumentTypes
            .AnyAsync(x => x.Id == documentTypeId && x.IsActive, cancellationToken);

        if (!documentTypeExists)
        {
            throw new ArgumentException($"Inventory document type with ID {documentTypeId} not found", nameof(documentTypeId));
        }

        // Ensure only one latest row per (InventoryBatchId, DocumentTypeId)
        var latestExisting = await _context.InventoryDocuments
            .Where(x => x.InventoryBatchId == inventoryBatchId
                        && x.DocumentTypeId == documentTypeId
                        && x.IsLatest
                        && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        foreach (var existing in latestExisting)
        {
            existing.MarkAsSuperseded();
            existing.UpdatedBy = createdBy;
            existing.UpdatedDate = DateTime.Now;
        }

        var entity = InventoryDocumentEntity.Create(
            inventoryBatchId,
            documentTypeId,
            displayOrder,
            remarks);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.InventoryDocuments.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task UpdateDocumentBindingAsync(
        int id,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.InventoryDocuments
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new ArgumentException($"Inventory document record with ID {id} not found.", nameof(id));
        }

        entity.LinkDocumentBinding(documentBindingId);
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<InventoryDocumentEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.InventoryDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && !x.MarkedForDeletion, cancellationToken);
    }

    public async Task<List<InventoryDocumentEntity>> GetLatestByInventoryBatchIdAsync(
        int inventoryBatchId,
        CancellationToken cancellationToken = default)
    {
        return await _context.InventoryDocuments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.DocumentType)
            .Include(x => x.DocumentBinding)
                .ThenInclude(db => db!.Document)
            .Where(x => x.InventoryBatchId == inventoryBatchId
                        && x.IsLatest
                        && x.IsActive
                        && !x.MarkedForDeletion)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.DocumentTypeId)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsSupersededAsync(
        int id,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.InventoryDocuments
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new ArgumentException($"Inventory document record with ID {id} not found.", nameof(id));
        }

        if (!entity.IsLatest)
        {
            throw new ArgumentException(
                $"Inventory document with ID {id} is a superseded version and cannot be replaced.",
                nameof(id));
        }

        entity.MarkAsSuperseded();
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.InventoryDocuments
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new ArgumentException($"InventoryDocument with ID {id} not found", nameof(id));
        }

        entity.MarkForDeletion();
        entity.UpdatedBy = deletedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        int id,
        int? displayOrder,
        string? remarks,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.InventoryDocuments
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"InventoryDocument with ID {id} not found");
        }

        entity.SetDisplayOrder(displayOrder);
        entity.SetRemarks(remarks);
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ToggleEnabledAsync(
        int id,
        bool isEnabled,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.InventoryDocuments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"InventoryDocument with ID {id} not found");
        }

        if (isEnabled)
        {
            if (entity.MarkedForDeletion)
            {
                entity.RestoreFromDeletion();
            }
            entity.IsActive = true;
        }
        else
        {
            if (!entity.MarkedForDeletion)
            {
                entity.MarkForDeletion();
            }
        }

        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
