using AutoMapper;
using NtisPlatform.Application.DTOs.Master.TaxCalculationGuideline;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class TaxCalculationGuidelineMappingProfile : Profile
{
    public TaxCalculationGuidelineMappingProfile()
    {
        CreateMap<TaxCalculationGuidelineEntity, TaxCalculationGuidelineDto>();

        CreateMap<TaxCalculationGuidelineDto, TaxCalculationGuidelineEntity>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        CreateMap<CreateTaxCalculationGuidelineDto, TaxCalculationGuidelineEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateTaxCalculationGuidelineDto, TaxCalculationGuidelineEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
