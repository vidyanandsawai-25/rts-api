using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class SocietyWingDetailsMappingProfile : Profile
{
    public SocietyWingDetailsMappingProfile()
    {
        CreateMap<SocietyWingDetailsEntity, SocietyWingDetailsDto>();

        CreateMap<CreateSocietyWingDetailsDto, SocietyWingDetailsEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateSocietyWingDetailsDto, SocietyWingDetailsEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

        CreateMap<CreateSocietyWingDetailsDto, SocietyDetailsEntity>()
           .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
           .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
           .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateSocietyWingDetailsDto, SocietyDetailsEntity>()
           .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
           .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
           .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
