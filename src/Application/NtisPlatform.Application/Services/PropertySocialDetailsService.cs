using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for the Property "Social Info" tab. Owns the hierarchy/upsert business flow;
/// all social-attribute and social-detail queries are delegated to <see cref="IPropertySocialDetailsRepository"/>
/// so this service contains no EF Core query expressions. Generic CRUD is provided by the base service.
/// </summary>
public class PropertySocialDetailsService : BaseCommonCrudService<PropertySocialDetailsEntity, PropertySocialDetailsDto, CreatePropertySocialDetailsDto, UpdatePropertySocialDetailsDto, PropertySocialDetailsQueryParameters, int>, IPropertySocialDetailsService
{
    private readonly IPropertySocialDetailsRepository _socialDetailsRepository;
    private readonly IRepository<DocumentBindingEntity, int> _documentBindingRepository;
    private readonly IRepository<DocumentEntity, int> _documentRepository;

    public PropertySocialDetailsService(
        IRepository<PropertySocialDetailsEntity, int> repository,
        IPropertySocialDetailsRepository socialDetailsRepository,
        IRepository<DocumentBindingEntity, int> documentBindingRepository,
        IRepository<DocumentEntity, int> documentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _socialDetailsRepository = socialDetailsRepository;
        _documentBindingRepository = documentBindingRepository;
        _documentRepository = documentRepository;
    }

    private sealed class PropertySocialDetailBindingInfo
    {
        public int PropertySocialDetailId { get; init; }
        public int BindingId { get; init; }
        public Guid DocumentGuid { get; init; }
        public string? BindingPurpose { get; init; }
    }

    // ── Generic CRUD overrides ─────────────────────────────────────────────────────────
    // PropertySocialDetailsDto carries IsPhotoRequired/IsDocumentRequired (from the SocialAttribute)
    // and PhotoGuid/DocumentGuid (from polymorphic DocumentBinding+Document). The AutoMapper profile
    // intentionally ignores those members because they cannot be mapped from the entity alone, so the
    // base CRUD result must be enriched here — otherwise GET/POST/PUT would return false/null defaults.

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
            await EnrichDtosAsync(new List<PropertySocialDetailsDto> { result }, cancellationToken);
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
            await EnrichDtosAsync(new List<PropertySocialDetailsDto> { result }, cancellationToken);
        return result;
    }

    /// <summary>
    /// Populates the DTO members the AutoMapper profile cannot derive from the entity alone:
    /// requirement flags from the parent <see cref="SocialAttributeEntity"/>, and the document/photo
    /// GUIDs + binding ids from the polymorphic DocumentBinding→Document association.
    /// </summary>
    private async Task EnrichDtosAsync(List<PropertySocialDetailsDto> dtos, CancellationToken cancellationToken)
    {
        if (dtos == null || dtos.Count == 0) return;

        var psdIds = dtos.Select(d => d.Id).ToList();
        var attributeIds = dtos.Select(d => d.SocialAttributeId).Distinct().ToList();

        // 1. Requirement flags come from the SocialAttribute the detail belongs to.
        var requirements = (await _socialDetailsRepository.GetActiveSocialAttributesAsync(cancellationToken))
            .Where(sa => attributeIds.Contains(sa.Id))
            .ToDictionary(sa => sa.Id, sa => (sa.IsPhotoRequired, sa.IsDocumentRequired));

        // 2. Polymorphic document/photo bindings for these detail rows.
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

        // 3. Project enrichment onto each DTO.
        foreach (var dto in dtos)
        {
            if (requirements.TryGetValue(dto.SocialAttributeId, out var req))
            {
                dto.IsPhotoRequired = req.IsPhotoRequired;
                dto.IsDocumentRequired = req.IsDocumentRequired;
            }

            // Prefer the explicitly referenced binding; otherwise the first non-photo (document) binding.
            var docBinding = bindings.FirstOrDefault(b => b.PropertySocialDetailId == dto.Id && b.BindingId == dto.DocumentBindingId)
                          ?? bindings.FirstOrDefault(b => b.PropertySocialDetailId == dto.Id && b.BindingPurpose != "Photo");
            if (docBinding != null)
            {
                dto.DocumentGuid = docBinding.DocumentGuid;
                dto.DocumentBindingId = docBinding.BindingId;
            }

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
        var allSocialAttributes = await _socialDetailsRepository.GetActiveSocialAttributesAsync(cancellationToken);

        // Step 2: Get existing property social details for this property
        var existingDetails = await _socialDetailsRepository.GetActiveSocialDetailsByPropertyAsync(propertyId, cancellationToken);

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
        var existingRecords = await _socialDetailsRepository.GetActiveSocialDetailsByPropertyAsync(dto.PropertyId, cancellationToken);

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
        var updatedRecords = await _socialDetailsRepository.GetActiveSocialDetailsWithAttributeByPropertyAsync(dto.PropertyId, cancellationToken);

        var dtos = _mapper.Map<List<PropertySocialDetailsDto>>(updatedRecords);
        await EnrichDtosAsync(dtos, cancellationToken);
        return dtos;
    }
}
