using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.Property;

/// <summary>
/// Implementation of the "Residential Society and Wing Registration" use-case for the Property aggregate.
/// Owns: aggregate-invariant enforcement, Wing FK validation, the get-or-create society decision
/// (including the parent-id fallback lookup to prevent duplicate rows in legacy data), the full
/// transaction boundary around the two-save create-then-link flow, and the "empty DTO when society
/// is missing" decision.
/// Persistence is delegated to <see cref="IPropertySocietyRepository"/>, master checks to
/// <see cref="IMasterRepository"/>, saving / transactions to <see cref="IUnitOfWork"/>, and
/// aggregate invariants to <see cref="IPropertyMutationInvariantPolicy"/>.
/// </summary>
public class PropertySocietyService : IPropertySocietyService
{
    private readonly IPropertySocietyRepository _repository;
    private readonly IMasterRepository _masterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPropertyMutationInvariantPolicy _invariantPolicy;

    public PropertySocietyService(
        IPropertySocietyRepository repository,
        IMasterRepository masterRepository,
        IUnitOfWork unitOfWork,
        IPropertyMutationInvariantPolicy invariantPolicy)
    {
        _repository = repository;
        _masterRepository = masterRepository;
        _unitOfWork = unitOfWork;
        _invariantPolicy = invariantPolicy;
    }

    public async Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        // Repository returns null for two distinct cases:
        //   (a) property not found  → service propagates null → controller returns 404.
        //   (b) property found but no society yet → service returns an empty DTO.
        var result = await _repository.GetSocietyDetailsAsync(propertyId, cancellationToken);
        if (result != null) return result;

        // Distinguish the two null cases.
        if (!await _repository.PropertyExistsAsync(propertyId, cancellationToken))
            return null; // (a) property not found

        // (b) Property exists, society row not yet created → return an empty projection.
        return new PropertySocietyDetailsDto { PropertyId = propertyId, SocietyDetailId = null };
    }

    public async Task<PropertySocietyDetailsDto?> UpdateSocietyDetailsAsync(
        int propertyId,
        UpdatePropertySocietyDetailsDto dto,
        CancellationToken cancellationToken = default)
    {
        // A missing property is reported as null (→ 404).
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Enforce all Property aggregate write invariants before any state change.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        // Wing FK validation (business rule; message preserved for the API contract).
        if (dto.WingId.HasValue && !await _masterRepository.WingExistsAsync(dto.WingId.Value, cancellationToken))
            throw new PropertyValidationException($"Wing with ID {dto.WingId.Value} does not exist or is inactive.");

        // Single timestamp: consistent across all entity fields in this operation.
        var now = DateTime.Now;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // ── Locate the existing society child row ────────────────────────────────
            // Step 1: try the FK stored on the parent.
            SocietyDetailsEntity? society = null;
            if (property.SocietyDetailId.HasValue)
                society = await _repository.GetSocietyByIdAsync(property.SocietyDetailId.Value, cancellationToken);

            // Step 2: FK was null or pointed at a deleted/stale row — fall back to a
            //         lookup by PropertyId so we never create a duplicate child in legacy
            //         or partially-migrated data where the parent FK was never set.
            if (society == null)
                society = await _repository.GetSocietyByPropertyIdAsync(propertyId, cancellationToken);

            // Step 3: still nothing → create a new row.
            bool isNew = society == null;
            if (isNew)
            {
                society = new SocietyDetailsEntity
                {
                    PropertyId = propertyId,
                    IsActive = true,
                    CreatedDate = now
                };
                _repository.AddSociety(society);
            }

            // ── Apply all editable fields in one place ───────────────────────────────
            // society is guaranteed non-null here: either it was found above or we just created it.
            ApplySocietyFields(society!, dto, now);

            // ── First save: flushes the insert (assigns DB-generated PK) and updates ─
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ── Second save: link the parent's FK to the newly created child ──────────
            // Both saves are inside the same transaction, so a failure in either rolls
            // back the entire operation — no orphaned child rows.
            if (isNew || property.SocietyDetailId != society!.Id)
            {
                property.SocietyDetailId = society!.Id;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        // Re-read via the read path (AsNoTracking projection) after the transaction commits.
        return await _repository.GetSocietyDetailsAsync(propertyId, cancellationToken)
               ?? new PropertySocietyDetailsDto { PropertyId = propertyId, SocietyDetailId = null };
    }

    // ── Private helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Copies every editable society field from the DTO onto the entity in a single place.
    /// Using one <paramref name="now"/> value guarantees a consistent timestamp across all
    /// fields within the same operation.
    /// </summary>
    private static void ApplySocietyFields(
        SocietyDetailsEntity society,
        UpdatePropertySocietyDetailsDto dto,
        DateTime now)
    {
        society.WingId = dto.WingId;
        society.WingName = dto.WingName;
        society.SocietyName = dto.SocietyName;
        society.SocietyAddress = dto.SocietyAddress;
        society.SecretaryName = dto.SecretaryName;
        society.ManagerName = dto.ManagerName;
        society.LandOwnerName = dto.LandOwnerName;
        society.BuilderName = dto.BuilderName;
        society.SocietyNameEnglish = dto.SocietyNameEnglish;
        society.SocietyAddressEnglish = dto.SocietyAddressEnglish;
        society.SecretaryNameEnglish = dto.SecretaryNameEnglish;
        society.ManagerNameEnglish = dto.ManagerNameEnglish;
        society.LandOwnerNameEnglish = dto.LandOwnerNameEnglish;
        society.BuilderNameEnglish = dto.BuilderNameEnglish;
        society.ManagerMobileNo = dto.ManagerMobileNo;
        society.SecretaryMobileNo = dto.SecretaryMobileNo;
        society.SocietyEmailId = dto.SocietyEmailId;
        society.SecretaryEmailId = dto.SecretaryEmailId;
        society.ManagerEmailId = dto.ManagerEmailId;
        society.UpdatedDate = now;
    }
}
