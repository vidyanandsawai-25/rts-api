using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.AssetNatureFactorCVMaster;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetNatureFactorCVMappingProfile : Profile
{
    public AssetNatureFactorCVMappingProfile()
    {
        // ConstructionTypeDescription has no source member on the entity (a deliberately clean POCO
        // with only ConstructionTypeId - no navigation property). It's populated via the SQL JOIN in
        // AssetNatureFactorCVService.GetAllAsync, which builds the DTO directly and never calls
        // _mapper.Map for that path; Ignore() here only satisfies AutoMapper's config validation for
        // the GetById/Create/Update codepaths, where it stays empty.
        CreateMap<AssetNatureFactorCVMasterEntity, AssetNatureFactorCVMasterDto>()
            .ForMember(d => d.ConstructionTypeDescription, o => o.Ignore());

        CreateMap<CreateAssetNatureFactorCVMasterDto, AssetNatureFactorCVMasterEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));

        CreateMap<UpdateAssetNatureFactorCVMasterDto, AssetNatureFactorCVMasterEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UpdatedBy));
    }
}
