using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class RetrospectiveRuleSummaryMappingProfile : Profile
{
    public RetrospectiveRuleSummaryMappingProfile()
    {
        CreateMap<RetrospectiveRuleSummaryEntity, RetrospectiveRuleSummaryDto>();

        CreateMap<CreateRetrospectiveRuleSummaryDto, RetrospectiveRuleSummaryEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateRetrospectiveRuleSummaryDto, RetrospectiveRuleSummaryEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());
    }
}
