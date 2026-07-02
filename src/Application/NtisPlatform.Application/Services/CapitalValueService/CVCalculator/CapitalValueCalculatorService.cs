using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Calculation;
using NtisPlatform.Application.Services.CapitalValue.MasterDataProviders;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Services.CapitalValue.CVCalculator;

/// <summary>
/// Handles the core capital value calculation logic for a single property detail.
/// Separated from service layer for better testability and maintainability.
/// </summary>
public class CapitalValueCalculatorService : ICapitalValueCalculator
{
    private readonly ILogger<CapitalValueCalculatorService> _logger;

    public CapitalValueCalculatorService(ILogger<CapitalValueCalculatorService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates capital value and taxes for a single property detail.
    /// Returns the standard CapitalValueDto with additional context including factor entities for ID storage.
    /// </summary>
    public CapitalValueCalculationResult Calculate(PropertyDetailsEntity propertyDetail,MasterDataContext masterData,bool hasLift,int propertyId,int moujaId,string csn)
    {


        // Validate and parse assessment year
        if (!int.TryParse(propertyDetail.AssessmentYear, out int assessmentYear) || assessmentYear <= 0)
        {
            throw new InvalidPropertyDataException("AssessmentYear", propertyDetail.AssessmentYear, propertyDetail.Id);
        }

        // Find year range
        var yearRange = masterData.YearRanges.FirstOrDefault(x => assessmentYear >= x.FromYear && assessmentYear <= x.ToYear);

        if (yearRange == null)
        {
            throw new YearRangeNotFoundException(assessmentYear, propertyDetail.Id);
        }

        // Validate type of use group
        var typeOfUseGroupCVId = propertyDetail.TypeOfUse?.TypeOfUseGroupCVId;
        if (!typeOfUseGroupCVId.HasValue)
        {
            throw new TypeOfUseGroupNotFoundException(propertyDetail.Id, propertyDetail.TypeOfUseId);
        }

        var typeOfUseGroup = propertyDetail.TypeOfUse?.TypeOfUseGroupCV;
        if (typeOfUseGroup == null)
        {
            throw new TypeOfUseGroupNotFoundException(propertyDetail.Id, propertyDetail.TypeOfUseId);
        }

        // Determine if floor-wise rate applies
        bool isFloorWiseRateApplicable = typeOfUseGroup.IsFloorWiseRateApplicable;
        int? floorGroupId = null;

        if (isFloorWiseRateApplicable)
        {
            floorGroupId = propertyDetail.Floor?.FloorGroupId;
            if (!floorGroupId.HasValue)
            {
                throw new FloorGroupNotFoundException(propertyDetail.Id);
            }
        }

        // Find rate master
        var rateMaster = masterData.RateMasters.FirstOrDefault(x =>
            x.AssessmentYearRangeId == yearRange.Id &&
            x.TypeOfUseGroupCVId == typeOfUseGroupCVId.Value &&
            (isFloorWiseRateApplicable ? x.FloorGroupId == floorGroupId : x.FloorGroupId == null));

        if (rateMaster == null)
        {
            throw new RateMasterNotFoundException(moujaId, csn, assessmentYear, typeOfUseGroupCVId.Value, floorGroupId);
        }

        // Validate rate amount from rate master
        if (!rateMaster.RateAmount.HasValue || rateMaster.RateAmount.Value <= 0)
        {
            throw new InvalidPropertyDataException(
                "RateMaster.RateAmount",
                rateMaster.RateAmount,
                propertyDetail.Id);
        }

        // Parse construction year and calculate age
        if (!int.TryParse(propertyDetail.ConstructionYear, out int constructionYear) || constructionYear <= 0)
        {
            throw new InvalidPropertyDataException("ConstructionYear", propertyDetail.ConstructionYear, propertyDetail.Id);
        }

        // Determine the assessment year to use for age calculation
        int AssessmentYear = assessmentYear;

        // Check if there's a rule override for assessment year
        if (masterData.AssessmentYearRule != null &&  !string.IsNullOrWhiteSpace(masterData.AssessmentYearRule.PolicyValue) && int.TryParse(masterData.AssessmentYearRule.PolicyValue, out int ruleAssessmentYear))
        {
            AssessmentYear = ruleAssessmentYear;
           
        }


        // Calculate age using the formula: AssessmentYear - constructionYear
        int ageOfProperty =  AssessmentYear - constructionYear;

         // Calculate factors and retrieve entities
        // Nature (NTB) Factor
        var ntbFactorKey = (propertyDetail.ConstructionTypeId, yearRange.Id);
        decimal ntbFactor = masterData.NatureFactors.GetValueOrDefault(ntbFactorKey) ?? 1;
        if (ntbFactor == 0) ntbFactor = 1; // Treat 0 as 1 to prevent zero calculations
        var ntbFactorEntity = masterData.NatureFactorEntities?.FirstOrDefault(x => x.ConstructionTypeId == propertyDetail.ConstructionTypeId && x.YearRangeCVId == yearRange.Id);

        if (ntbFactorEntity == null)
        {
            _logger.LogWarning(
                "NatureFactorEntity not found for PropertyDetailsId: {PropertyDetailsId}, ConstructionTypeId: {ConstructionTypeId}, YearRangeCVId: {YearRangeCVId}",
                propertyDetail.Id, propertyDetail.ConstructionTypeId, yearRange.Id);
        }

        // Use Factor
        decimal useFactor = 1;
        UseFactorCVMasterEntity? useFactorEntity = null;
        if (propertyDetail.SubTypeOfUseId.HasValue)
        {
            var useFactorKey = (propertyDetail.TypeOfUseId, yearRange.Id, propertyDetail.SubTypeOfUseId.Value);
            useFactor = masterData.UseFactors.GetValueOrDefault(useFactorKey) ?? 1;
            if (useFactor == 0) useFactor = 1; // Treat 0 as 1 to prevent zero calculations
            useFactorEntity = masterData.UseFactorEntities?.FirstOrDefault(x =>
                x.TypeOfUseId == propertyDetail.TypeOfUseId &&
                x.YearRangeCVId == yearRange.Id &&
                x.SubTypeOfUseId == propertyDetail.SubTypeOfUseId.Value);

            if (useFactorEntity == null)
            {
                _logger.LogWarning(
                    "UseFactorEntity not found for PropertyDetailsId: {PropertyDetailsId}, TypeOfUseId: {TypeOfUseId}, YearRangeCVId: {YearRangeCVId}, SubTypeOfUseId: {SubTypeOfUseId}",
                    propertyDetail.Id, propertyDetail.TypeOfUseId, yearRange.Id, propertyDetail.SubTypeOfUseId.Value);
            }
        }

        // Age Factor - if age is negative, set factor to 1
        decimal ageFactor = 1;
        AgeFactorCVMasterEntity? ageFactorEntity = null;

        if (ageOfProperty < 0)
        {
            _logger.LogDebug( "Negative age calculated for PropertyDetailsId: {PropertyDetailsId}. Setting age factor to 1.", propertyDetail.Id);
        }
        else
        {
            ageFactorEntity = masterData.AgeFactors.FirstOrDefault(x =>
                x.ConstructionTypeId == propertyDetail.ConstructionTypeId && 
                x.YearRangeCVId == yearRange.Id && 
                ageOfProperty >= x.AgeFrom && 
                ageOfProperty <= x.AgeTo);

            ageFactor = ageFactorEntity?.Factor ?? 1;
            if (ageFactor == 0) ageFactor = 1; // Treat 0 as 1 to prevent zero calculations
        }


        // Floor Factor
        decimal floorFactor = 1;
        FloorFactorCVMasterEntity? floorFactorEntity = null;
        if (propertyDetail.FloorId != 0)
        {
            if (masterData.FloorFactors.TryGetValue((propertyDetail.FloorId, yearRange.Id), out var floorFactorFromDict))
            {
                floorFactorEntity = floorFactorFromDict;
                floorFactor = hasLift ? floorFactorEntity?.FactorWithLift ?? 1
                    : floorFactorEntity?.FactorWithoutLift ?? 1;
                if (floorFactor == 0) floorFactor = 1; // Treat 0 as 1 to prevent zero calculations
            }
            else
            {
                _logger.LogWarning( "FloorFactorEntity not found for PropertyDetailsId: {PropertyDetailsId}, FloorId: {FloorId}, YearRangeCVId: {YearRangeCVId}",
                    propertyDetail.Id, propertyDetail.FloorId, yearRange.Id);
            }
        }
        double carpetArea = propertyDetail.CarpetAreaSqMeter ?? 0;
        double builtupArea = propertyDetail.BuiltupAreaSqMeter ?? 0;
        double selectedArea = 0;
        var AreaRule = masterData.CapitalValueAreaTypeRule;

        if (AreaRule == null)
        {
            throw new PolicyCodeNotFoundException("CapitalValueAreaType");
        }
 
        if (AreaRule.PolicyValue == "BuiltupArea")
        {
            selectedArea =  builtupArea > 0 ? builtupArea : carpetArea * 1.2;
        }
        else
        {
            selectedArea = carpetArea;
        }

        // Calculate capital value
        decimal calculationArea = (decimal)selectedArea;
        decimal rate = rateMaster.RateAmount.Value;
        decimal baseValue = rate * calculationArea;
        decimal capitalValue = baseValue * ntbFactor * useFactor * ageFactor * floorFactor;

        // Check if renter conditions are met and apply 0.75 multiplier
       
        if (propertyDetail.IsRenter == true)
        {
            if (masterData.RenterData.TryGetValue(propertyDetail.Id, out var renterMast))
            {
                if (!string.IsNullOrWhiteSpace(renterMast.TaxLiability) && renterMast.TaxLiability.Equals("Renter", StringComparison.OrdinalIgnoreCase))
                {
                    capitalValue = capitalValue * 0.75m;
                 }
            }
        }
         // Get applicable taxes
        var taxes = masterData.TaxData
            .Where(x => x.TypeOfUseId == propertyDetail.TypeOfUseId && x.YearRangeCVId == yearRange.Id)
             .GroupBy(x => x.TaxId)
            .Select(g => g.First())
            .ToList();

        if (!taxes.Any())
        {
            throw new TaxPercentageNotFoundException(
                propertyDetail.TypeOfUseId,
                propertyDetail.TypeOfUse?.Description ?? "Unknown",
                assessmentYear,
                yearRange.Id);
        }

        // Build CapitalValueDto directly
        var result = new CapitalValueDto
        {
            PropertyId = propertyId,
            PropertyDetailsId = propertyDetail.Id,
            CapitalValue = capitalValue,
            BaseValue = (double)baseValue,
            FloorFactor = (double)floorFactor,
            SDRR = (double)rate,
            UseFactor = (double)useFactor,
            NTBFactor = (double)ntbFactor,
            AgeFactor = (double)ageFactor,
            ConstructionYear = propertyDetail.ConstructionYear,
            AssessmentYear = propertyDetail.AssessmentYear,
            NoOfRooms = propertyDetail.NoOfRooms,
            CarpetAreaSqFeet = propertyDetail.CarpetAreaSqFeet,
            CarpetAreaSqMeter = propertyDetail.CarpetAreaSqMeter,
            BuiltupAreaSqMeter = propertyDetail.BuiltupAreaSqMeter,
            BuiltupAreaSqFeet = propertyDetail.BuiltupAreaSqFeet,
            FloorDescription = propertyDetail.Floor?.Description,
            SubFloorDescription = propertyDetail.SubFloor?.Description,
            ConstructionTypeDescription = propertyDetail.ConstructionType?.Description,
            TypeOfUseDescription = propertyDetail.TypeOfUse?.Description,
            SubTypeOfUseDescription = propertyDetail.SubTypeOfUse?.Description,
            Taxes = taxes.Select(t => new TaxHeadDto
            {
                TaxId = t.TaxId,
                TaxName = t.TaxMaster?.TaxName ?? string.Empty,
                Percentage = t.TaxPercentage,
                Amount = Math.Round(capitalValue * (t.TaxPercentage / 100), 2, MidpointRounding.AwayFromZero)
            }).ToList()
        };

        return new CapitalValueCalculationResult
        {
            Result = result,
            YearRange = yearRange,
            RateMaster = rateMaster,
            FloorFactorEntity = floorFactorEntity,
            AgeFactorEntity = ageFactorEntity,
            NatureFactorEntity = ntbFactorEntity,
            UseFactorEntity = useFactorEntity
        };
    }
}
