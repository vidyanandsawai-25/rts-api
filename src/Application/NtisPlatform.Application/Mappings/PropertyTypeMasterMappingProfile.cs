using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class PropertyTypeMasterMappingProfile : Profile
{
    public PropertyTypeMasterMappingProfile()
    {
        CreateMap<PropertyTypeMasterEntity, PropertyTypeMasterDto>()
            ;

        CreateMap<CreatePropertyTypeMasterDto, PropertyTypeMasterEntity>()
          .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdatePropertyTypeMasterDto, PropertyTypeMasterEntity>()
      .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
      .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
      .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

    }

}

