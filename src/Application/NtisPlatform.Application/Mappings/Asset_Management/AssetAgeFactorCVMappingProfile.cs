using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAgeFactorCVMaster;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetAgeFactorCVMappingProfile : Profile
{
    public AssetAgeFactorCVMappingProfile()
    {
        // ConstructionTypeDescription has no source member on the entity (a deliberately clean POCO
        // with only ConstructionTypeId - no navigation property). It's populated via the SQL JOIN in
        // AssetAgeFactorCVService.GetAllAsync, which builds the DTO directly and never calls
        // _mapper.Map for that path; Ignore() here only satisfies AutoMapper's config validation for
        // the GetById/Create/Update codepaths, where it stays empty.
        CreateMap<AssetAgeFactorCVMasterEntity, AssetAgeFactorCVMasterDto>()
            .ForMember(d => d.ConstructionTypeDescription, o => o.Ignore());

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
