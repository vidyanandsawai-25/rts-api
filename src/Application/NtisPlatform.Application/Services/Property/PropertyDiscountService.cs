using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.Property;

/// <summary>
/// Implementation of the "Social Discount Attribute Management" use-case for the Property aggregate.
/// Owns aggregate-invariant enforcement, the discount-applicable validation rules, the per-item
/// upsert decisions and the save boundary; persistence is delegated to
/// <see cref="IPropertyDiscountRepository"/>, saving to <see cref="IUnitOfWork"/>, and aggregate
/// invariants to <see cref="IPropertyMutationInvariantPolicy"/>.
/// </summary>
public class PropertyDiscountService : IPropertyDiscountService
{
    private readonly IPropertyDiscountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPropertyMutationInvariantPolicy _invariantPolicy;

    public PropertyDiscountService(
        IPropertyDiscountRepository repository,
        IUnitOfWork unitOfWork,
        IPropertyMutationInvariantPolicy invariantPolicy)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _invariantPolicy = invariantPolicy;
    }

    public Task<PropertyDiscountInfoResponseDto?> GetDiscountDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _repository.GetDiscountDetailsAsync(propertyId, cancellationToken);

    public async Task<PropertyDiscountInfoResponseDto?> UpdateDiscountDetailsAsync(int propertyId, UpsertPropertyDiscountInfoDto dto, CancellationToken cancellationToken = default)
    {
        // A missing property is reported as null (→ 404).
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Enforce all Property aggregate write invariants before any state change.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        // Single timestamp: consistent across all record creates/updates in this operation.
        var now = DateTime.Now;

        var existingRecords = await _repository.GetSocialDetailsIncludingDeletedAsync(propertyId, cancellationToken);
        var allowedAttributeIds = await _repository.GetDiscountApplicableAttributeIdsAsync(cancellationToken);

        if (dto.DiscountAttributes != null && dto.DiscountAttributes.Any())
        {
            foreach (var item in dto.DiscountAttributes)
            {
                // Only discount-applicable attributes may be updated via this endpoint.
                if (!allowedAttributeIds.Contains(item.SocialAttributeId))
                {
                    throw new PropertyValidationException(
                        $"SocialAttribute with ID {item.SocialAttributeId} is not marked as discount-applicable. " +
                        "Only attributes with IsDiscountApplicable=true can be updated via the discount-details endpoint.");
                }

                if (item.PropertySocialDetailId.HasValue && item.PropertySocialDetailId.Value > 0)
                {
                    // Update an explicitly identified record.
                    var existingRecord = existingRecords.FirstOrDefault(x => x.Id == item.PropertySocialDetailId.Value);
                    if (existingRecord == null)
                    {
                        throw new PropertyValidationException(
                            $"PropertySocialDetails with ID {item.PropertySocialDetailId.Value} not found for property {propertyId}.");
                    }

                    if (existingRecord.SocialAttributeId != item.SocialAttributeId)
                    {
                        throw new PropertyValidationException(
                            $"PropertySocialDetails with ID {item.PropertySocialDetailId.Value} does not match SocialAttributeId {item.SocialAttributeId}.");
                    }

                    ApplyValues(existingRecord, item, dto.UpdatedBy, now);
                }
                else
                {
                    // Upsert by SocialAttributeId.
                    var existingByAttribute = existingRecords.FirstOrDefault(x => x.SocialAttributeId == item.SocialAttributeId);
                    if (existingByAttribute != null)
                    {
                        ApplyValues(existingByAttribute, item, dto.UpdatedBy, now);
                    }
                    else
                    {
                        var newRecord = new PropertySocialDetailsEntity
                        {
                            PropertyId = propertyId,
                            SocialAttributeId = item.SocialAttributeId,
                            BitValue = item.BitValue,
                            IntValue = item.IntValue,
                            DecimalValue = item.DecimalValue,
                            TextValue = item.TextValue,
                            DateValue = item.DateValue,
                            DocumentBindingId = item.DocumentBindingId,
                            Remark = item.Remark,
                            CreatedBy = dto.UpdatedBy,
                            CreatedDate = now,
                            IsActive = true
                        };
                        _repository.AddSocialDetail(newRecord);
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _repository.GetDiscountDetailsAsync(propertyId, cancellationToken);
    }

    /// <summary>
    /// Copies the editable values from an upsert item onto an existing social-detail row.
    /// Accepts a pre-captured <paramref name="now"/> so all records in one request share the same timestamp.
    /// </summary>
    private static void ApplyValues(
        PropertySocialDetailsEntity record,
        DiscountAttributeItemDto item,
        int updatedBy,
        DateTime now)
    {
        record.BitValue = item.BitValue;
        record.IntValue = item.IntValue;
        record.DecimalValue = item.DecimalValue;
        record.TextValue = item.TextValue;
        record.DateValue = item.DateValue;
        record.DocumentBindingId = item.DocumentBindingId;
        record.Remark = item.Remark;
        record.UpdatedBy = updatedBy;
        record.UpdatedDate = now;
        record.IsActive = true;
        record.MarkedForDeletion = false;
        record.MarkedForDeletionDate = null;
    }
}
