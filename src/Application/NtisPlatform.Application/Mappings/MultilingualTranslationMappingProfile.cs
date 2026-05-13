using NtisPlatform.Application.DTOs.Master.MultilingualDetail;
using NtisPlatform.Core.Entities;
using AutoMapper;

namespace NtisPlatform.Application.Mappings;

public class MultilingualTranslationMappingProfile : Profile
{

    public MultilingualTranslationMappingProfile()
    {
        CreateMap<MultilingualResourceEntity, MultilingualTranslationDtos>();

        CreateMap<CreateMultilingualTranslationDtos, MultilingualResourceEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateMultilingualTranslationDtos, MultilingualResourceEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}

