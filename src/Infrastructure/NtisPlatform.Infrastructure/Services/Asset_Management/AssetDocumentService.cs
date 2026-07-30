using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services.Asset_Management;

/// <summary>
/// Service for AMS.AssetDocument operations.
/// </summary>
public class AssetDocumentService : IAssetDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public AssetDocumentService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateAsync(
        int assetId,
        int documentDefinitionId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var assetExists = await _context.AssetMaster
            .AsNoTracking()
            .AnyAsync(x => x.Id == assetId && !x.MarkedForDeletion, cancellationToken);

        if (!assetExists)
        {
            throw new ArgumentException($"Asset with ID {assetId} not found", nameof(assetId));
        }

        var documentDefinitionExists = await _context.AssetDocumentDefinitions
            .AsNoTracking()
            .AnyAsync(x => x.Id == documentDefinitionId && x.IsActive, cancellationToken);

        if (!documentDefinitionExists)
        {
            throw new ArgumentException($"Document definition with ID {documentDefinitionId} not found", nameof(documentDefinitionId));
        }

        // Supersede any existing latest documents for this asset and document definition
        var existingLatest = await _context.AssetDocuments
            .Where(x => x.AssetId == assetId && x.DocumentDefinitionId == documentDefinitionId && x.IsLatest && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        foreach (var oldDoc in existingLatest)
        {
            oldDoc.MarkAsSuperseded();
            oldDoc.UpdatedBy = createdBy;
            oldDoc.UpdatedDate = DateTime.Now;
        }

        var entity = AssetDocumentEntity.Create(
            assetId,
            documentDefinitionId,
            displayOrder,
            remarks);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.AssetDocuments.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task UpdateDocumentBindingAsync(
        int documentId,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AssetDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"AssetDocument with ID {documentId} not found");
        }

        entity.LinkDocumentBinding(documentBindingId);
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AssetDocumentEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.AssetDocuments
            .AsNoTracking()
            .Include(x => x.DocumentDefinition)
            .Include(x => x.DocumentBinding)
                .ThenInclude(db => db!.Document)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && !x.MarkedForDeletion, cancellationToken);
    }

    public async Task<List<AssetDocumentEntity>> GetLatestByAssetIdAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AssetDocuments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.DocumentDefinition)
            .Include(x => x.DocumentBinding)
                .ThenInclude(db => db!.Document)
            .Where(x => x.AssetId == assetId
                        && x.IsLatest
                        && x.IsActive
                        && !x.MarkedForDeletion)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.DocumentDefinitionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AssetDocumentEntity>> GetLatestByAssetIdIncludingInactiveAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AssetDocuments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.DocumentDefinition)
            .Include(x => x.DocumentBinding)
                .ThenInclude(db => db!.Document)
            .Where(x => x.AssetId == assetId
                        && x.IsLatest)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.DocumentDefinitionId)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsSupersededAsync(
        int documentId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AssetDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"AssetDocument with ID {documentId} not found");
        }

        if (!entity.IsLatest)
        {
            throw new ArgumentException(
                $"AssetDocument with ID {documentId} is a superseded version and cannot be replaced.",
                nameof(documentId));
        }

        entity.MarkAsSuperseded();
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        int documentId,
        int? displayOrder,
        string? remarks,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AssetDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"AssetDocument with ID {documentId} not found");
        }

        entity.SetDisplayOrder(displayOrder);
        entity.SetRemarks(remarks);
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ToggleEnabledAsync(
        int documentId,
        bool isEnabled,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AssetDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"AssetDocument with ID {documentId} not found");
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

    public async Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AssetDocuments
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"AssetDocument with ID {id} not found");
        }

        entity.MarkForDeletion();
        entity.UpdatedBy = deletedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
