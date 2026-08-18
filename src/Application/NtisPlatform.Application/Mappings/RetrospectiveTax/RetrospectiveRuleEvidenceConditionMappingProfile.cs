using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class RetrospectiveRuleEvidenceConditionMappingProfile : Profile
{
    public RetrospectiveRuleEvidenceConditionMappingProfile()
    {
        CreateMap<RetrospectiveRuleEvidenceConditionEntity, RetrospectiveRuleEvidenceConditionDto>();

        CreateMap<CreateRetrospectiveRuleEvidenceConditionDto, RetrospectiveRuleEvidenceConditionEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateRetrospectiveRuleEvidenceConditionDto, RetrospectiveRuleEvidenceConditionEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
