using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class PenaltyRuleMappingProfile : Profile
{
    public PenaltyRuleMappingProfile()
    {
        CreateMap<PenaltyRuleMasterEntity, PenaltyRuleDto>();

        CreateMap<CreatePenaltyRuleDto, PenaltyRuleMasterEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));

        CreateMap<UpdatePenaltyRuleDto, PenaltyRuleMasterEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UpdatedBy));
    }
}
