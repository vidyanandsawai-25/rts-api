using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NtisPlatform.Application.Services.TaxEngine
{
    public static class RateableValueCalculator
    {
        public static PropertyTaxCalculationRVResultsEntity CalculateBaseValues(
            PropertyDetailsEntity detail,
            int financeYear,
            int taxZoneId,
            int wardId,
            List<TypeOfUseEntity> typeOfUses,
            List<RateEntity> rates,
            List<DepreciationMasterEntity> depreciations,
            List<AssessmentYearRangeEntity> yearRanges,
            List<RenterMastEntity> renters) // <-- add this

        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));

            var yearRange = ResolveYearRange(financeYear, yearRanges);

            decimal rentMonthly = 0;
            decimal rentYearly = 0;
            var typeOfUse = typeOfUses.FirstOrDefault(x => x.Id == detail.TypeOfUseId);
            if (typeOfUse == null)
                throw new InvalidOperationException($"TypeOfUse not found for TypeOfUseId={detail.TypeOfUseId}");

            var rate = rates.FirstOrDefault(x =>
                x.TaxZoneId == taxZoneId &&
                x.ConstructionTypeId == detail.ConstructionTypeId &&
                x.TypeOfUseGroupId == typeOfUse.TypeOfUseGroupId &&
                x.YearRangeRVId == yearRange.Id &&
                x.IsActive);

            if (rate == null)
                throw new InvalidOperationException(
                    $"Rate not found for TaxZoneId={taxZoneId}, WardId={wardId}, FloorId={detail.FloorId}, ConstructionTypeId={detail.ConstructionTypeId}, TypeOfUseGroupId={typeOfUse.TypeOfUseGroupId}, YearRangeRVId={yearRange.Id}");

            decimal areaSqM = Convert.ToDecimal(detail.CarpetAreaSqMeter ?? 0d);
            decimal rateSqM = rate.RateSquareMeter ?? 0m;
            decimal monthlyRate = rateSqM / 12;
            decimal yearlyRate = rateSqM ;

            decimal yearlyRentCalc = rateSqM * areaSqM;
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

            if (rentYearly > yearlyRentCalc)

            {
                yearlyRent = rentYearly;
                depreciationAmount = 0;
            }

            else
                yearlyRent = yearlyRentCalc;

           decimal annualRentalValue = yearlyRent - depreciationAmount;
                      

            annualRentalValue = Math.Round(annualRentalValue, 0, MidpointRounding.AwayFromZero);
            depreciationAmount = Math.Round(depreciationAmount, 0, MidpointRounding.AwayFromZero);

            decimal maintenance = Math.Round(annualRentalValue * 0.10m, 0, MidpointRounding.AwayFromZero);
            decimal rateableValue = Math.Round(annualRentalValue - maintenance, 0, MidpointRounding.AwayFromZero);

            return new PropertyTaxCalculationRVResultsEntity
            {
                PropertyId = detail.PropertyId,
                PropertyDetailsId = detail.Id,
                MonthlyRate = Math.Round(Convert.ToDouble(monthlyRate), 2),
                YearlyRate = Convert.ToDouble(yearlyRate),
                YearlyRent = Convert.ToDouble(yearlyRent),
                Depreciation = depreciationAmount,
                AnnualRentalValue = Convert.ToDouble(annualRentalValue),
                Maintenance = maintenance,
                RateableValue = rateableValue,
                TotalAreaSqMtr = Convert.ToDouble(areaSqM),
                RAreaSqMtr = typeOfUse.Type?.Equals("R", StringComparison.OrdinalIgnoreCase) == true ? Convert.ToDouble(areaSqM) : 0d,
                CAreaSqlMtr = typeOfUse.Type?.Equals("C", StringComparison.OrdinalIgnoreCase) == true ? Convert.ToDouble(areaSqM) : 0d
            };
        }

        private static AssessmentYearRangeEntity ResolveYearRange(
            int financeYear,
            List<AssessmentYearRangeEntity> yearRanges)
        {
            var range = yearRanges.FirstOrDefault(x =>
                x.FromYear <= financeYear &&
                x.ToYear >= financeYear &&
                x.IsActive);

            if (range == null)
                throw new InvalidOperationException($"Assessment year range not found for financeYear={financeYear}");

            return range;
        }


        // Removed ResolveDeclaredYearlyRent: rent values are now passed in as parameters

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
