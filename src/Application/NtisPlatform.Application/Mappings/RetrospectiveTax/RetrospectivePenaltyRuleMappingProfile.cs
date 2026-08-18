using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class RetrospectivePenaltyRuleMappingProfile : Profile
{
    public RetrospectivePenaltyRuleMappingProfile()
    {
        CreateMap<RetrospectivePenaltyRuleEntity, RetrospectivePenaltyRuleDto>();

        CreateMap<CreateRetrospectivePenaltyRuleDto, RetrospectivePenaltyRuleEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateRetrospectivePenaltyRuleDto, RetrospectivePenaltyRuleEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
