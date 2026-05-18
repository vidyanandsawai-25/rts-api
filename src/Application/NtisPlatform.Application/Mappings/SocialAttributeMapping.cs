using AutoMapper;
using NtisPlatform.Application.DTOs.Master.SocialAttributeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class SocialAttributeMappingProfile : Profile
    {
        public SocialAttributeMappingProfile()
        {

            CreateMap<SocialAttributeEntity, SocialAttributeDto>();

            CreateMap<CreateSocialAttributeDto, SocialAttributeEntity>()
             .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
             .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());

            CreateMap<UpdateSocialAttributeDto, SocialAttributeEntity>()
              .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
              .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());
        }
    }
}