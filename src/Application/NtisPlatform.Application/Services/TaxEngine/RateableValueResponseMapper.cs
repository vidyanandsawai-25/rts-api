using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using System;
using System.Collections.Generic;
using System.Linq;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Application.Services.TaxEngine
{
    public static class RateableValueResponseMapper
    {
        public static RateableValueResponseDto Map(
            int propertyId,
            int financeYear,
            IReadOnlyList<PropertyDetailsEntity> details,
            List<PropertyTaxCalculationRVResultsEntity> resultRows,
            List<PolicyTaxDetailsEntity> policyRows,
            IReadOnlyList<FloorEntity> floors,
            IReadOnlyList<ConstructionTypeEntity> constructionTypes,
            IReadOnlyList<TypeOfUseEntity> typeOfUses,
            IReadOnlyList<SubTypeOfUseEntity> subTypeOfUses,
            IReadOnlyList<SubFloorEntity> subFloors,
            IReadOnlyList<RenterMastEntity> renters,
            IReadOnlyList<PropertyOccupancyDetailsEntity> occupancies,
            TaxGetterCache<TaxMasterEntity> taxMasterCache)
        {
            var floorMap = floors.ToDictionary(x => x.Id, x => x.Description ?? x.FloorCode ?? string.Empty);
            var constructionTypeMap = constructionTypes.ToDictionary(
                x => x.Id,
                x => !string.IsNullOrWhiteSpace(x.ConstructionCode)
                    ? x.ConstructionCode!
                    : (x.Description ?? string.Empty));

            var typeOfUseMap = typeOfUses.ToDictionary(x => x.Id, x => x.Description ?? string.Empty);
            var subTypeOfUseMap = subTypeOfUses.ToDictionary(x => x.Id, x => x.Description ?? string.Empty);
            var subFloorMap = subFloors.ToDictionary(x => x.Id, x => x.Description ?? x.SubFloorCode ?? string.Empty);
            var detailMap = details.ToDictionary(x => x.Id, x => x);

            var renterMap = renters
                .Where(x => x.IsActive && !x.MarkedForDeletion)
                .GroupBy(x => x.PropertyDetailsId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).FirstOrDefault());


            var occupancyMap = occupancies
                .Where(x => x.IsActive && !x.MarkedForDeletion)
                .GroupBy(x => x.PropertyDetailId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).FirstOrDefault());


            var detailDtos = resultRows
                .GroupBy(x => x.PropertyDetailsId)
                .Select(g =>
                {
                    var first = g.First();
                    var detail = detailMap[g.Key];

                    var renter = renterMap.TryGetValue(detail.Id, out var r) ? r : null;
                    var occupancy = occupancyMap.TryGetValue(detail.Id, out var o) ? o : null;

                    var taxes = g
                        .OrderBy(x => x.TaxId)
                        .GroupBy(x => taxMasterCache.GetTaxName(x.TaxId), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Sum(v => v.TaxAmount ?? 0m),
                            StringComparer.OrdinalIgnoreCase);

                    return new RateableValueDetailDto
                    {
                        PropertyDetailsId = detail.Id,
                        Taxable = detail.IsTaxable ?? true,
                        Floor = floorMap.TryGetValue(detail.FloorId, out var floorName) ? floorName : string.Empty,
                        SubFloor = detail.SubFloorId.HasValue && subFloorMap.TryGetValue(detail.SubFloorId.Value, out var subFloorName) ? subFloorName : string.Empty,
                        ConstructionYear = detail.ConstructionYear ?? string.Empty,
                        AssessmentYear = detail.AssessmentYear ?? string.Empty,
                        ConstructionType = constructionTypeMap.TryGetValue(detail.ConstructionTypeId, out var conType) ? conType : string.Empty,
                        Use = typeOfUseMap.TryGetValue(detail.TypeOfUseId, out var useName) ? useName : string.Empty,
                        SubTypeOfUse = detail.SubTypeOfUseId.HasValue && subTypeOfUseMap.TryGetValue(detail.SubTypeOfUseId.Value, out var subUse)
                            ? subUse
                            : string.Empty,
                        NoOfRooms = detail.NoOfRooms ?? 0,
                        CarpetAreaSqFeet = detail.CarpetAreaSqFeet ?? 0d,
                        CarpetAreaSqMeter = detail.CarpetAreaSqMeter ?? 0d,
                        BuiltupAreaSqFeet = detail.BuiltupAreaSqFeet ?? 0d,
                        BuiltupAreaSqMeter = detail.BuiltupAreaSqMeter ?? 0d,
                        OccupancyNumber = occupancy?.OccupancyNumber ?? string.Empty,
                        OccupancyDate = occupancy?.OccupancyDate,
                        RenterName = !string.IsNullOrWhiteSpace(renter?.RenterNameEnglish)
                            ? renter.RenterNameEnglish!
                            : (renter?.RenterName ?? string.Empty),
                        RentMonthly = ToDecimal(renter?.RentMonthly),
                        RentYearly = ToDecimal(renter?.FinalYearlyRent > 0 ? renter.FinalYearlyRent : ((renter?.RentMonthly ?? 0d) * 12d)),
                        MonthlyRate = first.MonthlyRate ?? 0m,
                        YearlyRate = first.YearlyRate ?? 0m,
                        YearlyRent = first.YearlyRent ?? 0m,
                        Depreciation = first.Depreciation ?? 0m,
                        DepreciationPer = Math.Round(first.DepreciationPer ?? 0m, 2),
                        AppliedOn = first.AppliedOn ?? string.Empty,
                        AnnualRentalValue = first.AnnualRentalValue ?? 0m,
                        Maintenance = first.Maintenance ?? 0m,
                        RateableValue = first.RateableValue ?? 0m,
                        TaxTotal = g.Sum(x => x.TaxAmount ?? 0m),
                        Taxes = taxes
                    };
                })
                .OrderBy(x => x.PropertyDetailsId)
                .ToList();

            PolicyTaxDto? policyDto = null;

            if (policyRows != null && policyRows.Count > 0)
            {
                var firstPolicy = policyRows.First();

                policyDto = new PolicyTaxDto
                {
                    PolicyCode = firstPolicy.PolicyCode,
                    PolicyDate = firstPolicy.PolicyDate,
                    PolicyYear = firstPolicy.PolicyYear,
                    PolicyRVorCVvalue = firstPolicy.PolicyRVorCVvalue ?? 0m,
                    TaxTotal = policyRows.Sum(x => x.TaxAmount ?? 0m),
                    Taxes = policyRows
                        .OrderBy(x => x.TaxId)
                        .GroupBy(x => taxMasterCache.GetTaxName(x.TaxId), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Sum(v => v.TaxAmount ?? 0m),
                            StringComparer.OrdinalIgnoreCase)
                };
            }

            return new RateableValueResponseDto
            {
                PropertyId = propertyId,
                FinanceYear = financeYear,
                TotalRateableValue = detailDtos.Sum(x => x.RateableValue),
                // Use policy row total when available to avoid double-counting education/employment taxes
                // (which are calculated at property-type level but duplicated across detail rows)
                TotalTax = policyDto?.TaxTotal ?? detailDtos.Sum(x => x.TaxTotal),
                Policy = policyDto,
                Details = detailDtos
            };
        }

        private static decimal ToDecimal(double? value)
        {
            return Convert.ToDecimal(value ?? 0d);
        }
    }
}
