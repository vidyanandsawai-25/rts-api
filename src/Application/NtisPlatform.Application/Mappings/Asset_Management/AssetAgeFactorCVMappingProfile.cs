using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAgeFactorCVMaster;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetAgeFactorCVMappingProfile : Profile
{
    public AssetAgeFactorCVMappingProfile()
    {
        CreateMap<AssetAgeFactorCVMasterEntity, AssetAgeFactorCVMasterDto>();

        CreateMap<CreateAssetAgeFactorCVMasterDto, AssetAgeFactorCVMasterEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));

        CreateMap<UpdateAssetAgeFactorCVMasterDto, AssetAgeFactorCVMasterEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UpdatedBy));
    }
}
