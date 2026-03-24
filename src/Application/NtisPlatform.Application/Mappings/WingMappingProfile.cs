using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class WingMappingProfile : Profile
{
    public WingMappingProfile()
    {
        CreateMap<WingEntity, WingDto>();

        CreateMap<CreateWingDto, WingEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateWingDto, WingEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}