using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for PTIS.PropertyPhoto operations (supporting Property 'P', Society 'S', and Wing 'W').
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
        var propertyExists = await _context.PropertyMast
            .AnyAsync(x => x.Id == propertyId && x.IsActive && !x.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
        {
            throw new ArgumentException($"Property with ID {propertyId} not found", nameof(propertyId));
        }

        var photoTypeExists = await _context.PropertyPhotoTypes
            .AnyAsync(x => x.Id == photoTypeId && x.IsActive, cancellationToken);

        if (!photoTypeExists)
        {
            throw new ArgumentException($"Photo type with ID {photoTypeId} not found", nameof(photoTypeId));
        }

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

    public async Task<int> CreateWithDetailsAsync(
        int propertyId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var property = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.WingDetailId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
        {
            throw new ArgumentException($"Property with ID {propertyId} not found", nameof(propertyId));
        }

        var photoType = await _context.PropertyPhotoTypes
            .FirstOrDefaultAsync(x => x.Id == photoTypeId && x.IsActive, cancellationToken);

        if (photoType == null)
        {
            throw new ArgumentException($"Photo type with ID {photoTypeId} not found", nameof(photoTypeId));
        }

        int? wingDetailId = property.WingDetailId;
        int? societyDetailId = null;

        if (wingDetailId.HasValue)
        {
            var wingInfo = await _context.Set<WingDetailsMastEntity>()
                .Where(w => w.Id == wingDetailId.Value && w.IsActive && !w.MarkedForDeletion)
                .Select(w => new { w.SocietyDetailsMastId })
                .FirstOrDefaultAsync(cancellationToken);
            societyDetailId = wingInfo?.SocietyDetailsMastId;
        }
        else
        {
            societyDetailId = await _context.SocietyDetailsMast
                .Where(s => s.PropertyId == propertyId && s.IsActive && !s.MarkedForDeletion)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        string entityType = "P";
        int? targetSocietyDetailId = null;
        int? targetWingDetailId = null;

        if (string.Equals(photoType.PhotoScope, "WING", StringComparison.OrdinalIgnoreCase))
        {
            entityType = "W";
            targetWingDetailId = wingDetailId;
            targetSocietyDetailId = societyDetailId;
        }
        else if (string.Equals(photoType.PhotoScope, "SOCIETY", StringComparison.OrdinalIgnoreCase))
        {
            entityType = "S";
            targetSocietyDetailId = societyDetailId;
            targetWingDetailId = null;
        }
        else // PROPERTY scope
        {
            entityType = "P";
            targetWingDetailId = wingDetailId;
            targetSocietyDetailId = societyDetailId;
        }

        int? finalPropertyId = propertyId;

        var entity = PropertyPhotoEntity.CreateWithDetails(
            finalPropertyId,
            photoTypeId,
            entityType,
            targetSocietyDetailId,
            targetWingDetailId,
            documentBindingId: null,
            displayOrder: displayOrder,
            remarks: remarks);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.PropertyPhotos.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task<int> CreateForSocietyAsync(
        int societyId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var photoTypeExists = await _context.PropertyPhotoTypes
            .AnyAsync(x => x.Id == photoTypeId && x.IsActive, cancellationToken);

        if (!photoTypeExists)
        {
            throw new ArgumentException($"Photo type with ID {photoTypeId} not found", nameof(photoTypeId));
        }

        var entity = PropertyPhotoEntity.CreateForSociety(
            societyId,
            photoTypeId,
            displayOrder,
            remarks);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.PropertyPhotos.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task<int> CreateForWingAsync(
        int wingId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var photoTypeExists = await _context.PropertyPhotoTypes
            .AnyAsync(x => x.Id == photoTypeId && x.IsActive, cancellationToken);

        if (!photoTypeExists)
        {
            throw new ArgumentException($"Photo type with ID {photoTypeId} not found", nameof(photoTypeId));
        }

        var entity = PropertyPhotoEntity.CreateForWing(
            wingId,
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

        var binding = await _context.DocumentBindings
            .FirstOrDefaultAsync(db => db.Id == documentBindingId, cancellationToken);
        if (binding != null)
        {
            if (binding.ReferenceTableId == null || binding.ReferenceTableId == 0)
            {
                binding.ReferenceTableId = propertyPhotoId;
                binding.ReferenceTableIdGuid = null;
            }
            if (!string.IsNullOrWhiteSpace(binding.BindingPurpose))
            {
                entity.SetRemarks(binding.BindingPurpose);
            }
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
        // Resolve property's WingDetailId and SocietyDetailId (if any)
        var propertyInfo = await _context.PropertyMast
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.WingDetailId, p.Type })
            .FirstOrDefaultAsync(cancellationToken);

        int? wingDetailId = propertyInfo?.WingDetailId;
        string? propertyType = propertyInfo?.Type;
        int? societyDetailId = null;

        var planPhotoTypeId = await _context.PropertyPhotoTypes
            .Where(t => t.PhotoTypeCode == "PROPERTY_PLAN")
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (wingDetailId.HasValue)
        {
            var wingInfo = await _context.Set<WingDetailsMastEntity>()
                .Where(w => w.Id == wingDetailId.Value && w.IsActive && !w.MarkedForDeletion)
                .Select(w => new { w.SocietyDetailsMastId })
                .FirstOrDefaultAsync(cancellationToken);
            societyDetailId = wingInfo?.SocietyDetailsMastId;
        }
        else
        {
            societyDetailId = await _context.SocietyDetailsMast
                .Where(s => s.PropertyId == propertyId && s.IsActive && !s.MarkedForDeletion)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var wingDetailIds = new List<int>();
        if (societyDetailId.HasValue)
        {
            wingDetailIds = await _context.Set<WingDetailsMastEntity>()
                .Where(w => w.SocietyDetailsMastId == societyDetailId.Value && w.IsActive && !w.MarkedForDeletion)
                .Select(w => w.Id)
                .ToListAsync(cancellationToken);
        }

        return await _context.PropertyPhotos
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.PhotoType)
            .Include(x => x.DocumentBinding)
                .ThenInclude(db => db!.Document)
            .Include(x => x.WingDetail)
                .ThenInclude(w => w!.WingMaster)
            .Where(x => x.IsLatest
                        && x.IsActive
                        && !x.MarkedForDeletion
                        && (x.DocumentBinding == null
                            || (x.DocumentBinding.IsActive
                                && !x.DocumentBinding.MarkedForDeletion
                                && x.DocumentBinding.Document != null
                                && x.DocumentBinding.Document.IsActive
                                && !x.DocumentBinding.Document.MarkedForDeletion))
                        && (
                            x.PropertyId == propertyId
                            || (x.EntityType == "W" && (
                                (wingDetailId.HasValue && x.WingDetailId == wingDetailId.Value)
                                || (!wingDetailId.HasValue && societyDetailId.HasValue && x.WingDetailId.HasValue && wingDetailIds.Contains(x.WingDetailId.Value))
                            ))
                            || (x.EntityType == "S" && societyDetailId.HasValue && x.SocietyDetailId == societyDetailId.Value
                                // The shared PROPERTY_PLAN row carries no PropertyId -- every unit
                                // resolves it by matching its own Type against the stored Type.
                                && (!planPhotoTypeId.HasValue || x.PhotoTypeId != planPhotoTypeId.Value || x.Type == propertyType))
                            || ((x.EntityType == "P" || x.EntityType == null) && (
                                (wingDetailId.HasValue && x.WingDetailId == wingDetailId.Value)
                                || (societyDetailId.HasValue && x.SocietyDetailId == societyDetailId.Value)
                            ))
                        ))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.PhotoTypeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PropertyPhotoEntity>> GetLatestBySocietyIdAsync(
        int societyId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PropertyPhotos
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.PhotoType)
            .Include(x => x.DocumentBinding)
                .ThenInclude(db => db!.Document)
            .Where(x => x.EntityType == "S"
                        && x.SocietyDetailId == societyId
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

    public async Task<List<PropertyPhotoEntity>> GetLatestByWingIdAsync(
        int wingId,
        CancellationToken cancellationToken = default)
    {
        var matchingWingDetailIds = await _context.Set<WingDetailsMastEntity>()
            .Where(w => (w.Id == wingId || w.WingMasterId == wingId) && w.IsActive && !w.MarkedForDeletion)
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

        if (!matchingWingDetailIds.Contains(wingId))
        {
            matchingWingDetailIds.Add(wingId);
        }

        return await _context.PropertyPhotos
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.PhotoType)
            .Include(x => x.DocumentBinding)
                .ThenInclude(db => db!.Document)
            .Where(x => x.EntityType == "W"
                        && matchingWingDetailIds.Contains(x.WingDetailId ?? 0)
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
                $"PropertyPhoto with ID {propertyPhotoId} is a superseded version and cannot be replaced.",
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
        entity.UnlinkDocumentBinding();
        entity.UpdatedBy = deletedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
