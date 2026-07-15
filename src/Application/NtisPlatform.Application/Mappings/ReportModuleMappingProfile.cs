using AutoMapper;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class ReportModuleMappingProfile : Profile
{
    public ReportModuleMappingProfile()
    {
        CreateMap<ReportModuleEntity, ReportModuleDto>()
            .ForMember(dest => dest.LogoBase64, opt => opt.MapFrom(src =>
                src.LogoContent != null && src.LogoContent.Length > 0 ? Convert.ToBase64String(src.LogoContent) : null));

        CreateMap<CreateReportModuleDto, ReportModuleEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());

        CreateMap<UpdateReportModuleDto, ReportModuleEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());
    }
}
