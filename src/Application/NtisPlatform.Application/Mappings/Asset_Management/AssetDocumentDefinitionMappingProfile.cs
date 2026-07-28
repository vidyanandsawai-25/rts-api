using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetDocumentDefinitionMappingProfile : Profile
{
    public AssetDocumentDefinitionMappingProfile()
    {
        CreateMap<AssetDocumentDefinitionEntity, AssetDocumentDefinitionDto>()
            .ForMember(d => d.AssetCategoryName, o => o.MapFrom(s => s.AssetCategory != null ? s.AssetCategory.CategoryName : null))
            .ForMember(d => d.AssetTypeName, o => o.MapFrom(s => s.AssetType != null ? s.AssetType.TypeName : null));

        CreateMap<CreateAssetDocumentDefinitionDto, AssetDocumentDefinitionEntity>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletion, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletionDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));

        CreateMap<UpdateAssetDocumentDefinitionDto, AssetDocumentDefinitionEntity>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletion, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletionDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UpdatedBy));
    }
}
