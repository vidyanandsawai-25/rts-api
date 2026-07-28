using MediatR;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Events;
using NtisPlatform.Application.Interfaces.TaxEngine;
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
/// so the RV-refresh-then-Occupation-Tax pipeline runs — but only when the certificate's TYPE has
/// IsTaxable=1 (CC/OC/Electric Bill and any other type flagged taxable), regardless of which
/// higher-level orchestration (bulk-save, the single-certificate save endpoint, or any future
/// caller) invoked the change. IsTaxable is a separate flag from IsProtected (which only governs
/// whether the certificate TYPE master row can be deactivated/deleted, not tax triggering) — a
/// non-taxable type like "Possession Certificate" or "Index 2" must never re-run Occupation Tax.
/// This is the one place every mutation path converges, so it's the most reliable place to
/// guarantee the trigger actually fires for every taxable type.
/// </remarks>
public class PropertyCertificateService : IPropertyCertificateService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ICertificateTaxGuidelineReaderService _guidelineReader;

    public PropertyCertificateService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ICertificateTaxGuidelineReaderService guidelineReader)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _guidelineReader = guidelineReader;
    }

    /// <summary>
    /// True when the given certificate type is flagged IsTaxable — the only condition under
    /// which a certificate mutation should trigger the RV-refresh-then-Occupation-Tax pipeline.
    /// </summary>
    private async Task<bool> IsTaxableCertificateTypeAsync(int certificateTypeId, CancellationToken cancellationToken)
    {
        return await _context.PropertyCertificateTypeMasters
            .AsNoTracking()
            .Where(t => t.Id == certificateTypeId)
            .Select(t => t.IsTaxable)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// True when both the certificate type is taxable AND PTIS.CertificateTaxGuideline's
    /// RECALCULATE_ON_CERTIFICATE_SAVE/_DELETE toggle allows recalculation for this kind of change.
    /// </summary>
    private async Task<bool> ShouldRecalculateAsync(int certificateTypeId, bool isDelete, CancellationToken cancellationToken)
    {
        if (!await IsTaxableCertificateTypeAsync(certificateTypeId, cancellationToken))
        {
            return false;
        }

        var guideline = await _guidelineReader.GetActiveSettingsAsync(cancellationToken);
        return isDelete ? guideline.RecalculateOnDelete : guideline.RecalculateOnSave;
    }

    public async Task<int> CreateAsync(
        int propertyId,
        int certificateTypeId,
        string? certificateNo,
        DateTime? issueDate,
        int createdBy,
        CancellationToken cancellationToken = default,
        int? propertyDetailsId = null,
        bool suppressRecalculation = false)
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
            propertyDetailsId);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.PropertyCertificates.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!suppressRecalculation && await ShouldRecalculateAsync(certificateTypeId, isDelete: false, cancellationToken))
        {
            await _publisher.Publish(new PropertyCertificateChangedEvent(propertyId, createdBy), cancellationToken);
        }

        return entity.Id;
    }

    public async Task<int> CreateWithDocumentAsync(
        int propertyId,
        int certificateTypeId,
        int documentBindingId,
        string? certificateNo,
        DateTime? issueDate,
        int createdBy,
        CancellationToken cancellationToken = default,
        int? propertyDetailsId = null)
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
            propertyDetailsId);

        entity.CreatedBy = createdBy;
        entity.CreatedDate = DateTime.Now;

        _context.PropertyCertificates.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (await ShouldRecalculateAsync(certificateTypeId, isDelete: false, cancellationToken))
        {
            await _publisher.Publish(new PropertyCertificateChangedEvent(propertyId, createdBy), cancellationToken);
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

        if (!suppressRecalculation && await ShouldRecalculateAsync(entity.CertificateTypeId, isDelete: false, cancellationToken))
        {
            await _publisher.Publish(new PropertyCertificateChangedEvent(entity.PropertyId, updatedBy), cancellationToken);
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
        // certificate from tax consideration, so it's gated the same as a delete.
        if (!suppressRecalculation && await ShouldRecalculateAsync(entity.CertificateTypeId, isDelete: !isEnabled, cancellationToken))
        {
            await _publisher.Publish(new PropertyCertificateChangedEvent(entity.PropertyId, updatedBy), cancellationToken);
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

        if (!suppressRecalculation && await ShouldRecalculateAsync(entity.CertificateTypeId, isDelete: true, cancellationToken))
        {
            await _publisher.Publish(new PropertyCertificateChangedEvent(entity.PropertyId, deletedBy), cancellationToken);
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
