using AutoMapper;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for Capital Value related DTOs and Entities
/// Reduces manual mapping code and ensures consistent mapping logic
/// </summary>
public class CapitalValueMappingProfile : Profile
{
    public CapitalValueMappingProfile()
    {
        // PropertyTaxCalculationCVResultsEntity -> CapitalValueDto
        CreateMap<PropertyTaxCalculationCVResultsEntity, CapitalValueDto>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // Id represents PropertyDetailsId, not CV result ID
            .ForMember(dest => dest.PropertyId, opt => opt.MapFrom(src => src.PropertyId))
            .ForMember(dest => dest.PropertyDetailsId, opt => opt.MapFrom(src => src.PropertyDetailsId))
            .ForMember(dest => dest.CapitalValue, opt => opt.MapFrom(src => src.CapitalValue))
            .ForMember(dest => dest.BaseValue, opt => opt.MapFrom(src => src.BaseValue))
           
            .ForMember(dest => dest.FloorFactor, opt => opt.MapFrom(src => src.FloorFactor))
            .ForMember(dest => dest.AgeFactor, opt => opt.MapFrom(src => src.AgeFactor))
            .ForMember(dest => dest.NTBFactor, opt => opt.MapFrom(src => src.NTBFactor))
            .ForMember(dest => dest.UseFactor, opt => opt.MapFrom(src => src.UseFactor))
            .ForMember(dest => dest.Taxes, opt => opt.Ignore()) // Populated separately
            .ForMember(dest => dest.FloorDescription, opt => opt.Ignore())
            .ForMember(dest => dest.SubFloorDescription, opt => opt.Ignore())
            .ForMember(dest => dest.ConstructionTypeDescription, opt => opt.Ignore())
            .ForMember(dest => dest.TypeOfUseDescription, opt => opt.Ignore())
            .ForMember(dest => dest.SubTypeOfUseDescription, opt => opt.Ignore())
            .ForMember(dest => dest.ConstructionYear, opt => opt.Ignore())
            .ForMember(dest => dest.AssessmentYear, opt => opt.Ignore())
            .ForMember(dest => dest.NoOfRooms, opt => opt.Ignore())
            .ForMember(dest => dest.CarpetAreaSqFeet, opt => opt.Ignore())
            .ForMember(dest => dest.CarpetAreaSqMeter, opt => opt.Ignore())
            .ForMember(dest => dest.BuiltupAreaSqMeter, opt => opt.Ignore())
            .ForMember(dest => dest.BuiltupAreaSqFeet, opt => opt.Ignore())
            .ForMember(dest => dest.RenterYesNo, opt => opt.Ignore())
            .ForMember(dest => dest.RenterName, opt => opt.Ignore())
            .ForMember(dest => dest.RentMonthly, opt => opt.Ignore())
            .ForMember(dest => dest.YearRangeCVId, opt => opt.Ignore());

        // PropertyDetailsEntity -> CapitalValueDto (for detail info)
        CreateMap<PropertyDetailsEntity, CapitalValueDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id)) // Id represents PropertyDetailsId
            .ForMember(dest => dest.PropertyId, opt => opt.MapFrom(src => src.PropertyId))
            .ForMember(dest => dest.PropertyDetailsId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ConstructionYear, opt => opt.MapFrom(src => src.ConstructionYear))
            .ForMember(dest => dest.AssessmentYear, opt => opt.MapFrom(src => src.AssessmentYear))
            .ForMember(dest => dest.NoOfRooms, opt => opt.MapFrom(src => src.NoOfRooms))
            .ForMember(dest => dest.CarpetAreaSqFeet, opt => opt.MapFrom(src => src.CarpetAreaSqFeet))
            .ForMember(dest => dest.CarpetAreaSqMeter, opt => opt.MapFrom(src => src.CarpetAreaSqMeter))
            .ForMember(dest => dest.BuiltupAreaSqMeter, opt => opt.MapFrom(src => src.BuiltupAreaSqMeter))
            .ForMember(dest => dest.BuiltupAreaSqFeet, opt => opt.MapFrom(src => src.BuiltupAreaSqFeet))
            .ForMember(dest => dest.RenterYesNo, opt => opt.MapFrom(src => src.IsRenter))
            .ForMember(dest => dest.RenterName, opt => opt.Ignore())
            .ForMember(dest => dest.RentMonthly, opt => opt.Ignore())
            .ForMember(dest => dest.FloorDescription, opt => opt.Ignore())
            .ForMember(dest => dest.SubFloorDescription, opt => opt.Ignore())
            .ForMember(dest => dest.ConstructionTypeDescription, opt => opt.Ignore())
            .ForMember(dest => dest.TypeOfUseDescription, opt => opt.Ignore())
            .ForMember(dest => dest.SubTypeOfUseDescription, opt => opt.Ignore())
            .ForMember(dest => dest.SDRR, opt => opt.Ignore())
            .ForMember(dest => dest.BaseValue, opt => opt.Ignore())
            .ForMember(dest => dest.FloorFactor, opt => opt.Ignore())
            .ForMember(dest => dest.AgeFactor, opt => opt.Ignore())
            .ForMember(dest => dest.NTBFactor, opt => opt.Ignore())
            .ForMember(dest => dest.UseFactor, opt => opt.Ignore())
            .ForMember(dest => dest.CapitalValue, opt => opt.Ignore())
            .ForMember(dest => dest.YearRangeCVId, opt => opt.Ignore())
            .ForMember(dest => dest.Taxes, opt => opt.Ignore());

        // TaxHeadDto creation (for inline mapping)
        CreateMap<PropertyTaxCalculationCVResultsEntity, TaxHeadDto>()
            .ForMember(dest => dest.TaxId, opt => opt.MapFrom(src => src.TaxId))
            .ForMember(dest => dest.Percentage, opt => opt.MapFrom(src => src.TaxPercentage))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TaxAmount))
            .ForMember(dest => dest.TaxName, opt => opt.Ignore()); // Populated from join

        // For creating PropertyTaxCalculationCVResultsEntity from calculation results
        CreateMap<CapitalValueDto, PropertyTaxCalculationCVResultsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyId, opt => opt.MapFrom(src => src.PropertyId))
            .ForMember(dest => dest.PropertyDetailsId, opt => opt.MapFrom(src => src.PropertyDetailsId))
            .ForMember(dest => dest.CapitalValue, opt => opt.MapFrom(src => src.CapitalValue))
            .ForMember(dest => dest.BaseValue, opt => opt.MapFrom(src => src.BaseValue))
             
            .ForMember(dest => dest.FloorFactor, opt => opt.MapFrom(src => src.FloorFactor))
            .ForMember(dest => dest.AgeFactor, opt => opt.MapFrom(src => src.AgeFactor))
            .ForMember(dest => dest.NTBFactor, opt => opt.MapFrom(src => src.NTBFactor))
            .ForMember(dest => dest.UseFactor, opt => opt.MapFrom(src => src.UseFactor))
            .ForMember(dest => dest.TaxId, opt => opt.Ignore())
            .ForMember(dest => dest.TaxPercentage, opt => opt.Ignore())
            .ForMember(dest => dest.TaxAmount, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.PropertyDetails, opt => opt.Ignore())
            .ForMember(dest => dest.TaxMaster, opt => opt.Ignore()) // Navigation property - ignore in reverse mapping

            .ForMember(dest => dest.PropertyMast, opt => opt.Ignore());

    }
}
