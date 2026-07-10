using AutoMapper;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class ReportParameterDefinitionMappingProfile : Profile
{
    public ReportParameterDefinitionMappingProfile()
    {
        CreateMap<ReportParameterDefinitionEntity, ReportParameterDefinitionDto>();

        CreateMap<CreateReportParameterDefinitionDto, ReportParameterDefinitionEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateReportParameterDefinitionDto, ReportParameterDefinitionEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
