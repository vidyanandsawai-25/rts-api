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

        public RVCalculationResultsEntity CalculateBaseValues(
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
            decimal? overrideRate = null,
            int? detailYearRangeRVId = null,
            decimal? overrideRent = null,
			bool isPlotProperty = false,
            decimal? overrideMaintenancePercent = null)
        {
            if (detail == null)
                throw new ArgumentNullException(nameof(detail));

            var options = policyOptions ?? RateableValuePolicyOptions.Default;

            if (detail.IsTaxable == false)
                return CreateZeroResult(detail, "Not Taxable");

            // Use the detail's resolved year range ID
            // If year range ID is 0 (means AssessmentYear not found), apply zero tax
            AssessmentYearRangeEntity? yearRange = null;
            if (detailYearRangeRVId.HasValue && detailYearRangeRVId.Value == 0)
            {
                return CreateZeroResult(detail, "Year Not Found");
            }

            if (detailYearRangeRVId.HasValue)
            {
                yearRange = yearRanges.FirstOrDefault(y => y.Id == detailYearRangeRVId.Value && y.IsActive);
            }

            if (yearRange == null)
                return CreateZeroResult(detail, "Year Not Found");

            var typeOfUse = typeOfUses.FirstOrDefault(x => x.Id == detail.TypeOfUseId);
            if (typeOfUse == null)
            {
                _logger.LogWarning(
                    "TypeOfUse not found for PropertyDetailsId={PropertyDetailsId}, TypeOfUseId={TypeOfUseId}. Returning zero values.",
                    detail.Id, detail.TypeOfUseId);
                return CreateZeroResult(detail, "TypeOfUse Not Found");
            }

            if (string.Equals(typeOfUse.Type, "N", StringComparison.OrdinalIgnoreCase))
                return CreateZeroResult(detail, "Type is N");

            // Plot / OpenPlot rule: tax is calculated only when Plot-category properties use an
            // OpenPlot type of use, and only when non-Plot properties use a non-OpenPlot type of use.
            // A Plot property with a non-OpenPlot use, or a non-Plot property with an OpenPlot use,
            // must not be taxed.
            bool isOpenPlotUse = string.Equals(
                typeOfUse.TypeOfUseCategory?.TypeOfUseCategoryCode,
                "OpenPlot",
                StringComparison.OrdinalIgnoreCase);

            if (isPlotProperty != isOpenPlotUse)
            {
                // AppliedOn is persisted to a nvarchar(20) column - keep these within that limit.
                return CreateZeroResult(detail, "OpenPlot");
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

            if (overrideRent.HasValue && overrideRent.Value > 0)
            {
                rentYearly = overrideRent.Value;
            }
            else if (detail.IsRenter == true && renters != null)
            {
                var rentRow = renters
                    .Where(r => r.PropertyDetailsId == detail.Id && r.IsActive && !r.MarkedForDeletion)
                    .OrderByDescending(r => r.CreatedDate)
                    .FirstOrDefault();

                if (rentRow != null)
                {
                    double yearlyRentValue = rentRow.FinalYearlyRent > 0
                        ? rentRow.FinalYearlyRent.Value
                        : ((rentRow.RentMonthly ?? 0d) * 12d);
                    rentYearly = Convert.ToDecimal(yearlyRentValue);
                }
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
            // Override via policy key RateableValuePolicyConstants.MaintenanceRateKey or explicit overrideMaintenancePercent argument.
            decimal maintenancePercent = overrideMaintenancePercent ?? options.MaintenanceRatePercent;
            decimal maintenance = Math.Round(
                annualRentalValue * maintenancePercent / 100m, 0,
                MidpointRounding.AwayFromZero);
            decimal rateableValue = Math.Round(annualRentalValue - maintenance, 0, MidpointRounding.AwayFromZero);

            decimal areaSqMtr = options.IsSqFeetUnit
                ? selectedArea * 0.092903m
                : selectedArea;

            return new RVCalculationResultsEntity
            {
                PropertyId = detail.PropertyId,
                PropertyDetailsId = detail.Id,

                MonthlyRate = (double)Math.Round(monthlyRate, 2),
                YearlyRate = (double)yearlyRate,
                YearlyRent = (double)yearlyRent,

                Depreciation = depreciationAmount,
                DepreciationPer = depreciationRate,
                AppliedOn = appliedOn,

                AnnualRentalValue = (double)annualRentalValue,
                Maintenance = maintenance,
                RateableValue = rateableValue,

                TotalAreaSqMtr = (double)areaSqMtr,

                RAreaSqMtr = string.Equals(typeOfUse.Type, "R", StringComparison.OrdinalIgnoreCase)
                    ? (double)areaSqMtr : 0d,

                CAreaSqlMtr = string.Equals(typeOfUse.Type, "C", StringComparison.OrdinalIgnoreCase)
                    ? (double)areaSqMtr : 0d
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
            int.TryParse(detail.AssessmentYear, out int assessmentYear);
            int buildingAge = assessmentYear > 0 ? Math.Max(0, financeYear - assessmentYear) : 0;

            var depreciation = depreciations.FirstOrDefault(x =>
                x.ConstructionTypeId == detail.ConstructionTypeId &&
                x.IsActive &&
                buildingAge >= x.MinYear &&
                buildingAge <= x.MaxYear);

            return depreciation?.Rate ?? 0m;
        }

        private static RVCalculationResultsEntity CreateZeroResult(PropertyDetailsEntity detail, string appliedOn)
        {
            return new RVCalculationResultsEntity
            {
                PropertyId = detail.PropertyId,
                PropertyDetailsId = detail.Id,
                MonthlyRate = 0d,
                YearlyRate = 0d,
                YearlyRent = 0d,
                Depreciation = 0m,
                DepreciationPer = 0m,
                AppliedOn = appliedOn,
                AnnualRentalValue = 0d,
                Maintenance = 0m,
                RateableValue = 0m,
                TotalAreaSqMtr = 0d,
                RAreaSqMtr = 0d,
                CAreaSqlMtr = 0d
            };
        }
    }
}
