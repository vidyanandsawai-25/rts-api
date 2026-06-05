using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using System;
using System.Collections.Generic;
using System.Linq;


namespace NtisPlatform.Application.Services.TaxEngine
{
    public static class RateableValueCalculator
    {
        /// <summary>
        /// Calculates base values for property tax (backward compatible - uses default policies)
        /// </summary>
        public static PropertyTaxCalculationRVResultsEntity CalculateBaseValues(
            PropertyDetailsEntity detail,
            int financeYear,
            int taxZoneId,
            int wardId,
            List<TypeOfUseEntity> typeOfUses,
            List<RateEntity> rates,
            List<DepreciationMasterEntity> depreciations,
            List<AssessmentYearRangeEntity> yearRanges,
            List<RenterMastEntity> renters)
        {
            return CalculateBaseValues(detail, financeYear, taxZoneId, wardId, typeOfUses, rates, depreciations, yearRanges, renters, RateableValuePolicyOptions.Default);
        }

        /// <summary>
        /// Calculates base values for property tax using policy-based configuration.
        /// This overload computes the selected area internally.
        /// </summary>
        public static PropertyTaxCalculationRVResultsEntity CalculateBaseValues(
            PropertyDetailsEntity detail,
            int financeYear,
            int taxZoneId,
            int wardId,
            List<TypeOfUseEntity> typeOfUses,
            List<RateEntity> rates,
            List<DepreciationMasterEntity> depreciations,
            List<AssessmentYearRangeEntity> yearRanges,
            List<RenterMastEntity> renters,
            RateableValuePolicyOptions policyOptions,
            decimal? overrideRateSqM = null)
        {
            var options = policyOptions ?? RateableValuePolicyOptions.Default;
            decimal selectedArea = RateableValuePolicyHelper.GetSelectedArea(detail, options);
            return CalculateBaseValues(detail, financeYear, taxZoneId, wardId, typeOfUses, rates, depreciations, yearRanges, renters, selectedArea, options, overrideRateSqM);
        }

