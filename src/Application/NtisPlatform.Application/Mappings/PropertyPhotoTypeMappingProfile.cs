using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyPhotoType;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class PropertyPhotoTypeMappingProfile : Profile
{
    public PropertyPhotoTypeMappingProfile()
    {
        CreateMap<PropertyPhotoTypeEntity, PropertyPhotoTypeDto>();

        CreateMap<CreatePropertyPhotoTypeDto, PropertyPhotoTypeEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdatePropertyPhotoTypeDto, PropertyPhotoTypeEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
