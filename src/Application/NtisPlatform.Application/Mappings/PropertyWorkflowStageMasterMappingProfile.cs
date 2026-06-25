using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class PropertyWorkflowStageMasterMappingProfile : Profile
{
    public PropertyWorkflowStageMasterMappingProfile()
    {
        CreateMap<PropertyWorkflowStageMasterEntity, PropertyWorkflowStageMasterDto>();

        CreateMap<CreatePropertyWorkflowStageMasterDto, PropertyWorkflowStageMasterEntity>()
          .ForMember(dest => dest.Id, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdatePropertyWorkflowStageMasterDto, PropertyWorkflowStageMasterEntity>()
          .ForMember(dest => dest.Id, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

    }

}
