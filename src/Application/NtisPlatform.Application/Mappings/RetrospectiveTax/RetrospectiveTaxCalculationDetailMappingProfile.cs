using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculationDetail;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class RetrospectiveTaxCalculationDetailMappingProfile : Profile
{
    public RetrospectiveTaxCalculationDetailMappingProfile()
    {
        CreateMap<RetrospectiveTaxCalculationDetailEntity, RetrospectiveTaxCalculationDetailDto>();

        CreateMap<CreateRetrospectiveTaxCalculationDetailDto, RetrospectiveTaxCalculationDetailEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore());

        CreateMap<UpdateRetrospectiveTaxCalculationDetailDto, RetrospectiveTaxCalculationDetailEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore());
    }
}
