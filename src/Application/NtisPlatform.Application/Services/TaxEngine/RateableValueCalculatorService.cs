using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NtisPlatform.Application.Services.TaxEngine
{
    public class RateableValueCalculatorService : IRateableValueCalculatorService
    {
        private readonly ILogger<RateableValueCalculatorService> _logger;

        public RateableValueCalculatorService(ILogger<RateableValueCalculatorService> logger)
        {
            _logger = logger;
        }

        public PropertyTaxCalculationRVResultsEntity CalculateBaseValues(
            PropertyDetailsEntity detail,
            int financeYear,
            int taxZoneId,
            int? wardId,
            List<TypeOfUseEntity> typeOfUses,
            List<RateEntity> rates,
            List<DepreciationMasterEntity> depreciations,
            List<AssessmentYearRangeEntity> yearRanges,
            IReadOnlyList<RenterMastEntity> renters,
            decimal selectedArea,
            RateableValuePolicyOptions policyOptions,
            decimal? overrideRate = null)
        {
            if (detail == null)
                throw new ArgumentNullException(nameof(detail));

            var options = policyOptions ?? RateableValuePolicyOptions.Default;

            if (detail.IsTaxable == false)
                return CreateZeroResult(detail, "Not Taxable");

            var yearRange = ResolveYearRange(financeYear, yearRanges);
            if (yearRange == null)
                return CreateZeroResult(detail, "Year Range Not Found");

            var typeOfUse = typeOfUses.FirstOrDefault(x => x.Id == detail.TypeOfUseId);
            if (typeOfUse == null)
            {
                _logger.LogWarning(
                    "TypeOfUse not found for PropertyDetailsId={PropertyDetailsId}, TypeOfUseId={TypeOfUseId}. Returning zero values.",
                    detail.Id, detail.TypeOfUseId);
                return CreateZeroResult(detail, "TypeOfUse Not Found");
            }

            var rate = rates.FirstOrDefault(x =>
                x.TaxZoneId == taxZoneId &&
                x.ConstructionTypeId == detail.ConstructionTypeId &&
                x.TypeOfUseGroupId == typeOfUse.TypeOfUseGroupId &&
                x.YearRangeRVId == yearRange.Id &&
                x.IsActive);

            if (rate == null)
            {
                _logger.LogWarning(
                    "No rate found for PropertyDetailsId={PropertyDetailsId}, TaxZoneId={TaxZoneId}, " +
                    "ConstructionTypeId={ConstructionTypeId}, TypeOfUseGroupId={TypeOfUseGroupId}, YearRangeRVId={YearRangeRVId}.",
                    detail.Id, taxZoneId, detail.ConstructionTypeId, typeOfUse.TypeOfUseGroupId, yearRange.Id);
            }

            decimal ratePerUnit = overrideRate ?? RateableValueCalculator.GetRatePerUnit(rate, options);

            decimal monthlyRate;
            decimal yearlyRate;

            if (options.IsMonthlyRate)
            {
                monthlyRate = ratePerUnit;
                yearlyRate = ratePerUnit * 12;
            }
            else
            {
                yearlyRate = ratePerUnit;
                monthlyRate = ratePerUnit / 12;
            }

            decimal yearlyRentCalculated = options.IsMonthlyRate
                ? selectedArea * ratePerUnit * 12
                : selectedArea * ratePerUnit;

            decimal rentYearly = 0m;

            if (detail.IsRenter == true)
            {
                var rentRow = renters?.FirstOrDefault(r => r.PropertyDetailsId == detail.Id);
                if (rentRow != null)
                    rentYearly = Convert.ToDecimal(rentRow.FinalYearlyRent ?? 0d);
            }

            decimal depreciationRate = ResolveDepreciationRate(detail, financeYear, depreciations);
            decimal depreciationAmount = yearlyRentCalculated * depreciationRate / 100m;

            decimal yearlyRent;
            string appliedOn;

            if (rentYearly > yearlyRentCalculated)
            {
                yearlyRent = rentYearly;
                depreciationAmount = 0m;
                appliedOn = "Rent";
            }
            else
            {
                yearlyRent = yearlyRentCalculated;
                appliedOn = "Area";
            }

            decimal annualRentalValue = Math.Round(yearlyRent - depreciationAmount, 0, MidpointRounding.AwayFromZero);
            depreciationAmount = Math.Round(depreciationAmount, 0, MidpointRounding.AwayFromZero);

            // Maintenance deduction is policy-driven. Default is 10% (see RateableValuePolicyConstants.DefaultMaintenanceRateValue).
            // Override via policy key RateableValuePolicyConstants.MaintenanceRateKey.
            decimal maintenance = Math.Round(
                annualRentalValue * options.MaintenanceRatePercent / 100m, 0,
                MidpointRounding.AwayFromZero);
            decimal rateableValue = Math.Round(annualRentalValue - maintenance, 0, MidpointRounding.AwayFromZero);

            decimal areaSqMtr = options.IsSqFeetUnit
                ? selectedArea * 0.092903m
                : selectedArea;

            return new PropertyTaxCalculationRVResultsEntity
            {
                PropertyId = detail.PropertyId,
                PropertyDetailsId = detail.Id,

                MonthlyRate = Math.Round(monthlyRate, 2),
                YearlyRate = yearlyRate,
                YearlyRent = yearlyRent,

                Depreciation = depreciationAmount,
                DepreciationPer = depreciationRate,
                AppliedOn = appliedOn,

                AnnualRentalValue = annualRentalValue,
                Maintenance = maintenance,
                RateableValue = rateableValue,

                TotalAreaSqMtr = areaSqMtr,

                RAreaSqMtr = string.Equals(typeOfUse.Type, "R", StringComparison.OrdinalIgnoreCase)
                    ? areaSqMtr : 0m,

                CAreaSqlMtr = string.Equals(typeOfUse.Type, "C", StringComparison.OrdinalIgnoreCase)
                    ? areaSqMtr : 0m
            };
        }

        private AssessmentYearRangeEntity? ResolveYearRange(int financeYear, List<AssessmentYearRangeEntity> yearRanges)
        {
            var range = yearRanges.FirstOrDefault(x =>
                x.FromYear <= financeYear && x.ToYear >= financeYear && x.IsActive);

            if (range == null)
                _logger.LogWarning("Assessment year range not found for FinanceYear={FinanceYear}.", financeYear);

            return range;
        }

        private decimal ResolveDepreciationRate(PropertyDetailsEntity detail, int financeYear, List<DepreciationMasterEntity> depreciations)
        {
            int.TryParse(detail.ConstructionYear, out int constructionYear);
            int buildingAge = constructionYear > 0 ? Math.Max(0, financeYear - constructionYear) : 0;

            var depreciation = depreciations.FirstOrDefault(x =>
                x.ConstructionTypeId == detail.ConstructionTypeId &&
                x.IsActive &&
                buildingAge >= x.MinYear &&
                buildingAge <= x.MaxYear);

            return depreciation?.Rate ?? 0m;
        }

        private static PropertyTaxCalculationRVResultsEntity CreateZeroResult(PropertyDetailsEntity detail, string appliedOn)
        {
            return new PropertyTaxCalculationRVResultsEntity
            {
                PropertyId = detail.PropertyId,
                PropertyDetailsId = detail.Id,
                MonthlyRate = 0m,
                YearlyRate = 0m,
                YearlyRent = 0m,
                Depreciation = 0m,
                DepreciationPer = 0m,
                AppliedOn = appliedOn,
                AnnualRentalValue = 0m,
                Maintenance = 0m,
                RateableValue = 0m,
                TotalAreaSqMtr = 0m,
                RAreaSqMtr = 0m,
                CAreaSqlMtr = 0m
            };
        }
    }
}
