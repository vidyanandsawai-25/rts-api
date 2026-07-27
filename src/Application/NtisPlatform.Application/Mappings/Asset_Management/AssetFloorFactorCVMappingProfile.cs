using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetFloorFactorCVMappingProfile : Profile
{
    public AssetFloorFactorCVMappingProfile()
    {
        CreateMap<AssetFloorFactorCVEntity, AssetFloorFactorCVDto>();

        CreateMap<CreateAssetFloorFactorCVDto, AssetFloorFactorCVEntity>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletion, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletionDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));

        CreateMap<UpdateAssetFloorFactorCVDto, AssetFloorFactorCVEntity>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletion, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletionDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UpdatedBy));
    }
}
