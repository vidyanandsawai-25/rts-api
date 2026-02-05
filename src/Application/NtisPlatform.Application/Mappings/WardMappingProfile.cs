using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;


namespace NtisPlatform.Application.Mappings;

public class WardMappingProfile : Profile
{
    public WardMappingProfile()
    {
        CreateMap<WardEntity, WardDto>();

        CreateMap<CreateWardDto, WardEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateWardDto, WardEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}

