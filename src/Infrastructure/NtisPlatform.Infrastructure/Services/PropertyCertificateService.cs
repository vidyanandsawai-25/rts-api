using MediatR;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Events;
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
/// <remarks>
/// Publishes <see cref="PropertyCertificateChangedEvent"/> after every mutation that changes a
/// tax-relevant field (CertificateNo/IssueDate/PropertyId/PropertyDetailsId/enabled-state/deletion)
/// so the RV-refresh-then-Retrospective-Tax-Engine pipeline runs — but only when the certificate's
/// TYPE has IsTaxable=1 (CC/OC/Electric Bill and any other type flagged taxable), regardless of
/// which higher-level orchestration (bulk-save, the single-certificate save endpoint, or any future
/// caller) invoked the change. IsTaxable is a separate flag from IsProtected (which only governs
/// whether the certificate TYPE master row can be deactivated/deleted, not tax triggering) — a
/// non-taxable type like "Possession Certificate" or "Index 2" must never re-run the tax engine.
/// This is the one place every mutation path converges, so it's the most reliable place to
/// guarantee the trigger actually fires for every taxable type.
/// </remarks>
public class PropertyCertificateService : IPropertyCertificateService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public PropertyCertificateService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    /// <summary>
    /// True when the given certificate type is flagged IsTaxable — the only condition under
    /// which a certificate mutation should trigger the RV-refresh-then-Retrospective-Tax-Engine
    /// pipeline. Always recalculates for a taxable type; there is no separate on/off toggle.
    /// </summary>
    private async Task<bool> ShouldRecalculateAsync(int certificateTypeId, bool isDelete, CancellationToken cancellationToken)
    {
        return await _context.PropertyCertificateTypeMasters
            .AsNoTracking()
            .Where(t => t.Id == certificateTypeId)
            .Select(t => t.IsTaxable)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(
        int? propertyId,
        int certificateTypeId,
        string? certificateNo,
        DateTime? issueDate,
        int createdBy,
        CancellationToken cancellationToken = default,
        int? propertyDetailsId = null,
        bool suppressRecalculation = false,
        string entityType = "P",
        int? societyDetailId = null,
        int? wingDetailId = null)
    {
        // Validate PropertyId exists -- only meaningful when EntityType is 'P' ('S'/'W' scoped
        // certificates have no single PropertyId to validate).
        if (propertyId.HasValue)
        {
            var propertyExists = await _context.PropertyMast
                .AnyAsync(x => x.Id == propertyId.Value && x.IsActive, cancellationToken);

            if (!propertyExists)
            {
                throw new PropertyNotFoundException(propertyId.Value);
            }
        }

        // Validate CertificateTypeId exists
        var certificateTypeExists = await _context.PropertyCertificateTypeMasters
            .AnyAsync(x => x.Id == certificateTypeId && x.IsActive, cancellationToken);

        if (!certificateTypeExists)
        {
            throw new CertificateTypeNotFoundException(certificateTypeId);
        }

        if (propertyDetailsId.HasValue)
        {
            var floorExists = await _context.PropertyDetails
                .AnyAsync(x => x.Id == propertyDetailsId.Value && x.PropertyId == propertyId
                    && x.IsActive && !x.MarkedForDeletion, cancellationToken);

            if (!floorExists)
            {
                throw new ArgumentException(
                    $"PropertyDetails {propertyDetailsId.Value} does not belong to property {propertyId}, or is inactive/deleted.",
                    nameof(propertyDetailsId));
            }
        }

        // Create entity without DocumentBinding
        var entity = PropertyCertificateEntity.Create(
            propertyId,
            certificateTypeId,
            certificateNo,
            issueDate,
            propertyDetailsId,
            entityType,
            societyDetailId,
            wingDetailId);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.PropertyCertificates.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // No single PropertyId to recalculate for on a Society/Wing-scoped row -- callers that
        // create those (EntityType 'S'/'W') resolve every member property themselves and publish
        // one event per property (see PropertyCertificateApplicationService.SaveCertificateAsync).
        if (propertyId.HasValue && !suppressRecalculation && await ShouldRecalculateAsync(certificateTypeId, isDelete: false, cancellationToken))
        {
            await _publisher.Publish(new PropertyCertificateChangedEvent(propertyId.Value, createdBy), cancellationToken);
        }

        return entity.Id;
    }

    public async Task<int> CreateWithDocumentAsync(
        int? propertyId,
        int certificateTypeId,
        int documentBindingId,
        string? certificateNo,
        DateTime? issueDate,
        int createdBy,
        CancellationToken cancellationToken = default,
        int? propertyDetailsId = null,
        string entityType = "P",
        int? societyDetailId = null,
        int? wingDetailId = null)
    {
        // Validate PropertyId exists -- only meaningful when EntityType is 'P'.
        if (propertyId.HasValue)
        {
            var propertyExists = await _context.PropertyMast
                .AnyAsync(x => x.Id == propertyId.Value && x.IsActive, cancellationToken);

            if (!propertyExists)
            {
                throw new PropertyNotFoundException(propertyId.Value);
            }
        }

        // Validate CertificateTypeId exists
        var certificateTypeExists = await _context.PropertyCertificateTypeMasters
            .AnyAsync(x => x.Id == certificateTypeId && x.IsActive, cancellationToken);

        if (!certificateTypeExists)
        {
            throw new CertificateTypeNotFoundException(certificateTypeId);
        }

        if (propertyDetailsId.HasValue)
        {
            var floorExists = await _context.PropertyDetails
                .AnyAsync(x => x.Id == propertyDetailsId.Value && x.PropertyId == propertyId
                    && x.IsActive && !x.MarkedForDeletion, cancellationToken);

            if (!floorExists)
            {
                throw new ArgumentException(
                    $"PropertyDetails {propertyDetailsId.Value} does not belong to property {propertyId}, or is inactive/deleted.",
                    nameof(propertyDetailsId));
            }
        }

        // Use optimized factory method that includes DocumentBindingId
        var entity = PropertyCertificateEntity.CreateWithDocument(
            propertyId,
            certificateTypeId,
            documentBindingId,
            certificateNo,
            issueDate,
            propertyDetailsId,
            entityType,
            societyDetailId,
            wingDetailId);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.PropertyCertificates.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (propertyId.HasValue && await ShouldRecalculateAsync(certificateTypeId, isDelete: false, cancellationToken))
        {
            await _publisher.Publish(new PropertyCertificateChangedEvent(propertyId.Value, createdBy), cancellationToken);
        }

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

    public async Task<List<PropertyCertificateEntity>> GetByWingDetailIdAsync(
        int wingDetailId,
        PropertyCertificateIncludeOptions includeOptions,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PropertyCertificates
            .AsNoTracking();

        if (includeOptions != PropertyCertificateIncludeOptions.None)
        {
            query = query.AsSplitQuery();
        }

        query = ApplyIncludes(query, includeOptions);

        return await query
            .Where(x => x.EntityType == "W" && x.WingDetailId == wingDetailId && x.IsActive && !x.MarkedForDeletion)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PropertyCertificateEntity>> GetBySocietyDetailIdAsync(
        int societyDetailId,
        PropertyCertificateIncludeOptions includeOptions,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PropertyCertificates
            .AsNoTracking();

        if (includeOptions != PropertyCertificateIncludeOptions.None)
        {
            query = query.AsSplitQuery();
        }

        query = ApplyIncludes(query, includeOptions);

        return await query
            .Where(x => x.EntityType == "S" && x.SocietyDetailId == societyDetailId && x.IsActive && !x.MarkedForDeletion)
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
        CancellationToken cancellationToken = default,
        bool suppressRecalculation = false)
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

        // See CreateAsync's note: a Society/Wing-scoped row has no single PropertyId to
        // recalculate for, so it's skipped here -- the caller resolves member properties itself.
        if (entity.PropertyId.HasValue && !suppressRecalculation && await ShouldRecalculateAsync(entity.CertificateTypeId, isDelete: false, cancellationToken))
        {
            await _publisher.Publish(new PropertyCertificateChangedEvent(entity.PropertyId.Value, updatedBy), cancellationToken);
        }
    }

    public async Task ToggleEnabledAsync(
        int id,
        bool isEnabled,
        int updatedBy,
        CancellationToken cancellationToken = default,
        bool suppressRecalculation = false)
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

        // Enabling behaves like a save (RECALCULATE_ON_CERTIFICATE_SAVE); disabling removes the
        // certificate from tax consideration, so it's gated the same as a delete. See CreateAsync's
        // note on why a null PropertyId (Society/Wing scope) skips auto-publish here.
        if (entity.PropertyId.HasValue && !suppressRecalculation && await ShouldRecalculateAsync(entity.CertificateTypeId, isDelete: !isEnabled, cancellationToken))
        {
            await _publisher.Publish(new PropertyCertificateChangedEvent(entity.PropertyId.Value, updatedBy), cancellationToken);
        }
    }

    public async Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default,
        bool suppressRecalculation = false)
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

        // See CreateAsync's note on why a null PropertyId (Society/Wing scope) skips auto-publish.
        if (entity.PropertyId.HasValue && !suppressRecalculation && await ShouldRecalculateAsync(entity.CertificateTypeId, isDelete: true, cancellationToken))
        {
            await _publisher.Publish(new PropertyCertificateChangedEvent(entity.PropertyId.Value, deletedBy), cancellationToken);
        }
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
