using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class PropertyTaxCalculationCVResultsMappingProfile : Profile
{
    public PropertyTaxCalculationCVResultsMappingProfile()
    {
        // Entity to DTO mapping
        CreateMap<PropertyTaxCalculationCVResultsEntity, PropertyTaxCalculationCVResultsDto>()
            .ForMember(dest => dest.TaxName, opt => opt.MapFrom(src => src.TaxMaster != null ? src.TaxMaster.TaxName : null))
            .ForMember(dest => dest.FloorFactor, opt => opt.Ignore())  // Computed based on lift availability
            .ForMember(dest => dest.AgeFactor, opt => opt.MapFrom(src => src.AgeFactorCVMaster != null ? (double?)src.AgeFactorCVMaster.Factor : null))
            .ForMember(dest => dest.NTBFactor, opt => opt.MapFrom(src => src.NatureFactorCVMaster != null ? (double?)src.NatureFactorCVMaster.Factor : null))
            .ForMember(dest => dest.UseFactor, opt => opt.MapFrom(src => src.UseFactorCVMaster != null ? (double?)src.UseFactorCVMaster.Factor : null))
            .ForMember(dest => dest.CVInputHash, opt => opt.MapFrom(src => src.CVInputHash))
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.MapFrom(src => src.MarkedForDeletion));

        // CreateDto to Entity mapping
        CreateMap<CreatePropertyTaxCalculationCVResultsDto, PropertyTaxCalculationCVResultsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
             .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.CVInputHash, opt => opt.MapFrom(src => src.CVInputHash))
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.MapFrom(_ => false))

            .ForMember(dest => dest.PropertyMast, opt => opt.Ignore())
            .ForMember(dest => dest.TaxMaster, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyDetails, opt => opt.Ignore())
            .ForMember(dest => dest.RateCVMaster, opt => opt.Ignore())
            .ForMember(dest => dest.FloorFactorCVMaster, opt => opt.Ignore())
            .ForMember(dest => dest.AgeFactorCVMaster, opt => opt.Ignore())
            .ForMember(dest => dest.NatureFactorCVMaster, opt => opt.Ignore())
            .ForMember(dest => dest.UseFactorCVMaster, opt => opt.Ignore());

        // UpdateDto to Entity mapping
        CreateMap<UpdatePropertyTaxCalculationCVResultsDto, PropertyTaxCalculationCVResultsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyDetailsId, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyId, opt => opt.Ignore())
            .ForMember(dest => dest.TaxId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())

            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CVInputHash, opt => opt.Ignore()) // Not updated via UpdateDto
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore()) // Not updated via UpdateDto
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.PropertyMast, opt => opt.Ignore())
            .ForMember(dest => dest.TaxMaster, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyDetails, opt => opt.Ignore())
            .ForMember(dest => dest.RateCVMaster, opt => opt.Ignore())
            .ForMember(dest => dest.FloorFactorCVMaster, opt => opt.Ignore())
            .ForMember(dest => dest.AgeFactorCVMaster, opt => opt.Ignore())
            .ForMember(dest => dest.NatureFactorCVMaster, opt => opt.Ignore())
            .ForMember(dest => dest.UseFactorCVMaster, opt => opt.Ignore());
    }
}
