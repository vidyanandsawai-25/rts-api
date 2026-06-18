using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.Property;

/// <summary>
/// Implementation of the "Owner and Occupier Registration" use-case for the Property aggregate.
/// Owns existence handling, the upsert decision for the assessment row and the transaction boundary;
/// persistence is delegated to <see cref="IPropertyKycRepository"/>, saving to <see cref="IUnitOfWork"/>,
/// and aggregate invariants to <see cref="IPropertyMutationInvariantPolicy"/>.
/// </summary>
public class PropertyKycService : IPropertyKycService
{
    private readonly IPropertyKycRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPropertyMutationInvariantPolicy _invariantPolicy;

    public PropertyKycService(
        IPropertyKycRepository repository,
        IUnitOfWork unitOfWork,
        IPropertyMutationInvariantPolicy invariantPolicy)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _invariantPolicy = invariantPolicy;
    }

    public Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _repository.GetKycDetailsAsync(propertyId, cancellationToken);

    public async Task<PropertyKycDetailsDto?> UpdateKycDetailsAsync(
        int propertyId,
        UpdatePropertyKycDetailsDto dto,
        CancellationToken cancellationToken = default)
    {
        // A missing property is reported as null (→ 404).
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Enforce all Property aggregate write invariants before any state change.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        // Single timestamp: consistent across all entity fields in this operation.
        var now = DateTime.Now;

        // Wrap in a transaction: PropertyMast and potentially a new assessment row both save
        // in this operation; both must succeed or roll back atomically (Critical #4 — aggregate invariants).
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            ApplyKycFields(property, dto, now);
            await UpsertAssessmentAsync(propertyId, dto, now, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return await _repository.GetKycDetailsAsync(propertyId, cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Copies every editable KYC field from the DTO onto the entity in a single place.
    /// Using one <paramref name="now"/> value ensures a consistent timestamp.
    /// </summary>
    private static void ApplyKycFields(
        PropertyEntity property,
        UpdatePropertyKycDetailsDto dto,
        DateTime now)
    {
        property.OwnerTitle = dto.OwnerTitle;
        property.OwnerName = dto.OwnerName;
        property.OwnerTitleEnglish = dto.OwnerTitleEnglish;
        property.OwnerNameEnglish = dto.OwnerNameEnglish;
        property.OccupierTitle = dto.OccupierTitle;
        property.OccupierName = dto.OccupierName;
        property.OccupierTitleEnglish = dto.OccupierTitleEnglish;
        property.OccupierNameEnglish = dto.OccupierNameEnglish;
        property.Address = dto.Address;
        property.Location = dto.Location;
        property.AddressEnglish = dto.AddressEnglish;
        property.LocationEnglish = dto.LocationEnglish;
        property.FlatOrShopName = dto.FlatOrShopName;
        property.FlatOrShopNameEnglish = dto.FlatOrShopNameEnglish;
        property.FlatOrShopNo = dto.FlatOrShopNo;
        property.FlatOrShopNoEnglish = dto.FlatOrShopNoEnglish;
        property.MobileNo = dto.MobileNo;
        property.AlternateMobileNo = dto.AlternateMobileNo;
        property.EmailId = dto.EmailId;
        property.PinCode = dto.PinCode;
        property.UpdatedDate = now;
    }

    /// <summary>Updates the assessment row's owner-type/aadhar in place, or inserts one only when that data is supplied.</summary>
    private async Task UpsertAssessmentAsync(
        int propertyId,
        UpdatePropertyKycDetailsDto dto,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var assessmentId = await _repository.GetFirstAssessmentIdAsync(propertyId, cancellationToken);
        bool hasAssessmentData = dto.OwnerTypeId.HasValue || dto.AdharCardNo != null;

        if (assessmentId > 0)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId, cancellationToken);
            if (assessment != null)
            {
                assessment.OwnerTypeId = dto.OwnerTypeId;
                assessment.AdharCardNo = dto.AdharCardNo;
                assessment.UpdatedDate = now;
            }
        }
        else if (hasAssessmentData)
        {
            await _repository.AddAssessmentAsync(new PropertyAssessmentEntity
            {
                PropertyId = propertyId,
                OwnerTypeId = dto.OwnerTypeId,
                AdharCardNo = dto.AdharCardNo,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = now
            }, cancellationToken);
        }
    }
}
