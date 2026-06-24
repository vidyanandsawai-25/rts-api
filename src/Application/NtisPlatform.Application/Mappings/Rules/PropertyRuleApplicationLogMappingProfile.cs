using AutoMapper;
using NtisPlatform.Application.DTOs.Rules;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings.Rules
{
    public class PropertyRuleApplicationLogMappingProfile : Profile
    {
        public PropertyRuleApplicationLogMappingProfile()
        {
            CreateMap<PropertyRuleApplicationLogEntity, PropertyRuleApplicationLogDto>()
                .ForMember(dest => dest.RuleScopeId, opt => opt.Ignore())
                .ForMember(dest => dest.RuleScopeName, opt => opt.Ignore());
        }
    }
}
