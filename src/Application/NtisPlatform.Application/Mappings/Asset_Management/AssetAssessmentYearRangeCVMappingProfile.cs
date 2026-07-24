using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAssessmentYearRangeMasterCV;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetAssessmentYearRangeCVMappingProfile : Profile
{
    public AssetAssessmentYearRangeCVMappingProfile()
    {
        CreateMap<AssetAssessmentYearRangeMasterCVEntity, AssetAssessmentYearRangeMasterCVDto>();

        CreateMap<CreateAssetAssessmentYearRangeMasterCVDto, AssetAssessmentYearRangeMasterCVEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));

        CreateMap<UpdateAssetAssessmentYearRangeMasterCVDto, AssetAssessmentYearRangeMasterCVEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UpdatedBy));
    }
}
