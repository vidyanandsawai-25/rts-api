using AutoMapper;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class PropertySocialDetailsMappingProfile : Profile
{
    public PropertySocialDetailsMappingProfile()
    {
        CreateMap<PropertySocialDetailsEntity, PropertySocialDetailsDto>()
            .ForMember(dest => dest.SocialAttributeCode, opt => opt.MapFrom(src => src.SocialAttribute != null ? src.SocialAttribute.SocialAttributeCode : null))
            .ForMember(dest => dest.SocialAttributeName, opt => opt.MapFrom(src => src.SocialAttribute != null ? src.SocialAttribute.SocialAttributeName : null))
            .ForMember(dest => dest.IsPhotoRequired, opt => opt.Ignore())
            .ForMember(dest => dest.IsDocumentRequired, opt => opt.Ignore())
            .ForMember(dest => dest.PhotoBindingId, opt => opt.Ignore())
            .ForMember(dest => dest.PhotoGuid, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentGuid, opt => opt.Ignore());

        CreateMap<CreatePropertySocialDetailsDto, PropertySocialDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyMast, opt => opt.Ignore())
            .ForMember(dest => dest.SocialAttribute, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentBinding, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdatePropertySocialDetailsDto, PropertySocialDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyMast, opt => opt.Ignore())
            .ForMember(dest => dest.SocialAttribute, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentBinding, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}