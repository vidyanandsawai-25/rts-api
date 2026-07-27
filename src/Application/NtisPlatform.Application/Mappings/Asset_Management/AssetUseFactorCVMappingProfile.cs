using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.AssetUseFactorCVMaster;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetUseFactorCVMappingProfile : Profile
{
    public AssetUseFactorCVMappingProfile()
    {
        CreateMap<AssetUseFactorCVMasterEntity, AssetUseFactorCVMasterDto>();

        CreateMap<CreateAssetUseFactorCVMasterDto, AssetUseFactorCVMasterEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));

        CreateMap<UpdateAssetUseFactorCVMasterDto, AssetUseFactorCVMasterEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UpdatedBy));
    }
}
