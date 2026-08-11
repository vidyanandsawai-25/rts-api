using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using System.Security.Claims;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for the Property "Social Info" tab. Owns the hierarchy/upsert business flow;
/// all social-attribute and social-detail queries are delegated to <see cref="IPropertySocialDetailsRepository"/>
/// so this service contains no EF Core query expressions. Generic CRUD is provided by the base service.
///
/// <para>
/// Document operations are routed exclusively through <see cref="IDocumentApplicationService"/>
/// — this service never accesses document or binding repositories directly.
/// </para>
/// </summary>
public class PropertySocialDetailsService : BaseCommonCrudService<PropertySocialDetailsEntity, PropertySocialDetailsDto, CreatePropertySocialDetailsDto, UpdatePropertySocialDetailsDto, PropertySocialDetailsQueryParameters, int>, IPropertySocialDetailsService
{
    private readonly IPropertySocialDetailsRepository _socialDetailsRepository;
    private readonly IDocumentApplicationService _documentApplicationService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public PropertySocialDetailsService(
        IRepository<PropertySocialDetailsEntity, int> repository,
        IPropertySocialDetailsRepository socialDetailsRepository,
        IDocumentApplicationService documentApplicationService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHttpContextAccessor? httpContextAccessor = null) : base(repository, unitOfWork, mapper, null, null, httpContextAccessor)
    {
        _socialDetailsRepository = socialDetailsRepository;
        _documentApplicationService = documentApplicationService;
        _httpContextAccessor = httpContextAccessor;
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
        var allSocialAttributes = await _socialDetailsRepository.GetActiveSocialAttributesAsync(cancellationToken);
        var allowedSocialAttributes = FilterOutDiscountApplicableAttributes(allSocialAttributes);
        var allowedIds = allowedSocialAttributes.Select(x => x.Id).ToHashSet();

        var filteredItems = result.Items.Where(x => allowedIds.Contains(x.SocialAttributeId)).ToList();
        await EnrichDtosAsync(filteredItems, cancellationToken);

        return new PagedResult<PropertySocialDetailsDto>(filteredItems, filteredItems.Count, result.PageNumber, result.PageSize);
    }

