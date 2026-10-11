using AutoMapper;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RTSRuleMasterMappingProfile : Profile
{
    public RTSRuleMasterMappingProfile()
    {
        CreateMap<RTSRuleConditionEntity, RTSRuleConditionDto>();
        // Entity -> Read DTO
        CreateMap<RTSRuleMasterEntity, RTSRuleMasterDto>()
            .ForMember(dest => dest.Conditions, opt => opt.MapFrom(src => src.Conditions.OrderBy(x => x.ConditionOrder)));

        // Create DTO -> Entity
        CreateMap<CreateRTSRuleDto, RTSRuleMasterEntity>()
            .ForMember(dest => dest.Conditions, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ServiceId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());

        // Update DTO -> Entity (for updating existing entities)
        CreateMap<UpdateRTSRuleDto, RTSRuleMasterEntity>()
            .ForMember(dest => dest.Conditions, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ServiceId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());
    }
}

