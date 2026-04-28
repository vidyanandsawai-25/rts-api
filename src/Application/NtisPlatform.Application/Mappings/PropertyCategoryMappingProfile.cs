using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class PropertyCategoryMappingProfile : Profile
{
    public PropertyCategoryMappingProfile()
    {
        CreateMap<PropertyCategoryEntity, PropertyCategoryDto>()
            ;

        // Add missing mapping for update scenarios
        CreateMap<PropertyCategoryDto, PropertyCategoryEntity>();

        CreateMap<PropertyCategoryCreateDto, PropertyCategoryEntity>()
          .ForMember(dest => dest.Id, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<PropertyCategoryUpdateDto, PropertyCategoryEntity>()
          .ForMember(dest => dest.Id, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}

