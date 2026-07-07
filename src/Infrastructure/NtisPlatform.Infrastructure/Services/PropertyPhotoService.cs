using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for PTIS.PropertyPhoto operations.
/// SEPARATE from Document service - persists the business row only.
/// </summary>
public class PropertyPhotoService : IPropertyPhotoService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public PropertyPhotoService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateAsync(
        int propertyId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        // Validate PropertyId exists
        var propertyExists = await _context.PropertyMast
            .AnyAsync(x => x.Id == propertyId && x.IsActive && !x.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
        {
            throw new ArgumentException($"Property with ID {propertyId} not found", nameof(propertyId));
        }

        // Validate PhotoTypeId exists
        var photoTypeExists = await _context.PropertyPhotoTypes
            .AnyAsync(x => x.Id == photoTypeId && x.IsActive, cancellationToken);

        if (!photoTypeExists)
        {
            throw new ArgumentException($"Photo type with ID {photoTypeId} not found", nameof(photoTypeId));
        }

        // Create entity without DocumentBinding (linked later once the binding exists)
        var entity = PropertyPhotoEntity.Create(
            propertyId,
            photoTypeId,
            displayOrder,
            remarks);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.PropertyPhotos.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task UpdateDocumentBindingAsync(
        int propertyPhotoId,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PropertyPhotos
            .FirstOrDefaultAsync(x => x.Id == propertyPhotoId && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new PropertyPhotoNotFoundException(propertyPhotoId);
        }

        entity.LinkDocumentBinding(documentBindingId);
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        // Sync ReferenceTableId on DocumentBinding
        var binding = await _context.DocumentBindings
            .FirstOrDefaultAsync(db => db.Id == documentBindingId, cancellationToken);
        if (binding != null && (binding.ReferenceTableId == null || binding.ReferenceTableId == 0))
        {
            binding.ReferenceTableId = propertyPhotoId;
            binding.ReferenceTableIdGuid = null;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PropertyPhotoEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.PropertyPhotos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && !x.MarkedForDeletion, cancellationToken);
    }

    public async Task<List<PropertyPhotoEntity>> GetLatestByPropertyIdAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PropertyPhotos
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.PhotoType)
            .Include(x => x.DocumentBinding)
                .ThenInclude(db => db!.Document)
            .Where(x => x.PropertyId == propertyId
                        && x.IsLatest
                        && x.IsActive
                        && !x.MarkedForDeletion
                        && (x.DocumentBinding == null
                            || (x.DocumentBinding.IsActive
                                && !x.DocumentBinding.MarkedForDeletion
                                && x.DocumentBinding.Document != null
                                && x.DocumentBinding.Document.IsActive
                                && !x.DocumentBinding.Document.MarkedForDeletion)))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.PhotoTypeId)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsSupersededAsync(
        int propertyPhotoId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PropertyPhotos
            .FirstOrDefaultAsync(x => x.Id == propertyPhotoId && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new PropertyPhotoNotFoundException(propertyPhotoId);
        }

        if (!entity.IsLatest)
        {
            throw new ArgumentException(
                $"PropertyPhoto with ID {propertyPhotoId} is a superseded version and cannot be replaced. Replace the current photo instead.",
                nameof(propertyPhotoId));
        }

        entity.MarkAsSuperseded();
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreFromSupersedingAsync(
        int id,
        int restoredBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PropertyPhotos
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new PropertyPhotoNotFoundException(id);
        }

        // Restore the photo to latest state (undo superseding)
        entity.RestoreFromSuperseding();
        entity.UpdatedBy = restoredBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PropertyPhotos
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new PropertyPhotoNotFoundException(id);
        }

        entity.MarkForDeletion();
        entity.UpdatedBy = deletedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
