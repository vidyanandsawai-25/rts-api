using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyAssessmentStatus;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class PropertyAssessmentStatusMappingProfile : Profile
{
    public PropertyAssessmentStatusMappingProfile()
    {
        CreateMap<PropertyAssessmentStatusEntity, PropertyAssessmentStatusDto>();

        CreateMap<PropertyAssessmentStatusDto, PropertyAssessmentStatusEntity>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        CreateMap<CreatePropertyAssessmentStatusDto, PropertyAssessmentStatusEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdatePropertyAssessmentStatusDto, PropertyAssessmentStatusEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