    public override async Task<PropertySocialDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await base.GetByIdAsync(id, cancellationToken);
        if (result != null)
        {
            var allSocialAttributes = await _socialDetailsRepository.GetActiveSocialAttributesAsync(cancellationToken);
            var allowedSocialAttributes = FilterOutDiscountApplicableAttributes(allSocialAttributes);
            var allowedIds = allowedSocialAttributes.Select(x => x.Id).ToHashSet();

            if (!allowedIds.Contains(result.SocialAttributeId))
            {
                return null;
            }

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
            await EnrichDtosAsync(new List<PropertySocialDetailsDto> { result }, cancellationToken);
        return result;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        var updatedBy = GetCurrentUserId();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Deactivate all associated document bindings to avoid orphans
            await DeactivateAllSocialDetailBindingsAsync(entity.Id, updatedBy, cancellationToken);

            // Mark the record for deletion
            entity.MarkedForDeletion = true;
            entity.MarkedForDeletionDate = DateTime.Now;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedDate = DateTime.Now;

            // Keep IsActive as-is (do not change to false/0)
            await _repository.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteByPropertyAndAttributeAsync(int propertyId, int socialAttributeId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.SocialAttributeId == socialAttributeId, cancellationToken);
            
        if (entity == null)
            return false;

        var updatedBy = GetCurrentUserId();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Deactivate all associated document bindings to avoid orphans
            await DeactivateAllSocialDetailBindingsAsync(entity.Id, updatedBy, cancellationToken);

            // Mark the record for deletion
            entity.MarkedForDeletion = true;
            entity.MarkedForDeletionDate = DateTime.Now;
            entity.UpdatedBy = updatedBy;
            entity.UpdatedDate = DateTime.Now;

            // Keep IsActive as-is (do not change to false/0)
            await _repository.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private int GetCurrentUserId()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        var claim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value
            ?? user?.FindFirst("userId")?.Value;

        if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id) && id > 0)
        {
            return id;
        }
        return 1; // Default fallback for system/audit processes if context is not available
    }

    /// <summary>
    /// Populates the DTO members the AutoMapper profile cannot derive from the entity alone:
    /// requirement flags from the parent <see cref="SocialAttributeEntity"/>, and the document/photo
    /// GUIDs + binding ids from the polymorphic DocumentBinding→Document association.
    /// Binding data is fetched via <see cref="IDocumentApplicationService"/> — no direct repo access.
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

        // 2. Fetch all active document bindings for these detail rows via the global document service.
        var bindingInfos = await _documentApplicationService.GetDocumentsByReferenceTableAsync(
            "PropertySocialDetails",
            psdIds,
            cancellationToken);

        // Project to internal binding summary (BindingPurpose is the key discriminator)
        var bindings = bindingInfos.Select(b => new PropertySocialDetailBindingInfo
        {
            PropertySocialDetailId = b.ReferenceTableId,
            BindingId = b.BindingId,
            DocumentGuid = b.DocumentGuid,
            BindingPurpose = b.BindingPurpose
        }).ToList();

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
        var allowedSocialAttributes = FilterOutDiscountApplicableAttributes(allSocialAttributes);

        // Step 2: Get existing property social details for this property
        var existingDetails = await _socialDetailsRepository.GetActiveSocialDetailsByPropertyAsync(propertyId, cancellationToken);

        // Step 2.5: Load polymorphic bindings (including photos and fallback documents) for existing details
        var detailIds = existingDetails.Select(x => x.Id).ToList();
        var bindings = new List<PropertySocialDetailBindingInfo>();

        if (detailIds.Any())
        {
            var bindingInfos = await _documentApplicationService.GetDocumentsByReferenceTableAsync(
                "PropertySocialDetails",
                detailIds,
                cancellationToken);

            bindings = bindingInfos.Select(b => new PropertySocialDetailBindingInfo
            {
                PropertySocialDetailId = b.ReferenceTableId,
                BindingId = b.BindingId,
                DocumentGuid = b.DocumentGuid,
                BindingPurpose = b.BindingPurpose
            }).ToList();
        }

        // Step 3: Build parent-child hierarchy
        var parentAttributes = allowedSocialAttributes.Where(x => x.ParentAttributeId == null).ToList();
        var result = new PropertySocialInfoResponseDto
        {
            PropertyId = propertyId,
            SocialAttributes = parentAttributes.Select(parent => BuildHierarchy(parent, allowedSocialAttributes, existingDetails, bindings)).ToList()
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
        var allSocialAttributes = await _socialDetailsRepository.GetActiveSocialAttributesAsync(cancellationToken);
        var allowedSocialAttributes = FilterOutDiscountApplicableAttributes(allSocialAttributes);
        var allowedIds = allowedSocialAttributes.Select(x => x.Id).ToHashSet();

        if (dto.SocialAttributes != null && dto.SocialAttributes.Any())
        {
            foreach (var item in dto.SocialAttributes)
            {
                if (!allowedIds.Contains(item.SocialAttributeId))
                {
                    throw new PropertyValidationException(
                        $"SocialAttribute with ID {item.SocialAttributeId} is marked as discount-applicable or has a discount-applicable parent/child. " +
                        "It cannot be updated via the social-details endpoint.");
                }
            }
        }

        if (dto.SocialAttributeIdsToRemove != null && dto.SocialAttributeIdsToRemove.Any())
        {
            foreach (var id in dto.SocialAttributeIdsToRemove)
            {
                if (!allowedIds.Contains(id))
                {
                    throw new PropertyValidationException(
                        $"SocialAttribute with ID {id} is marked as discount-applicable or has a discount-applicable parent/child. " +
                        "It cannot be removed via the social-details endpoint.");
                }
            }
        }

        var existingRecords = await _socialDetailsRepository.GetSocialDetailsByPropertyAsync(dto.PropertyId, cancellationToken);

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

                await DeactivateAllSocialDetailBindingsAsync(record.Id, dto.UpdatedBy, cancellationToken);
            }
        }

        var newBindingsToUpdate = new List<(int BindingId, PropertySocialDetailsEntity Record)>();

        // Step 2: Process social attributes to add or update
        if (dto.SocialAttributes != null && dto.SocialAttributes.Any())
        {
            foreach (var item in dto.SocialAttributes)
            {
                if (item.Id.HasValue && item.Id.Value > 0)
                {
                    // Update existing record
                    var existingRecord = existingRecords.FirstOrDefault(x => x.Id == item.Id.Value);
                    if (existingRecord != null)
                    {
                        await DeactivateOldDocumentBindingAsync(existingRecord.Id, item.DocumentBindingId, dto.UpdatedBy, cancellationToken);

                        // SocialAttributeId is part of the natural key (PropertyId + SocialAttributeId); do not change it during updates.
                        existingRecord.BitValue = item.BitValue;
                        existingRecord.IntValue = item.IntValue;
                        existingRecord.DecimalValue = item.DecimalValue;
                        existingRecord.TextValue = item.TextValue;
                        existingRecord.DateValue = item.DateValue;
                        existingRecord.DocumentBindingId = item.DocumentBindingId;
                        existingRecord.Remark = item.Remark;
                        existingRecord.IsActive = true;
                        existingRecord.MarkedForDeletion = false;
                        existingRecord.MarkedForDeletionDate = null;
                        existingRecord.UpdatedBy = dto.UpdatedBy;
                        existingRecord.UpdatedDate = DateTime.Now;
                        await _repository.UpdateAsync(existingRecord, cancellationToken);

                        if (item.DocumentBindingId.HasValue && item.DocumentBindingId.Value > 0)
                        {
                            newBindingsToUpdate.Add((item.DocumentBindingId.Value, existingRecord));
                        }
                    }
                }
                else
                {
                    // If client omitted Id, upsert by natural key (PropertyId + SocialAttributeId)
                    var existingByAttribute = existingRecords
                        .FirstOrDefault(x => x.SocialAttributeId == item.SocialAttributeId);

                    if (existingByAttribute != null)
                    {
                        await DeactivateOldDocumentBindingAsync(existingByAttribute.Id, item.DocumentBindingId, dto.UpdatedBy, cancellationToken);

                        existingByAttribute.BitValue = item.BitValue;
                        existingByAttribute.IntValue = item.IntValue;
                        existingByAttribute.DecimalValue = item.DecimalValue;
                        existingByAttribute.TextValue = item.TextValue;
                        existingByAttribute.DateValue = item.DateValue;
                        existingByAttribute.DocumentBindingId = item.DocumentBindingId;
                        existingByAttribute.Remark = item.Remark;
                        existingByAttribute.IsActive = true;
                        existingByAttribute.MarkedForDeletion = false;
                        existingByAttribute.MarkedForDeletionDate = null;
                        existingByAttribute.UpdatedBy = dto.UpdatedBy;
                        existingByAttribute.UpdatedDate = DateTime.Now;
                        await _repository.UpdateAsync(existingByAttribute, cancellationToken);

                        if (item.DocumentBindingId.HasValue && item.DocumentBindingId.Value > 0)
                        {
                            newBindingsToUpdate.Add((item.DocumentBindingId.Value, existingByAttribute));
                        }
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

                        if (item.DocumentBindingId.HasValue && item.DocumentBindingId.Value > 0)
                        {
                            newBindingsToUpdate.Add((item.DocumentBindingId.Value, newRecord));
                        }
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update ReferenceTableId for new document bindings so the binding points to the
        // correct entity row (its ID is only known after SaveChanges).
        foreach (var tuple in newBindingsToUpdate)
        {
            await _documentApplicationService.UpdateDocumentBindingReferenceAsync(
                tuple.BindingId,
                tuple.Record.Id,
                dto.UpdatedBy,
                cancellationToken);
        }

        if (newBindingsToUpdate.Any())
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Step 3: Return updated list
        var updatedRecords = await _socialDetailsRepository.GetActiveSocialDetailsWithAttributeByPropertyAsync(dto.PropertyId, cancellationToken);

        var dtos = _mapper.Map<List<PropertySocialDetailsDto>>(updatedRecords);
        await EnrichDtosAsync(dtos, cancellationToken);
        return dtos;
    }

    /// <summary>
    /// Deactivates all document bindings for a given social detail row via the global document service.
    /// </summary>
    private async Task DeactivateAllSocialDetailBindingsAsync(int propertySocialDetailId, int updatedBy, CancellationToken cancellationToken)
    {
        // Use GetDocumentsByReferenceTableAsync to find all active bindings for this detail row
        var activeBindingInfos = await _documentApplicationService.GetDocumentsByReferenceTableAsync(
            "PropertySocialDetails",
            new[] { propertySocialDetailId },
            cancellationToken);

        foreach (var bindingInfo in activeBindingInfos)
        {
            await _documentApplicationService.DeactivateDocumentBindingAsync(
                bindingInfo.BindingId,
                updatedBy,
                cancellationToken);
        }
    }

    /// <summary>
    /// Deactivates old document bindings for a social detail row, preserving the new binding if provided.
    /// Routes through the global document service — no direct repository access.
    /// </summary>
    private async Task DeactivateOldDocumentBindingAsync(int propertySocialDetailId, int? newDocumentBindingId, int updatedBy, CancellationToken cancellationToken)
    {
        var activeBindingInfos = await _documentApplicationService.GetDocumentsByReferenceTableAsync(
            "PropertySocialDetails",
            new[] { propertySocialDetailId },
            cancellationToken);

        foreach (var bindingInfo in activeBindingInfos)
        {
            if (newDocumentBindingId == null || bindingInfo.BindingId != newDocumentBindingId.Value)
            {
                await _documentApplicationService.DeactivateDocumentBindingAsync(
                    bindingInfo.BindingId,
                    updatedBy,
                    cancellationToken);
            }
        }
    }

    private List<SocialAttributeEntity> FilterOutDiscountApplicableAttributes(List<SocialAttributeEntity> allAttributes)
    {
        // Find all attribute IDs that are directly discount-applicable
        var directlyDiscountApplicableIds = allAttributes
            .Where(x => x.IsDiscountApplicable)
            .Select(x => x.Id)
            .ToHashSet();

        // An attribute is NOT allowed in social details if:
        // 1. It is directly discount-applicable.
        // 2. It has a parent that is directly discount-applicable.
        // 3. It has a child that is directly discount-applicable.
        return allAttributes.Where(sa =>
        {
            // 1. Directly discount-applicable?
            if (directlyDiscountApplicableIds.Contains(sa.Id))
                return false;

            // 2. Parent is directly discount-applicable?
            if (sa.ParentAttributeId.HasValue && directlyDiscountApplicableIds.Contains(sa.ParentAttributeId.Value))
                return false;

            // 3. Any child is directly discount-applicable?
            var hasDiscountApplicableChild = allAttributes.Any(c => c.ParentAttributeId == sa.Id && directlyDiscountApplicableIds.Contains(c.Id));
            if (hasDiscountApplicableChild)
                return false;

            return true;
        }).ToList();
    }
}
