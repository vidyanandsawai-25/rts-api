using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class RetrospectiveRuleDateConditionMappingProfile : Profile
{
    public RetrospectiveRuleDateConditionMappingProfile()
    {
        CreateMap<RetrospectiveRuleDateConditionEntity, RetrospectiveRuleDateConditionDto>();

        CreateMap<CreateRetrospectiveRuleDateConditionDto, RetrospectiveRuleDateConditionEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateRetrospectiveRuleDateConditionDto, RetrospectiveRuleDateConditionEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
