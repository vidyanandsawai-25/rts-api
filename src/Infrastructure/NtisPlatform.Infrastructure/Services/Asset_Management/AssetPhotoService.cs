using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services.Asset_Management;

/// <summary>
/// Service for AMS.AssetPhoto operations.
/// </summary>
public class AssetPhotoService : IAssetPhotoService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public AssetPhotoService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateAsync(
        int assetId,
        int photoTypeId,
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

        var photoTypeExists = await _context.AssetPhotoTypeMaster
            .AsNoTracking()
            .AnyAsync(x => x.Id == photoTypeId && x.IsActive, cancellationToken);

        if (!photoTypeExists)
        {
            throw new ArgumentException($"Photo type with ID {photoTypeId} not found", nameof(photoTypeId));
        }

        var entity = AssetPhotoEntity.Create(
            assetId,
            photoTypeId,
            null,
            displayOrder,
            remarks);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.AssetPhotos.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task UpdateDocumentBindingAsync(
        int photoId,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AssetPhotos
            .FirstOrDefaultAsync(x => x.Id == photoId && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new AssetPhotoNotFoundException(photoId);
        }

        entity.LinkDocumentBinding(documentBindingId);
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AssetPhotoEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.AssetPhotos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && !x.MarkedForDeletion, cancellationToken);
    }

    public async Task<List<AssetPhotoEntity>> GetLatestByAssetIdAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AssetPhotos
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.PhotoType)
            .Include(x => x.DocumentBinding)
                .ThenInclude(db => db!.Document)
            .Where(x => x.AssetId == assetId
                        && x.IsLatest
                        && x.IsActive
                        && !x.MarkedForDeletion)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.PhotoTypeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AssetPhotoEntity>> GetLatestByAssetIdIncludingInactiveAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AssetPhotos
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.PhotoType)
            .Include(x => x.DocumentBinding)
                .ThenInclude(db => db!.Document)
            .Where(x => x.AssetId == assetId
                        && x.SubUnitsDetailsId == null
                        && x.IsLatest)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.PhotoTypeId)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsSupersededAsync(
        int photoId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AssetPhotos
            .FirstOrDefaultAsync(x => x.Id == photoId && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new AssetPhotoNotFoundException(photoId);
        }

        if (!entity.IsLatest)
        {
            throw new ArgumentException(
                $"AssetPhoto with ID {photoId} is a superseded version and cannot be replaced. Replace the current photo instead.",
                nameof(photoId));
        }

        entity.MarkAsSuperseded();
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        int photoId,
        int? displayOrder,
        string? remarks,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AssetPhotos
            .FirstOrDefaultAsync(x => x.Id == photoId && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new AssetPhotoNotFoundException(photoId);
        }

        entity.SetDisplayOrder(displayOrder);
        entity.SetRemarks(remarks);
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ToggleEnabledAsync(
        int photoId,
        bool isEnabled,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AssetPhotos
            .FirstOrDefaultAsync(x => x.Id == photoId, cancellationToken);

        if (entity == null)
        {
            throw new AssetPhotoNotFoundException(photoId);
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
        var entity = await _context.AssetPhotos
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new AssetPhotoNotFoundException(id);
        }

        entity.MarkForDeletion();
        entity.UpdatedBy = deletedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
