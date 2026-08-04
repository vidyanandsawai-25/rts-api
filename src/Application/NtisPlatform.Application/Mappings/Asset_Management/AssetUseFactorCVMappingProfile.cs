using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.AssetUseFactorCVMaster;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetUseFactorCVMappingProfile : Profile
{
    public AssetUseFactorCVMappingProfile()
    {
        // TypeOfUseDescription/SubTypeOfUseDescription have no source members on the entity (a
        // deliberately clean POCO with only the FK ids - no navigation properties). They're populated
        // via the SQL JOINs in AssetUseFactorCVService.GetAllAsync, which builds the DTO directly and
        // never calls _mapper.Map for that path; Ignore() here only satisfies AutoMapper's config
        // validation for the GetById/Create/Update codepaths, where they stay empty.
        CreateMap<AssetUseFactorCVMasterEntity, AssetUseFactorCVMasterDto>()
            .ForMember(d => d.TypeOfUseDescription, o => o.Ignore())
            .ForMember(d => d.SubTypeOfUseDescription, o => o.Ignore());

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
