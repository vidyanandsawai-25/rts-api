using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertySocialDetailsService : BaseCommonCrudService<PropertySocialDetailsEntity, PropertySocialDetailsDto, CreatePropertySocialDetailsDto, UpdatePropertySocialDetailsDto, PropertySocialDetailsQueryParameters, int>, IPropertySocialDetailsService
{
    private readonly IRepository<SocialAttributeEntity> _socialAttributeRepository;

    public PropertySocialDetailsService(
        IRepository<PropertySocialDetailsEntity, int> repository, 
        IRepository<SocialAttributeEntity> socialAttributeRepository,
        IUnitOfWork unitOfWork, 
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _socialAttributeRepository = socialAttributeRepository;
    }

    public async Task<PropertySocialInfoResponseDto> GetPropertySocialInfoAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get ALL active social attributes
        var allSocialAttributes = await _socialAttributeRepository.GetQueryable()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder ?? int.MaxValue)
            .ThenBy(x => x.SocialAttributeCode)
            .ToListAsync(cancellationToken);

        // Step 2: Get existing property social details for this property
        var existingDetails = await _repository.GetQueryable()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .ToListAsync(cancellationToken);

        // Step 3: Build parent-child hierarchy
        var parentAttributes = allSocialAttributes.Where(x => x.ParentAttributeId == null).ToList();
        var result = new PropertySocialInfoResponseDto
        {
            PropertyId = propertyId,
            SocialAttributes = parentAttributes.Select(parent => BuildHierarchy(parent, allSocialAttributes, existingDetails)).ToList()
        };

        return result;
    }

    private SocialAttributeHierarchyDto BuildHierarchy(
        SocialAttributeEntity attribute,
        List<SocialAttributeEntity> allAttributes,
        List<PropertySocialDetailsEntity> existingDetails)
    {
        // Find existing value for this attribute
        var existingValue = existingDetails.FirstOrDefault(x => x.SocialAttributeId == attribute.Id);

        var dto = new SocialAttributeHierarchyDto
        {
            Id = attribute.Id,
            SocialAttributeCode = attribute.SocialAttributeCode,
            SocialAttributeName = attribute.SocialAttributeName,
            DataType = attribute.DataType,
            Unit = attribute.Unit,
            DisplayOrder = attribute.DisplayOrder,
            ParentAttributeId = attribute.ParentAttributeId,
            IsRequiredWhenParentTrue = attribute.IsRequiredWhenParentTrue,
            IsDiscountApplicable = attribute.IsDiscountApplicable,
            PhotoTypeId = attribute.PhotoTypeId,
            IsPhotoRequired = attribute.IsPhotoRequired,
            IsDocumentRequired = attribute.IsDocumentRequired,
            IsActive = attribute.IsActive,

            // Map existing values
            PropertySocialDetailId = existingValue?.Id,
            BitValue = existingValue?.BitValue,
            IntValue = existingValue?.IntValue,
            DecimalValue = existingValue?.DecimalValue,
            TextValue = existingValue?.TextValue,
            DateValue = existingValue?.DateValue,
            DocumentBindingId = existingValue?.DocumentBindingId,
            Remark = existingValue?.Remark
        };

        // Recursively build children
        var children = allAttributes.Where(x => x.ParentAttributeId == attribute.Id).ToList();
        dto.Children = children.Select(child => BuildHierarchy(child, allAttributes, existingDetails)).ToList();

        return dto;
    }

    public async Task<List<PropertySocialDetailsDto>> UpsertPropertySocialInfoAsync(UpsertPropertySocialInfoDto dto, CancellationToken cancellationToken = default)
    {
        var existingRecords = await _repository.GetQueryable()
            .Where(x => x.PropertyId == dto.PropertyId && x.IsActive)
            .ToListAsync(cancellationToken);

        // Step 1: Soft delete (mark as inactive) the social attributes to remove
        if (dto.SocialAttributeIdsToRemove != null && dto.SocialAttributeIdsToRemove.Any())
        {
            var recordsToRemove = existingRecords
                .Where(x => dto.SocialAttributeIdsToRemove.Contains(x.SocialAttributeId))
                .ToList();

            foreach (var record in recordsToRemove)
            {
                record.IsActive = false;
                record.UpdatedBy = dto.UpdatedBy;
                record.UpdatedDate = DateTime.Now;
                await _repository.UpdateAsync(record, cancellationToken);
            }
        }

        // Step 2: Process social attributes to add or update
        if (dto.SocialAttributes != null && dto.SocialAttributes.Any())
        {
            foreach (var item in dto.SocialAttributes)
            {
                if (item.Id.HasValue && item.Id.Value > 0)
                {
                    // Update existing record
                    var existingRecord = existingRecords.FirstOrDefault(x => x.Id == item.Id.Value && x.IsActive);
                    if (existingRecord != null)
                    {
                        // SocialAttributeId is part of the natural key (PropertyId + SocialAttributeId); do not change it during updates.
                        existingRecord.BitValue = item.BitValue;
                        existingRecord.IntValue = item.IntValue;
                        existingRecord.DecimalValue = item.DecimalValue;
                        existingRecord.TextValue = item.TextValue;
                        existingRecord.DateValue = item.DateValue;
                        existingRecord.DocumentBindingId = item.DocumentBindingId;
                        existingRecord.Remark = item.Remark;
                        existingRecord.UpdatedBy = dto.UpdatedBy;
                        existingRecord.UpdatedDate = DateTime.Now;
                        await _repository.UpdateAsync(existingRecord, cancellationToken);
                    }
                }
                else
                {
                    // If client omitted Id, upsert by natural key (PropertyId + SocialAttributeId)
                    var existingByAttribute = existingRecords
                        .FirstOrDefault(x => x.SocialAttributeId == item.SocialAttributeId && x.IsActive);

                    if (existingByAttribute != null)
                    {
                        existingByAttribute.BitValue = item.BitValue;
                        existingByAttribute.IntValue = item.IntValue;
                        existingByAttribute.DecimalValue = item.DecimalValue;
                        existingByAttribute.TextValue = item.TextValue;
                        existingByAttribute.DateValue = item.DateValue;
                        existingByAttribute.DocumentBindingId = item.DocumentBindingId;
                        existingByAttribute.Remark = item.Remark;
                        existingByAttribute.UpdatedBy = dto.UpdatedBy;
                        await _repository.UpdateAsync(existingByAttribute, cancellationToken);
                    }
                    else
                    {
                        // Create new record
                        var newRecord = new PropertySocialDetailsEntity
                        {
                            PropertyId = dto.PropertyId,
                            SocialAttributeId = item.SocialAttributeId,
                            BitValue = item.BitValue,
                            IntValue = item.IntValue,
                            DecimalValue = item.DecimalValue,
                            TextValue = item.TextValue,
                            DateValue = item.DateValue,
                            DocumentBindingId = item.DocumentBindingId,
                            Remark = item.Remark,
                            CreatedBy = dto.UpdatedBy,
                            IsActive = true
                        };
                        await _repository.AddAsync(newRecord, cancellationToken);
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Step 3: Return updated list
        var updatedRecords = await _repository.GetQueryable()
            .Include(x => x.SocialAttribute)
            .Where(x => x.PropertyId == dto.PropertyId && x.IsActive)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<PropertySocialDetailsDto>>(updatedRecords);
    }
}
