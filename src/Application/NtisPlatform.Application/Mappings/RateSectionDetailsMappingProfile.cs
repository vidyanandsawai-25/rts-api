using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RateSectionDetailsMappingProfile : Profile
{
    public RateSectionDetailsMappingProfile()
    {
        CreateMap<RateSectionDetailsEntity, RateSectionDetailsDto>();

        CreateMap<CreateRateSectionDetailsDto, RateSectionDetailsEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateRateSectionDetailsDto, RateSectionDetailsEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}

