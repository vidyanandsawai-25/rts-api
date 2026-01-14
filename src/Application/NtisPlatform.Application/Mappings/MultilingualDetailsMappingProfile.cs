using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class MultilingualDetailsMappingProfile : Profile
{
    public MultilingualDetailsMappingProfile()
    {
        CreateMap<MultilingualDetailsEntity, MultilingualDetailsDtos>()
            .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.Key));

        CreateMap<CreateMultilingualDetailsDtos, MultilingualDetailsEntity>()
            .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.Key))
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateMultilingualDetailsDtos, MultilingualDetailsEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}