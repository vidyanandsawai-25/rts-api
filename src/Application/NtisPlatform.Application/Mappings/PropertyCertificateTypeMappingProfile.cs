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
          .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
          // IsProtected is a system-managed flag, not settable via the create API.
          .ForMember(dest => dest.IsProtected, opt => opt.Ignore());

        CreateMap<UpdatePropertyCertificateTypeDto, PropertyCertificateTypeMasterEntity>()
      .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
      .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
      .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
      // IsProtected is a system-managed flag, not settable via the update API.
      .ForMember(dest => dest.IsProtected, opt => opt.Ignore());
    }
}
