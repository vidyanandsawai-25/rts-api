using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculation;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class RetrospectiveTaxCalculationMappingProfile : Profile
{
    public RetrospectiveTaxCalculationMappingProfile()
    {
        CreateMap<RetrospectiveTaxCalculationEntity, RetrospectiveTaxCalculationDto>();

        CreateMap<CreateRetrospectiveTaxCalculationDto, RetrospectiveTaxCalculationEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateRetrospectiveTaxCalculationDto, RetrospectiveTaxCalculationEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());
    }
}
