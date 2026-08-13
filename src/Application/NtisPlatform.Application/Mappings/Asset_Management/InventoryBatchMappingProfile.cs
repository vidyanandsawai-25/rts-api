using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.InventoryAsset;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class InventoryBatchMappingProfile : Profile
{
    public InventoryBatchMappingProfile()
    {
        // Entity → Read DTO
        CreateMap<InventoryBatchEntity, InventoryBatchDto>()
            .ForMember(dest => dest.BatchId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Names, opt => opt.Ignore())
            .ForMember(dest => dest.TotalCapitalValue, opt => opt.MapFrom(src => src.TotalBatchCV ?? 0))
            .ForMember(dest => dest.TotalDepreciation, opt => opt.MapFrom(src => src.TotalBatchValue - (src.TotalBatchCV ?? 0)))
            .ForMember(dest => dest.Message, opt => opt.Ignore())
            .ForMember(dest => dest.Units, opt => opt.Ignore());

        CreateMap<InventoryBatchEntity, InventoryBatchDetailDto>()
            .ForMember(dest => dest.BatchId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Names, opt => opt.Ignore())
            .ForMember(dest => dest.Units, opt => opt.Ignore());

        // Entity → Unit Response DTO
        CreateMap<InventoryAssetDetailEntity, InventoryUnitResponseDto>()
            .ForMember(dest => dest.AgeInYears, opt => opt.Ignore());

        // Create DTO → Entity
        CreateMap<CreateInventoryBatchDto, InventoryBatchEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.ConditionId, opt => opt.MapFrom(src => src.InventoryItemConditionId))
            .ForMember(dest => dest.TotalBatchValue, opt => opt.MapFrom(src => src.Quantity * src.UnitValue))
            .ForMember(dest => dest.TotalBatchCV, opt => opt.Ignore())
            .ForMember(dest => dest.ParentAsset, opt => opt.Ignore())
            .ForMember(dest => dest.Units, opt => opt.Ignore());

        // Update DTO → Entity (only update provided fields).
        // Note: each optional field gets its own .Condition() rather than a blanket
        // .ForAllMembers(...).Condition(...) - AutoMapper does not combine a ForAllMembers
        // condition with a member that also has an explicit ForMember/MapFrom override (as
        // ConditionId does here); the ForAllMembers condition silently wins and evaluates against
        // the already-type-converted value, so a null InventoryItemConditionId (converted to the
        // entity's non-nullable int) reads as "not null" and overwrites ConditionId with 0 instead
        // of leaving it unchanged. Explicit per-member conditions avoid that interaction entirely.
        CreateMap<UpdateInventoryBatchDto, InventoryBatchEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.BatchId))
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.ParentAssetId, opt => opt.Ignore())
            .ForMember(dest => dest.Quantity, opt => opt.Ignore())
            .ForMember(dest => dest.InventoryItemNameId, opt => opt.Condition(src => src.InventoryItemNameId != null))
            .ForMember(dest => dest.InventoryItemModelId, opt => opt.Condition(src => src.InventoryItemModelId != null))
            .ForMember(dest => dest.OwningDepartmentId, opt => opt.Condition(src => src.OwningDepartmentId != null))
            .ForMember(dest => dest.Specifications, opt => opt.Condition(src => src.Specifications != null))
            .ForMember(dest => dest.PurchaseDate, opt => opt.Condition(src => src.PurchaseDate != null))
            .ForMember(dest => dest.UnitValue, opt => opt.Condition(src => src.UnitValue != null))
            .ForMember(dest => dest.InvoiceNumber, opt => opt.Condition(src => src.InvoiceNumber != null))
            .ForMember(dest => dest.InvoiceDate, opt => opt.Condition(src => src.InvoiceDate != null))
            .ForMember(dest => dest.InvoiceFileName, opt => opt.Condition(src => src.InvoiceFileName != null))
            .ForMember(dest => dest.PhotoFileName, opt => opt.Condition(src => src.PhotoFileName != null))
            .ForMember(dest => dest.ConditionId, opt =>
            {
                opt.Condition(src => src.InventoryItemConditionId != null);
                opt.MapFrom(src => src.InventoryItemConditionId);
            })
            .ForMember(dest => dest.TotalBatchValue, opt => opt.Ignore())
            .ForMember(dest => dest.TotalBatchCV, opt => opt.Ignore())
            .ForMember(dest => dest.ParentAsset, opt => opt.Ignore())
            .ForMember(dest => dest.Units, opt => opt.Ignore());
    }
}
