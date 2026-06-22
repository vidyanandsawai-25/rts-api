using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for PTIS.PropertyCertificates operations.
/// SEPARATE from Document service.
/// </summary>
public class PropertyCertificateService : IPropertyCertificateService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public PropertyCertificateService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateAsync(
        int propertyId,
        int certificateTypeId,
        string? certificateNo,
        DateTime? issueDate,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        // Validate PropertyId exists
        var propertyExists = await _context.PropertyMast
            .AnyAsync(x => x.Id == propertyId && x.IsActive, cancellationToken);

        if (!propertyExists)
        {
            throw new PropertyNotFoundException(propertyId);
        }

        // Validate CertificateTypeId exists
        var certificateTypeExists = await _context.PropertyCertificateTypeMasters
            .AnyAsync(x => x.Id == certificateTypeId && x.IsActive, cancellationToken);

        if (!certificateTypeExists)
        {
            throw new CertificateTypeNotFoundException(certificateTypeId);
        }

        // Create entity without DocumentBinding
        var entity = PropertyCertificateEntity.Create(
            propertyId,
            certificateTypeId,
            certificateNo,
            issueDate);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.PropertyCertificates.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task<int> CreateWithDocumentAsync(
        int propertyId,
        int certificateTypeId,
        int documentBindingId,
        string? certificateNo,
        DateTime? issueDate,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        // Validate PropertyId exists
        var propertyExists = await _context.PropertyMast
            .AnyAsync(x => x.Id == propertyId && x.IsActive, cancellationToken);

        if (!propertyExists)
        {
            throw new PropertyNotFoundException(propertyId);
        }

        // Validate CertificateTypeId exists
        var certificateTypeExists = await _context.PropertyCertificateTypeMasters
            .AnyAsync(x => x.Id == certificateTypeId && x.IsActive, cancellationToken);

        if (!certificateTypeExists)
        {
            throw new CertificateTypeNotFoundException(certificateTypeId);
        }

        // Use optimized factory method that includes DocumentBindingId
        var entity = PropertyCertificateEntity.CreateWithDocument(
            propertyId,
            certificateTypeId,
            documentBindingId,
            certificateNo,
            issueDate);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.PropertyCertificates.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task UpdateDocumentBindingAsync(
        int propertyCertificateId,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PropertyCertificates
            .FirstOrDefaultAsync(x => x.Id == propertyCertificateId && x.IsActive, cancellationToken);

        if (entity == null)
        {
            throw new PropertyCertificateNotFoundException(propertyCertificateId);
        }

        entity.LinkDocumentBinding(documentBindingId);
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PropertyCertificateEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.PropertyCertificates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && !x.MarkedForDeletion, cancellationToken);
    }

    public async Task<PropertyCertificateEntity?> GetByIdAsync(
        int id,
        PropertyCertificateIncludeOptions includeOptions,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PropertyCertificates
            .AsNoTracking();

        // Apply includes based on flags
        query = ApplyIncludes(query, includeOptions);

        return await query
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && !x.MarkedForDeletion, cancellationToken);
    }

    public async Task<List<PropertyCertificateEntity>> GetByPropertyIdAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        // Legacy method: maintains backward compatibility with full eager loading
        return await GetByPropertyIdAsync(propertyId, PropertyCertificateIncludeOptions.All, cancellationToken);
    }

    public async Task<List<PropertyCertificateEntity>> GetByPropertyIdAsync(
        int propertyId,
        PropertyCertificateIncludeOptions includeOptions,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PropertyCertificates
            .AsNoTracking();

        // Use split query if loading multiple navigation properties for better performance
        if (includeOptions != PropertyCertificateIncludeOptions.None)
        {
            query = query.AsSplitQuery();
        }

        // Apply includes based on flags
        query = ApplyIncludes(query, includeOptions);

        return await query
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PropertyCertificateEntity>> GetByPropertyIdIncludingInactiveAsync(
        int propertyId,
        PropertyCertificateIncludeOptions includeOptions,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PropertyCertificates
            .AsNoTracking();

        // Use split query if loading multiple navigation properties for better performance
        if (includeOptions != PropertyCertificateIncludeOptions.None)
        {
            query = query.AsSplitQuery();
        }

        // Apply includes based on flags
        query = ApplyIncludes(query, includeOptions);

        return await query
            .Where(x => x.PropertyId == propertyId && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Applies Include statements to the query based on the specified options.
    /// Uses flags to provide flexible, composable loading strategies.
    /// </summary>
    private IQueryable<PropertyCertificateEntity> ApplyIncludes(
        IQueryable<PropertyCertificateEntity> query,
        PropertyCertificateIncludeOptions includeOptions)
    {
        if (includeOptions == PropertyCertificateIncludeOptions.None)
            return query;

        // Include CertificateType if requested
        if (includeOptions.HasFlag(PropertyCertificateIncludeOptions.CertificateType))
        {
            query = query.Include(x => x.CertificateType);
        }

        // Include DocumentBinding if requested
        if (includeOptions.HasFlag(PropertyCertificateIncludeOptions.DocumentBinding))
        {
            // If Document is also requested, include it via ThenInclude
            if (includeOptions.HasFlag(PropertyCertificateIncludeOptions.Document))
            {
                query = query.Include(x => x.DocumentBinding)
                    .ThenInclude(db => db!.Document);
            }
            else
            {
                query = query.Include(x => x.DocumentBinding);
            }
        }

        return query;
    }

    public async Task UpdateAsync(
        int id,
        string? certificateNo,
        DateTime? issueDate,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PropertyCertificates
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new PropertyCertificateNotFoundException(id);
        }

        entity.UpdateDetails(certificateNo, issueDate);
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
        var entity = await _context.PropertyCertificates
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new PropertyCertificateNotFoundException(id);
        }

        if (isEnabled)
        {
            entity.Enable();
        }
        else
        {
            entity.Disable();
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
        var entity = await _context.PropertyCertificates
            .FirstOrDefaultAsync(x => x.Id == id && !x.MarkedForDeletion, cancellationToken);

        if (entity == null)
        {
            throw new PropertyCertificateNotFoundException(id);
        }

        entity.MarkForDeletion();
        entity.UpdatedBy = deletedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlinkDocumentBindingAsync(
        int propertyCertificateId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PropertyCertificates
            .FirstOrDefaultAsync(x => x.Id == propertyCertificateId && x.IsActive, cancellationToken);

        if (entity == null)
        {
            throw new PropertyCertificateNotFoundException(propertyCertificateId);
        }

        entity.UnlinkDocumentBinding();
        entity.UpdatedBy = updatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
