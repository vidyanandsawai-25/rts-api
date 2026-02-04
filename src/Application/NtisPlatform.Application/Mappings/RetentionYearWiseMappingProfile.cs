using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings
{
    public class RetentionYearWiseMappingProfile : Profile
    {
        public RetentionYearWiseMappingProfile()
        {
            CreateMap<CreateRetentionYearWiseDto, RetentionYearWiseEntity>()
               .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
               .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
               .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateRetentionYearWiseDto, RetentionYearWiseEntity>()
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

            CreateMap<RetentionYearWiseEntity, RetentionYearWiseDto>();
        }
    }
}
