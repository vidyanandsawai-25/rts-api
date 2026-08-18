using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class RetrospectiveTaxPolicyMappingProfile : Profile
{
    public RetrospectiveTaxPolicyMappingProfile()
    {
        CreateMap<RetrospectiveTaxPolicyEntity, RetrospectiveTaxPolicyDto>();

        CreateMap<CreateRetrospectiveTaxPolicyDto, RetrospectiveTaxPolicyEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateRetrospectiveTaxPolicyDto, RetrospectiveTaxPolicyEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
