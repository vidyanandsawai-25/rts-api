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
        private static PropertyTaxCalculationRVResultsEntity Calculate(
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
            return new RateableValueCalculatorService(NullLogger<RateableValueCalculatorService>.Instance)
                .CalculateBaseValues(detail, financeYear, taxZoneId, wardId, typeOfUses, rates,
                    depreciations, yearRanges, renters, selectedArea, options);
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
            Assert.Equal(0m, result.MonthlyRate);
            Assert.Equal(0m, result.YearlyRate);
            Assert.Equal(0m, result.YearlyRent);
            Assert.Equal(0m, result.Depreciation);
            Assert.Equal(0m, result.DepreciationPer);
            Assert.Equal("Not Taxable", result.AppliedOn);
            Assert.Equal(0m, result.AnnualRentalValue);
            Assert.Equal(0m, result.Maintenance);
            Assert.Equal(0m, result.RateableValue);
            Assert.Equal(0m, result.RAreaSqMtr);
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
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId, ConstructionTypeId = detail.ConstructionTypeId, YearRangeRVId = 1, RateSquareMeter = 1000m, IsActive = true } };
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
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId, ConstructionTypeId = detail.ConstructionTypeId, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSquareMeter = 1200m, IsActive = true } };
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
            var rates = new List<RateEntity> { new() { TaxZoneId = 1, FloorId = detail.FloorId, ConstructionTypeId = detail.ConstructionTypeId, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSquareMeter = 1200m, IsActive = true } };
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

            Assert.Equal(0m, result.MonthlyRate);
            Assert.Equal(0m, result.YearlyRate);
            Assert.Equal(0m, result.YearlyRent);
            Assert.Equal(0m, result.Depreciation);
            Assert.Equal(0m, result.DepreciationPer);
            Assert.Equal(0m, result.AnnualRentalValue);
            Assert.Equal(0m, result.Maintenance);
            Assert.Equal(0m, result.RateableValue);
            Assert.Equal(0m, result.RAreaSqMtr);
        }
    }
}
