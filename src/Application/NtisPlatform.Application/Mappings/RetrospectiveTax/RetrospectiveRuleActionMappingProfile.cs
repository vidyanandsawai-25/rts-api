using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class RetrospectiveRuleActionMappingProfile : Profile
{
    public RetrospectiveRuleActionMappingProfile()
    {
        CreateMap<RetrospectiveRuleActionEntity, RetrospectiveRuleActionDto>();

        CreateMap<CreateRetrospectiveRuleActionDto, RetrospectiveRuleActionEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateRetrospectiveRuleActionDto, RetrospectiveRuleActionEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
