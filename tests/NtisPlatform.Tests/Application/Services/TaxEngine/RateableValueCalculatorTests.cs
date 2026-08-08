using Microsoft.Extensions.Logging.Abstractions;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using System;
using System.Collections.Generic;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.TaxEngine
{
    public class RateableValueCalculatorTests
    {
        // Convenience wrapper matching the old static signature (no selectedArea / policyOptions).
        // Computes selectedArea from policyOptions so call sites stay concise.
        private static RVCalculationResultsEntity Calculate(
            PropertyDetailsEntity detail,
            int financeYear,
            int taxZoneId,
            int wardId,
            List<TypeOfUseEntity> typeOfUses,
            List<RateEntity> rates,
            List<DepreciationMasterEntity> depreciations,
            List<AssessmentYearRangeEntity> yearRanges,
            List<RenterMastEntity> renters,
            RateableValuePolicyOptions? policyOptions = null)
        {
            var options = policyOptions ?? RateableValuePolicyOptions.Default;
            var selectedArea = RateableValuePolicyHelper.GetSelectedArea(detail, options);

            // If year ranges are provided, use the first active one's ID
            int? detailYearRangeRVId = null;
            if (yearRanges?.Any() == true)
            {
                var yearRange = yearRanges.FirstOrDefault(y => y.IsActive);
                detailYearRangeRVId = yearRange?.Id ?? (yearRanges.Count > 0 ? yearRanges[0].Id : null);
            }

            return new RateableValueCalculatorService(NullLogger<RateableValueCalculatorService>.Instance)
                .CalculateBaseValues(detail, financeYear, taxZoneId, wardId, typeOfUses, rates,
                    depreciations, yearRanges ?? new List<AssessmentYearRangeEntity>(), renters, selectedArea, options, null, detailYearRangeRVId);
        }

        [Fact]
        public void CalculateBaseValues_WhenDetailIsNull_ThrowsArgumentNullException()
        {
            PropertyDetailsEntity? detail = null;
            var exception = Assert.Throws<ArgumentNullException>(() =>
                Calculate(detail!, 2024, 1, 1,
                    new List<TypeOfUseEntity>(), new List<RateEntity>(),
                    new List<DepreciationMasterEntity>(), new List<AssessmentYearRangeEntity>(),
                    new List<RenterMastEntity>()));

            Assert.Equal("detail", exception.ParamName);
        }

        [Fact]
        public void CalculateBaseValues_WhenIsTaxableIsFalse_ReturnsZeroedResult()
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = false,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1, ConstructionYear = "2020"
            };

            var result = Calculate(detail, 2024, 1, 1,
                new List<TypeOfUseEntity>(), new List<RateEntity>(),
                new List<DepreciationMasterEntity>(), new List<AssessmentYearRangeEntity>(),
                new List<RenterMastEntity>());

            Assert.NotNull(result);
            Assert.Equal(1, result.PropertyId);
            Assert.Equal(100, result.PropertyDetailsId);
            Assert.Equal(0d, result.MonthlyRate);
            Assert.Equal(0d, result.YearlyRate);
            Assert.Equal(0d, result.YearlyRent);
            Assert.Equal(0m, result.Depreciation);
            Assert.Equal(0m, result.DepreciationPer);
            Assert.Equal("Not Taxable", result.AppliedOn);
            Assert.Equal(0d, result.AnnualRentalValue);
            Assert.Equal(0m, result.Maintenance);
            Assert.Equal(0m, result.RateableValue);
            Assert.Equal(0d, result.RAreaSqMtr);
        }

        [Fact]
        public void CalculateBaseValues_WhenIsTaxableIsNull_DoesNotShortCircuit()
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = null,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1,
                CarpetAreaSqMeter = 100, ConstructionYear = "2020"
            };

            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, Type = "R", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId??0, ConstructionTypeId = detail.ConstructionTypeId ?? 0, YearRangeRVId = 1, RateSquareMeter = 1000m, IsActive = true } };
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
            };

            var result = Calculate(detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>());

            Assert.NotNull(result);
            Assert.Equal(1, result.PropertyId);
            Assert.Equal(100, result.PropertyDetailsId);
            Assert.NotEqual("Not Taxable", result.AppliedOn);
        }

        [Fact]
        public void CalculateBaseValues_WhenIsTaxableIsTrue_PerformsCalculation()
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = true,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1,
                CarpetAreaSqMeter = 10d, ConstructionYear = "2020"
            };

            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId ?? 0, ConstructionTypeId = detail.ConstructionTypeId ?? 0, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSquareMeter = 1200m, IsActive = true } };
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
            };

            var result = Calculate(detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>());

            Assert.NotNull(result);
            Assert.Equal(1, result.PropertyId);
            Assert.Equal(100, result.PropertyDetailsId);
        }

        [Theory]
        [InlineData(false, "Not Taxable")]
        [InlineData(true, null)]
        public void CalculateBaseValues_AppliedOnProperty_SetCorrectlyBasedOnIsTaxable(bool isTaxable, string? expectedAppliedOn)
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = isTaxable,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1,
                CarpetAreaSqMeter = 10d, ConstructionYear = "2020"
            };

            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId ?? 0, ConstructionTypeId = detail.ConstructionTypeId ?? 0, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSquareMeter = 1200m, IsActive = true } };
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
            };

            var result = Calculate(detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>());

            Assert.NotNull(result);
            if (expectedAppliedOn != null)
            {
                Assert.Equal(expectedAppliedOn, result.AppliedOn);
            }
            else
            {
                Assert.NotEqual("Not Taxable", result.AppliedOn ?? string.Empty);
            }
        }

        [Fact]
        public void CalculateBaseValues_WhenIsTaxableIsFalse_AllNumericFieldsAreZero()
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = false,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1, ConstructionYear = "2020"
            };

            var yearRanges = new List<AssessmentYearRangeEntity> { new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true } };

            var result = Calculate(detail, 2024, 1, 1,
                new List<TypeOfUseEntity>(), new List<RateEntity>(),
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>());

            Assert.Equal(0d, result.MonthlyRate);
            Assert.Equal(0d, result.YearlyRate);
            Assert.Equal(0d, result.YearlyRent);
            Assert.Equal(0m, result.Depreciation);
            Assert.Equal(0m, result.DepreciationPer);
            Assert.Equal(0d, result.AnnualRentalValue);
            Assert.Equal(0m, result.Maintenance);
            Assert.Equal(0m, result.RateableValue);
            Assert.Equal(0d, result.RAreaSqMtr);
        }

        [Fact]
        public void CalculateBaseValues_WhenTypeOfUseTypeIsN_ReturnsZeroedResult()
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = true,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1,
                CarpetAreaSqMeter = 100d, ConstructionYear = "2020"
            };

            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, Type = "N", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId ?? 0, ConstructionTypeId = detail.ConstructionTypeId ?? 0, YearRangeRVId = 1, RateSquareMeter = 1000m, IsActive = true } };
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
            };

            var result = Calculate(detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>());

            Assert.NotNull(result);
            Assert.Equal(1, result.PropertyId);
            Assert.Equal(100, result.PropertyDetailsId);
            Assert.Equal(0d, result.MonthlyRate);
            Assert.Equal(0d, result.YearlyRate);
            Assert.Equal(0d, result.YearlyRent);
            Assert.Equal(0m, result.Depreciation);
            Assert.Equal(0m, result.DepreciationPer);
            Assert.Equal("Type is N", result.AppliedOn);
            Assert.Equal(0d, result.AnnualRentalValue);
            Assert.Equal(0m, result.Maintenance);
            Assert.Equal(0m, result.RateableValue);
            Assert.Equal(0d, result.RAreaSqMtr);
        }

        [Fact]
        public void CalculateBaseValues_WhenTypeOfUseTypeIsNLowercase_ReturnsZeroedResult()
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = true,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1,
                CarpetAreaSqMeter = 100d, ConstructionYear = "2020"
            };

            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, Type = "n", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId ?? 0, ConstructionTypeId = detail.ConstructionTypeId ?? 0, YearRangeRVId = 1, RateSquareMeter = 1000m, IsActive = true } };
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
            };

            var result = Calculate(detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>());

            Assert.NotNull(result);
            Assert.Equal("Type is N", result.AppliedOn);
            Assert.Equal(0m, result.RateableValue);
        }

        [Fact]
        public void CalculateBaseValues_WithRenter_WhenFinalYearlyRentIsZero_CalculatesFromMonthlyRent()
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = true, IsRenter = true,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1,
                CarpetAreaSqMeter = 10d, ConstructionYear = "2020"
            };

            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId ?? 0, ConstructionTypeId = detail.ConstructionTypeId ?? 0, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSquareMeter = 100m, IsActive = true } };
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
            };

            // Area rent = 10 * 100 * 12 = 12,000 yearly
            // Renter monthly rent = 2,000 -> 24,000 yearly (higher than 12,000)
            var renters = new List<RenterMastEntity>
            {
                new()
                {
                    Id = 1,
                    PropertyDetailsId = 100,
                    RentMonthly = 2000,
                    FinalYearlyRent = 0,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedDate = DateTime.Now
                }
            };

            var result = Calculate(detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, renters);

            Assert.NotNull(result);
            Assert.Equal("Rent", result.AppliedOn);
            Assert.Equal(24000.0, result.YearlyRent);
            Assert.Equal(0m, result.Depreciation); // No depreciation when actual rent applied
        }

        [Fact]
        public void CalculateBaseValues_WithMultipleRenters_SelectsLatestActiveRenter()
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = true, IsRenter = true,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1,
                CarpetAreaSqMeter = 10d, ConstructionYear = "2020"
            };

            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId ?? 0, ConstructionTypeId = detail.ConstructionTypeId ?? 0, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSquareMeter = 100m, IsActive = true } };
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
            };

            var renters = new List<RenterMastEntity>
            {
                new()
                {
                    Id = 1,
                    PropertyDetailsId = 100,
                    FinalYearlyRent = 15000,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedDate = DateTime.Now.AddDays(-10)
                },
                new()
                {
                    Id = 2,
                    PropertyDetailsId = 100,
                    FinalYearlyRent = 30000,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedDate = DateTime.Now // Latest
                }
            };

            var result = Calculate(detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, renters);

            Assert.NotNull(result);
            Assert.Equal("Rent", result.AppliedOn);
            Assert.Equal(30000.0, result.YearlyRent);
        }

        [Fact]
        public void CalculateBaseValues_WithOverrideRent_UsesOverrideRent()
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = true, IsRenter = true,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1,
                CarpetAreaSqMeter = 10d, ConstructionYear = "2020"
            };

            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId ?? 0, ConstructionTypeId = detail.ConstructionTypeId ?? 0, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSquareMeter = 100m, IsActive = true } };
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
            };

            // Area rent = 10 * 100 * 12 = 12,000
            // Override rent (rule-adjusted rent) = 20,000
            var service = new RateableValueCalculatorService(Microsoft.Extensions.Logging.Abstractions.NullLogger<RateableValueCalculatorService>.Instance);
            var result = service.CalculateBaseValues(
                detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>(),
                10m, RateableValuePolicyOptions.Default, null, 1, 20000m);

            Assert.NotNull(result);
            Assert.Equal("Rent", result.AppliedOn);
            Assert.Equal(20000.0, result.YearlyRent);
            Assert.Equal(0m, result.Depreciation);
        }

        private static (PropertyDetailsEntity detail, List<TypeOfUseEntity> typeOfUses, List<RateEntity> rates, List<AssessmentYearRangeEntity> yearRanges) BuildPlotRuleFixture(string? typeOfUseCategoryCode)
        {
            var detail = new PropertyDetailsEntity
            {
                Id = 100, PropertyId = 1, IsTaxable = true, IsRenter = false,
                TypeOfUseId = 1, ConstructionTypeId = 1, FloorId = 1,
                CarpetAreaSqMeter = 10d, ConstructionYear = "2020"
            };

            var typeOfUses = new List<TypeOfUseEntity>
            {
                new()
                {
                    Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true,
                    TypeOfUseCategory = typeOfUseCategoryCode == null
                        ? null
                        : new TypeOfUseCategoryEntity { Id = 1, TypeOfUseCategoryCode = typeOfUseCategoryCode }
                }
            };

            var rates = new List<RateEntity>
            {
                new() { TaxZoneId = 1, FloorId = detail.FloorId ?? 0, ConstructionTypeId = detail.ConstructionTypeId ?? 0, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSquareMeter = 100m, IsActive = true }
            };

            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
            };

            return (detail, typeOfUses, rates, yearRanges);
        }

        [Fact]
        public void CalculateBaseValues_WhenPlotPropertyWithOpenPlotUse_CalculatesTax()
        {
            var (detail, typeOfUses, rates, yearRanges) = BuildPlotRuleFixture("OpenPlot");

            var service = new RateableValueCalculatorService(NullLogger<RateableValueCalculatorService>.Instance);
            var result = service.CalculateBaseValues(
                detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>(),
                10m, RateableValuePolicyOptions.Default, null, 1, null, isPlotProperty: true);

            Assert.NotNull(result);
            Assert.NotEqual(0m, result.RateableValue);
        }

        [Fact]
        public void CalculateBaseValues_WhenPlotPropertyWithNonOpenPlotUse_ReturnsZeroedResult()
        {
            var (detail, typeOfUses, rates, yearRanges) = BuildPlotRuleFixture("Utility");

            var service = new RateableValueCalculatorService(NullLogger<RateableValueCalculatorService>.Instance);
            var result = service.CalculateBaseValues(
                detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>(),
                10m, RateableValuePolicyOptions.Default, null, 1, null, isPlotProperty: true);

            Assert.NotNull(result);
            Assert.Equal(0m, result.RateableValue);
            Assert.Equal("OpenPlot", result.AppliedOn);
        }

        [Fact]
        public void CalculateBaseValues_WhenNonPlotPropertyWithOpenPlotUse_ReturnsZeroedResult()
        {
            var (detail, typeOfUses, rates, yearRanges) = BuildPlotRuleFixture("OpenPlot");

            var service = new RateableValueCalculatorService(NullLogger<RateableValueCalculatorService>.Instance);
            var result = service.CalculateBaseValues(
                detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>(),
                10m, RateableValuePolicyOptions.Default, null, 1, null, isPlotProperty: false);

            Assert.NotNull(result);
            Assert.Equal(0m, result.RateableValue);
            Assert.Equal("OpenPlot", result.AppliedOn);
        }

        [Fact]
        public void CalculateBaseValues_WhenNonPlotPropertyWithNonOpenPlotUse_CalculatesTax()
        {
            var (detail, typeOfUses, rates, yearRanges) = BuildPlotRuleFixture("Utility");

            var service = new RateableValueCalculatorService(NullLogger<RateableValueCalculatorService>.Instance);
            var result = service.CalculateBaseValues(
                detail, 2024, 1, 1, typeOfUses, rates,
                new List<DepreciationMasterEntity>(), yearRanges, new List<RenterMastEntity>(),
                10m, RateableValuePolicyOptions.Default, null, 1, null, isPlotProperty: false);

            Assert.NotNull(result);
            Assert.NotEqual(0m, result.RateableValue);
        }
    }
}
