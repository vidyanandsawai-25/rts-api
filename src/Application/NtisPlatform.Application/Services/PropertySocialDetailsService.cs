using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertySocialDetailsService : BaseCommonCrudService<PropertySocialDetailsEntity, PropertySocialDetailsDto, CreatePropertySocialDetailsDto, UpdatePropertySocialDetailsDto, PropertySocialDetailsQueryParameters, int>, IPropertySocialDetailsService
{
    private readonly IRepository<SocialAttributeEntity> _socialAttributeRepository;
    private readonly IRepository<DocumentBindingEntity> _documentBindingRepository;
    private readonly IRepository<DocumentEntity> _documentRepository;

    private class PropertySocialDetailBindingInfo
    {
        public int PropertySocialDetailId { get; set; }
        public int BindingId { get; set; }
        public Guid DocumentGuid { get; set; }
        public string? BindingPurpose { get; set; }
    }

    public PropertySocialDetailsService(
        IRepository<PropertySocialDetailsEntity, int> repository, 
        IRepository<SocialAttributeEntity> socialAttributeRepository,
        IRepository<DocumentBindingEntity> documentBindingRepository,
        IRepository<DocumentEntity> documentRepository,
        IUnitOfWork unitOfWork, 
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _socialAttributeRepository = socialAttributeRepository;
        _documentBindingRepository = documentBindingRepository;
        _documentRepository = documentRepository;
    }

    public override async Task<PagedResult<PropertySocialDetailsDto>> GetAllAsync(PropertySocialDetailsQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var result = await base.GetAllAsync(queryParameters, cancellationToken);
        await EnrichDtosAsync(result.Items.ToList(), cancellationToken);
        return result;
    }

    public override async Task<PropertySocialDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await base.GetByIdAsync(id, cancellationToken);
        if (result != null)
        {
            await EnrichDtosAsync(new List<PropertySocialDetailsDto> { result }, cancellationToken);
        }
        return result;
    }

    public override async Task<PropertySocialDetailsDto> CreateAsync(CreatePropertySocialDetailsDto createDto, CancellationToken cancellationToken = default)
    {
        var result = await base.CreateAsync(createDto, cancellationToken);
        await EnrichDtosAsync(new List<PropertySocialDetailsDto> { result }, cancellationToken);
        return result;
    }

    public override async Task<PropertySocialDetailsDto?> UpdateAsync(int id, UpdatePropertySocialDetailsDto updateDto, CancellationToken cancellationToken = default)
    {
        var result = await base.UpdateAsync(id, updateDto, cancellationToken);
        if (result != null)
        {
            await EnrichDtosAsync(new List<PropertySocialDetailsDto> { result }, cancellationToken);
        }
        return result;
    }

    private async Task EnrichDtosAsync(List<PropertySocialDetailsDto> dtos, CancellationToken cancellationToken)
    {
        if (dtos == null || !dtos.Any()) return;

        var psdIds = dtos.Select(d => d.Id).ToList();
        var attributeIds = dtos.Select(d => d.SocialAttributeId).Distinct().ToList();

        // 1. Fetch requirements flags from SocialAttributeEntity
        var requirements = await _socialAttributeRepository.GetQueryable()
            .Where(sa => attributeIds.Contains(sa.Id))
            .Select(sa => new { sa.Id, sa.IsPhotoRequired, sa.IsDocumentRequired })
            .ToDictionaryAsync(sa => sa.Id, cancellationToken);

        // 2. Fetch polymorphic bindings (documents & photos)
        var bindings = await (
            from db in _documentBindingRepository.GetQueryable()
            where db.ReferenceTableName == "PropertySocialDetails"
               && db.ReferenceTableId.HasValue
               && psdIds.Contains(db.ReferenceTableId.Value)
               && db.IsActive
               && !db.MarkedForDeletion
            join doc in _documentRepository.GetQueryable() on db.DocumentId equals doc.Id
            select new PropertySocialDetailBindingInfo
            {
                PropertySocialDetailId = db.ReferenceTableId.Value,
                BindingId = db.Id,
                DocumentGuid = doc.DocumentGuid,
                BindingPurpose = db.BindingPurpose
            }).ToListAsync(cancellationToken);

        // 3. Enrich DTOs
        foreach (var dto in dtos)
        {
            if (requirements.TryGetValue(dto.SocialAttributeId, out var req))
            {
                dto.IsPhotoRequired = req.IsPhotoRequired;
                dto.IsDocumentRequired = req.IsDocumentRequired;
            }

            // Document binding
            var docBinding = bindings.FirstOrDefault(b => b.PropertySocialDetailId == dto.Id && b.BindingId == dto.DocumentBindingId);
            docBinding ??= bindings.FirstOrDefault(b => b.PropertySocialDetailId == dto.Id && b.BindingPurpose != "Photo");
            if (docBinding != null)
            {
                dto.DocumentGuid = docBinding.DocumentGuid;
                dto.DocumentBindingId = docBinding.BindingId;
            }

            // Photo binding
            var photoBinding = bindings.FirstOrDefault(b => b.PropertySocialDetailId == dto.Id && b.BindingPurpose == "Photo");
            if (photoBinding != null)
            {
                dto.PhotoBindingId = photoBinding.BindingId;
                dto.PhotoGuid = photoBinding.DocumentGuid;
            }
        }
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

        // Step 2.5: Load polymorphic bindings (including photos and fallback documents) for existing details
        var detailIds = existingDetails.Select(x => x.Id).ToList();
        var bindings = new List<PropertySocialDetailBindingInfo>();
        if (detailIds.Any())
        {
            bindings = await (
                from db in _documentBindingRepository.GetQueryable()
                where db.ReferenceTableName == "PropertySocialDetails"
                   && db.ReferenceTableId.HasValue
                   && detailIds.Contains(db.ReferenceTableId.Value)
                   && db.IsActive
                   && !db.MarkedForDeletion
                join doc in _documentRepository.GetQueryable() on db.DocumentId equals doc.Id
                select new PropertySocialDetailBindingInfo
                {
                    PropertySocialDetailId = db.ReferenceTableId.Value,
                    BindingId = db.Id,
                    DocumentGuid = doc.DocumentGuid,
                    BindingPurpose = db.BindingPurpose
                }).ToListAsync(cancellationToken);
        }

        // Step 3: Build parent-child hierarchy
        var parentAttributes = allSocialAttributes.Where(x => x.ParentAttributeId == null).ToList();
        var result = new PropertySocialInfoResponseDto
        {
            PropertyId = propertyId,
            SocialAttributes = parentAttributes.Select(parent => BuildHierarchy(parent, allSocialAttributes, existingDetails, bindings)).ToList()
        };

        return result;
    }

    private SocialAttributeHierarchyDto BuildHierarchy(
        SocialAttributeEntity attribute,
        List<SocialAttributeEntity> allAttributes,
        List<PropertySocialDetailsEntity> existingDetails,
        List<PropertySocialDetailBindingInfo> bindings)
    {
        // Find existing value for this attribute
        var existingValue = existingDetails.FirstOrDefault(x => x.SocialAttributeId == attribute.Id);

        Guid? docGuid = null;
        int? docBindingId = existingValue?.DocumentBindingId;
        int? photoBindingId = null;
        Guid? photoGuid = null;

        if (existingValue != null)
        {
            var psdId = existingValue.Id;
            var photoBinding = bindings.FirstOrDefault(b => b.PropertySocialDetailId == psdId && b.BindingPurpose == "Photo");
            if (photoBinding != null)
            {
                photoBindingId = photoBinding.BindingId;
                photoGuid = photoBinding.DocumentGuid;
            }

            if (docBindingId.HasValue)
            {
                var dbObj = bindings.FirstOrDefault(b => b.PropertySocialDetailId == psdId && b.BindingId == docBindingId.Value);
                docGuid = dbObj?.DocumentGuid;
            }
            else
            {
                var dbObj = bindings.FirstOrDefault(b => b.PropertySocialDetailId == psdId && b.BindingPurpose != "Photo");
                docGuid = dbObj?.DocumentGuid;
                docBindingId = dbObj?.BindingId;
            }
        }

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
            DocumentBindingId = docBindingId,
            DocumentGuid = docGuid,
            PhotoBindingId = photoBindingId,
            PhotoGuid = photoGuid,
            Remark = existingValue?.Remark
        };

        // Recursively build children
        var children = allAttributes.Where(x => x.ParentAttributeId == attribute.Id).ToList();
        dto.Children = children.Select(child => BuildHierarchy(child, allAttributes, existingDetails, bindings)).ToList();

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

        var dtos = _mapper.Map<List<PropertySocialDetailsDto>>(updatedRecords);
        await EnrichDtosAsync(dtos, cancellationToken);
        return dtos;
    }
}
