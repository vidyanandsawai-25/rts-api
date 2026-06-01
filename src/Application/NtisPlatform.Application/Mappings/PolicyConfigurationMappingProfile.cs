using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class PolicyConfigurationMappingProfile : Profile
{
    public PolicyConfigurationMappingProfile()
    {
        CreateMap<PolicyConfigurationEntity, PolicyConfigurationDto>();

        CreateMap<CreatePolicyConfigurationDto, PolicyConfigurationEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy,   opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdatePolicyConfigurationDto, PolicyConfigurationEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy,   opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
