using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class AssessmentYearRangeMappingProfile : Profile
    {
        public AssessmentYearRangeMappingProfile()
        {
            CreateMap<CreateAssessmentYearRangeDto, AssessmentYearRangeEntity>()
               .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
               .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
               .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateAssessmentYearRangeDto, AssessmentYearRangeEntity>()
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

            CreateMap<AssessmentYearRangeEntity, AssessmentYearRangeDto>()
                ;
        }
    }
}