        /// <summary>
        /// Calculates base values for property tax using precomputed selected area.
        /// Use this overload when processing multiple property details for better performance.
        /// </summary>
        public static PropertyTaxCalculationRVResultsEntity CalculateBaseValues(
            PropertyDetailsEntity detail,
            int financeYear,
            int taxZoneId,
            int wardId,
            List<TypeOfUseEntity> typeOfUses,
            List<RateEntity> rates,
            List<DepreciationMasterEntity> depreciations,
            List<AssessmentYearRangeEntity> yearRanges,
            List<RenterMastEntity> renters,
            decimal selectedArea,
            RateableValuePolicyOptions policyOptions,
            decimal? overrideRateSqM = null,
            ILogger? logger = null)


        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));

            // If IsTaxable is explicitly false, skip calculations by returning zeros.
            if (detail.IsTaxable == false)
            {
                return new PropertyTaxCalculationRVResultsEntity
                {
                    PropertyId = detail.PropertyId,
                    PropertyDetailsId = detail.Id,
                    MonthlyRate = 0d,
                    YearlyRate = 0d,
                    YearlyRent = 0d,
                    Depreciation = 0m,
                    DepreciationPer = 0m,
                    AppliedOn = "Not Taxable",
                    AnnualRentalValue = 0d,
                    Maintenance = 0m,
                    RateableValue = 0m,
                    TotalAreaSqMtr = 0d,
                    RAreaSqMtr = 0d,
                    CAreaSqlMtr = 0d,
                };
            }

            var yearRange = ResolveYearRange(financeYear, yearRanges, logger);
            if (yearRange == null)
            {
                return new PropertyTaxCalculationRVResultsEntity
                {
                    PropertyId = detail.PropertyId,
                    PropertyDetailsId = detail.Id,
                    MonthlyRate = 0d,
                    YearlyRate = 0d,
                    YearlyRent = 0d,
                    Depreciation = 0m,
                    DepreciationPer = 0m,
                    AppliedOn = "Year Range Not Found",
                    AnnualRentalValue = 0d,
                    Maintenance = 0m,
                    RateableValue = 0m,
                    TotalAreaSqMtr = 0d,
                    RAreaSqMtr = 0d,
                    CAreaSqlMtr = 0d,
                };
            }

            decimal rentMonthly = 0;
            decimal rentYearly = 0;
            var typeOfUse = typeOfUses.FirstOrDefault(x => x.Id == detail.TypeOfUseId);
            if (typeOfUse == null)
            {
                logger?.LogWarning(
                    "TypeOfUse not found for PropertyDetailsId={PropertyDetailsId}, TypeOfUseId={TypeOfUseId}. Returning zero values.",
                    detail.Id, detail.TypeOfUseId);
                return new PropertyTaxCalculationRVResultsEntity
                {
                    PropertyId = detail.PropertyId,
                    PropertyDetailsId = detail.Id,
                    MonthlyRate = 0d,
                    YearlyRate = 0d,
                    YearlyRent = 0d,
                    Depreciation = 0m,
                    DepreciationPer = 0m,
                    AppliedOn = "TypeOfUse Not Found",
                    AnnualRentalValue = 0d,
                    Maintenance = 0m,
                    RateableValue = 0m,
                    TotalAreaSqMtr = 0d,
                    RAreaSqMtr = 0d,
                    CAreaSqlMtr = 0d,
                };
            }

            var rate = rates.FirstOrDefault(x =>
                x.TaxZoneId == taxZoneId &&
                x.ConstructionTypeId == detail.ConstructionTypeId &&
                x.TypeOfUseGroupId == typeOfUse.TypeOfUseGroupId &&
                x.YearRangeRVId == yearRange.Id &&
                x.IsActive);

            if (rate == null)
            {
                logger?.LogWarning(
                    "No matching rate found for PropertyDetailsId={PropertyDetailsId}, TaxZoneId={TaxZoneId}, ConstructionTypeId={ConstructionTypeId}, TypeOfUseGroupId={TypeOfUseGroupId}, YearRangeRVId={YearRangeRVId}. Falling back to zero rate.",
                    detail.Id, taxZoneId, detail.ConstructionTypeId, typeOfUse.TypeOfUseGroupId, yearRange.Id);
                rate = new RateEntity { RateSquareMeter = 0m, RateSquareFeet = 0m };
            }

            var options = policyOptions ?? RateableValuePolicyOptions.Default;

            // Select the appropriate rate based on area unit policy (uses precomputed IsSqFeetUnit)
            decimal ratePerUnit = options.IsSqFeetUnit ? (rate.RateSquareFeet ?? 0m) : (rate.RateSquareMeter ?? 0m);
            ratePerUnit = overrideRateSqM ?? ratePerUnit;
            // Calculate monthly and yearly rates based on policy (uses precomputed IsMonthlyRate)
            decimal monthlyRate;
            decimal yearlyRate;

            if (options.IsMonthlyRate)
            {
                // Rate is stored as monthly rate
                monthlyRate = ratePerUnit;
                yearlyRate = ratePerUnit * 12;
            }
            else
            {
                // Rate is stored as yearly rate (default)
                yearlyRate = ratePerUnit;
                monthlyRate = ratePerUnit / 12;
            }

            // Calculate yearly rent from area (uses precomputed IsMonthlyRate)
            decimal yearlyRentCalc = options.IsMonthlyRate
                ? selectedArea * ratePerUnit * 12
                : selectedArea * ratePerUnit;

            if (detail.IsRenter == true)
            {
                var rentRow = renters?.FirstOrDefault(r => r.PropertyDetailsId == detail.Id);
                if (rentRow != null)
                {
                    rentYearly = (decimal)(rentRow.FinalYearlyRent ?? 0d);
                    rentMonthly = (decimal)(rentRow.RentMonthly ?? 0d);
                }
            }

            var depreciationRate = ResolveDepreciationRate(detail, financeYear, depreciations);

            //decimal depreciationAmount = yearlyRate * depreciationRate / 100m;
            decimal depreciationAmount = yearlyRentCalc * depreciationRate / 100m;
            decimal yearlyRent = 0;
            var appliedOn = "";

            if (rentYearly > yearlyRentCalc)
            {
                yearlyRent = rentYearly;
                depreciationAmount = 0;
                appliedOn = "Rent";
            }
            else
            {
                yearlyRent = yearlyRentCalc;
                appliedOn = "Area";
            }

            decimal annualRentalValue = yearlyRent - depreciationAmount;


            annualRentalValue = Math.Round(annualRentalValue, 0, MidpointRounding.AwayFromZero);
            depreciationAmount = Math.Round(depreciationAmount, 0, MidpointRounding.AwayFromZero);

            decimal maintenance = Math.Round(annualRentalValue * 0.10m, 0, MidpointRounding.AwayFromZero);
            decimal rateableValue = Math.Round(annualRentalValue - maintenance, 0, MidpointRounding.AwayFromZero);

            // Convert selectedArea to square meters if it's in square feet (1 sq ft = 0.092903 sq m)
            decimal areaSqMtr = options.IsSqFeetUnit ? selectedArea * 0.092903m : selectedArea;

            return new PropertyTaxCalculationRVResultsEntity
            {
                PropertyId = detail.PropertyId,
                PropertyDetailsId = detail.Id,
                MonthlyRate = Math.Round(Convert.ToDouble(monthlyRate), 2),
                YearlyRate = Convert.ToDouble(yearlyRate),
                YearlyRent = Convert.ToDouble(yearlyRent),
                Depreciation = depreciationAmount,
                DepreciationPer = depreciationRate,
                AppliedOn = appliedOn,
                AnnualRentalValue = Convert.ToDouble(annualRentalValue),
                Maintenance = maintenance,
                RateableValue = rateableValue,
                TotalAreaSqMtr = Convert.ToDouble(areaSqMtr),
                RAreaSqMtr = typeOfUse.Type?.Equals("R", StringComparison.OrdinalIgnoreCase) == true ? Convert.ToDouble(areaSqMtr) : 0d,
                CAreaSqlMtr = typeOfUse.Type?.Equals("C", StringComparison.OrdinalIgnoreCase) == true ? Convert.ToDouble(areaSqMtr) : 0d
            };
        }

        private static AssessmentYearRangeEntity? ResolveYearRange(
            int financeYear,
            List<AssessmentYearRangeEntity> yearRanges,
            ILogger? logger = null)
        {
            var range = yearRanges.FirstOrDefault(x =>
                x.FromYear <= financeYear &&
                x.ToYear >= financeYear &&
                x.IsActive);

            if (range == null)
            {
                logger?.LogWarning(
                    "Assessment year range not found for FinanceYear={FinanceYear}. Returning null.",
                    financeYear);
            }

            return range;
        }




        private static decimal ResolveDepreciationRate(
            PropertyDetailsEntity detail,
            int financeYear,
            List<DepreciationMasterEntity> depreciations)
        {
            int constructionYear = 0;
            int.TryParse(detail.ConstructionYear, out constructionYear);
            int buildingAge = constructionYear > 0 ? Math.Max(0, financeYear - constructionYear) : 0;

            var dep = depreciations.FirstOrDefault(x =>
                x.ConstructionTypeId == detail.ConstructionTypeId &&
                x.IsActive &&
                (buildingAge >= x.MinYear && buildingAge <= x.MaxYear)
                //||
                //(x.Year.HasValue && x.Year.Value == buildingAge)
                );

            return dep?.Rate ?? 0m;
        }
    }
}
