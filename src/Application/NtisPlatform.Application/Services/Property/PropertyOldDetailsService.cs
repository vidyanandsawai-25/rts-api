using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.Property;

/// <summary>
/// Implementation of the "Historical Property Data" use-case for the Property aggregate
/// (old property details, old taxes, and historical floor sub-sections).
/// Owns aggregate-invariant enforcement, validation, the upsert decisions and the transaction boundary;
/// persistence is delegated to <see cref="IPropertyOldDetailsRepository"/>, saving to
/// <see cref="IUnitOfWork"/>, and aggregate invariants to <see cref="IPropertyMutationInvariantPolicy"/>.
/// </summary>
public partial class PropertyOldDetailsService : IPropertyOldDetailsService
{
    private readonly IPropertyOldDetailsRepository _repository;
    private readonly IMasterRepository _masterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPropertyMutationInvariantPolicy _invariantPolicy;

    public PropertyOldDetailsService(
        IPropertyOldDetailsRepository repository,
        IMasterRepository masterRepository,
        IUnitOfWork unitOfWork,
        IPropertyMutationInvariantPolicy invariantPolicy)
    {
        _repository = repository;
        _masterRepository = masterRepository;
        _unitOfWork = unitOfWork;
        _invariantPolicy = invariantPolicy;
    }

    // ---- Old Property Details sub-section ----

    public Task<PropertyOldDetailsDto?> GetOldDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _repository.GetOldDetailsAsync(propertyId, cancellationToken);

    public async Task<PropertyOldDetailsDto?> UpdateOldDetailsAsync(int propertyId, UpdatePropertyOldDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // A missing property is reported as null (→ 404). Check before opening a transaction.
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Enforce all Property aggregate write invariants before any state change.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        // Single timestamp: consistent across all entity fields in this operation.
        var now = DateTime.Now;

        // EnsurePropertyMastOldAsync performs an intermediate save to get the new row's PK, then
        // links it back to the parent. Both that mid-save and the final save below are wrapped in
        // one transaction so a failure at either point rolls back the entire operation atomically.
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Get or create the PropertyMastOld root, linking it back to the property when created.
            int propertyMastOldId = await EnsurePropertyMastOldAsync(property, now, cancellationToken);

            // Update PropertyMastOld fields (only those supplied).
            var oldMastData = await _repository.GetPropertyMastOldByIdAsync(propertyMastOldId, cancellationToken);
            if (oldMastData != null)
            {
                if (dto.OldWardNo != null) oldMastData.OldWardNo = dto.OldWardNo;
                if (dto.OldPropertyNo != null) oldMastData.OldPropertyNo = dto.OldPropertyNo;
                if (dto.OldPartitionNo != null) oldMastData.OldPartitionNo = dto.OldPartitionNo;
                if (dto.OldEgovNo != null) oldMastData.OldEgovNo = dto.OldEgovNo;
                if (dto.OldPlotArea.HasValue) oldMastData.OldPlotArea = dto.OldPlotArea;
                if (dto.OldPlotNo != null) oldMastData.OldPlotNo = dto.OldPlotNo;
                if (dto.OldRV.HasValue) oldMastData.OldRV = dto.OldRV;
                if (dto.OldALV.HasValue) oldMastData.OldALV = dto.OldALV;
                if (dto.OldTotalTax.HasValue) oldMastData.OldTotalTax = dto.OldTotalTax;
                if (dto.OldZoneNo != null) oldMastData.OldZoneNo = dto.OldZoneNo;
                if (dto.OldConstructionArea != null) oldMastData.OldConstructionArea = dto.OldConstructionArea;
                if (dto.OldGeneralTax != null) oldMastData.OldGeneralTax = dto.OldGeneralTax;
                if (dto.OldCSN != null) oldMastData.OldCSN = dto.OldCSN;
                oldMastData.UpdatedDate = now;
            }

            // Upsert the first PropertyDetailsOld row.
            var oldDetailsId = await _repository.GetFirstOldDetailsIdAsync(propertyMastOldId, cancellationToken);

            // Validate any master-data references supplied in this request.
            if (dto.OldFloorId.HasValue || dto.OldConstructionTypeId.HasValue || dto.OldTypeOfUseId.HasValue)
                await ValidateFloorReferencesAsync(dto.OldFloorId, null, dto.OldConstructionTypeId, dto.OldTypeOfUseId, null, cancellationToken);

            bool hasOldDetailsData = dto.OldConstructionYear != null || dto.OldCarpetAreaSqFeet.HasValue ||
                                     dto.OldCarpetAreaSqMeter.HasValue ||
                                     dto.OldConstructionTypeId.HasValue || dto.OldTypeOfUseId.HasValue;
            if (oldDetailsId > 0)
            {
                var oldDetailsData = await _repository.GetOldDetailsByIdAsync(oldDetailsId, cancellationToken);
                if (oldDetailsData != null)
                {
                    if (dto.OldConstructionYear != null) oldDetailsData.OldConstructionYear = dto.OldConstructionYear;
                    if (dto.OldCarpetAreaSqFeet.HasValue) oldDetailsData.OldCarpetAreaSqFeet = dto.OldCarpetAreaSqFeet;
                    if (dto.OldCarpetAreaSqMeter.HasValue) oldDetailsData.OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter;
                    if (dto.OldConstructionTypeId.HasValue) oldDetailsData.OldConstructionTypeId = dto.OldConstructionTypeId.Value;
                    if (dto.OldTypeOfUseId.HasValue) oldDetailsData.OldTypeOfUseId = dto.OldTypeOfUseId.Value;
                    if (dto.OldFloorId.HasValue) oldDetailsData.OldFloorId = dto.OldFloorId.Value;
                    oldDetailsData.UpdatedDate = now;
                }
            }
            else if (hasOldDetailsData)
            {
                // Validate required fields before insert.
                if (!dto.OldFloorId.HasValue)
                    throw new PropertyValidationException("OldFloorId is required.");
                if (!dto.OldConstructionTypeId.HasValue)
                    throw new PropertyValidationException("OldConstructionTypeId is required.");
                if (!dto.OldTypeOfUseId.HasValue)
                    throw new PropertyValidationException("OldTypeOfUseId is required.");

                await _repository.AddOldDetailsAsync(new PropertyDetailsOldEntity
                {
                    PropertyMastOldId = propertyMastOldId,
                    OldConstructionYear = dto.OldConstructionYear,
                    OldCarpetAreaSqFeet = dto.OldCarpetAreaSqFeet,
                    OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter,
                    OldConstructionTypeId = dto.OldConstructionTypeId.Value,
                    OldTypeOfUseId = dto.OldTypeOfUseId.Value,
                    OldFloorId = dto.OldFloorId.Value,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedDate = now
                }, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return await _repository.GetOldDetailsAsync(propertyId, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Returns the property's PropertyMastOld id, creating (and linking) a new PropertyMastOld row when none
    /// exists. The intermediate save is protected by the transaction opened in <see cref="UpdateOldDetailsAsync"/>.
    /// </summary>
    private async Task<int> EnsurePropertyMastOldAsync(PropertyEntity property, DateTime now, CancellationToken cancellationToken)
    {
        if (property.PropertyMastOldId.HasValue)
            return property.PropertyMastOldId.Value;

        var newPropertyMastOld = new PropertyMastOldEntity
        {
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = now
        };
        await _repository.AddPropertyMastOldAsync(newPropertyMastOld, cancellationToken);

        // Save to get the DB-generated PK so we can link the parent FK.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        property.PropertyMastOldId = newPropertyMastOld.Id;
        property.UpdatedDate = now;
        return newPropertyMastOld.Id;
    }
}
