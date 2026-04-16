using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyCertificateType;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class PropertyCertificateTypeMappingProfile : Profile
{
    public PropertyCertificateTypeMappingProfile()
    {
        CreateMap<PropertyCertificateTypeMasterEntity, PropertyCertificateTypeDto>();

        CreateMap<CreatePropertyCertificateTypeDto, PropertyCertificateTypeMasterEntity>()
          .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdatePropertyCertificateTypeDto, PropertyCertificateTypeMasterEntity>()
      .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
      .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
      .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
