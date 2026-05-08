using AutoMapper;
using NtisPlatform.Application.DTOs.Master.CommonRemarkDetails;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class CommonRemarkDetailsMappingProfile : Profile
    {
        public CommonRemarkDetailsMappingProfile()
        {
            CreateMap<CommonRemarkDetailsEntity, CommonRemarkDetailsDtos>();

            CreateMap<CreateCommonRemarkDetailsDto, CommonRemarkDetailsEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateCommonRemarkDetailsDto, CommonRemarkDetailsEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
