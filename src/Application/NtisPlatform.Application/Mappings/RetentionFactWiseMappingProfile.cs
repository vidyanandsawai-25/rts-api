using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings
{
    public class RetentionFactWiseMappingProfile : Profile
    {
        public RetentionFactWiseMappingProfile()
        {
            CreateMap<CreateRetentionFactWiseDto, RetentionFactWiseEntity>()
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateRetentionFactWiseDto, RetentionFactWiseEntity>()
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())              
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
          
            CreateMap<RetentionFactWiseEntity, RetentionFactWiseDto>()
                ;
        }
    }
}
