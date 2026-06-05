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
        [Fact]
        public void CalculateBaseValues_WhenDetailIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            PropertyDetailsEntity? detail = null;
            int financeYear = 2024;
            int taxZoneId = 1;
            int wardId = 1;
            var typeOfUses = new List<TypeOfUseEntity>();
            var rates = new List<RateEntity>();
            var depreciations = new List<DepreciationMasterEntity>();
            var yearRanges = new List<AssessmentYearRangeEntity>();
            var renters = new List<RenterMastEntity>();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                RateableValueCalculator.CalculateBaseValues(
                    detail!, financeYear, taxZoneId, wardId, typeOfUses, rates, depreciations, yearRanges, renters));

            Assert.Equal("detail", exception.ParamName);
        }

        [Fact]
        public void CalculateBaseValues_WhenIsTaxableIsFalse_ReturnsZeroedResult()
        {
            // Arrange
            var detail = new PropertyDetailsEntity
            {
                Id = 100,
                PropertyId = 1,
                IsTaxable = false, // Explicitly set to false
                TypeOfUseId = 1,
                ConstructionTypeId = 1,
                FloorId = 1,
                ConstructionYear = "2020"
            };

            int financeYear = 2024;
            int taxZoneId = 1;
            int wardId = 1;
            var typeOfUses = new List<TypeOfUseEntity>();
            var rates = new List<RateEntity>();
            var depreciations = new List<DepreciationMasterEntity>();
            var yearRanges = new List<AssessmentYearRangeEntity>();
            var renters = new List<RenterMastEntity>();

            // Act
            var result = RateableValueCalculator.CalculateBaseValues(
                detail, financeYear, taxZoneId, wardId, typeOfUses, rates, depreciations, yearRanges, renters);

            // Assert
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
            // Arrange
            var detail = new PropertyDetailsEntity
            {
                Id = 100,
                PropertyId = 1,
                IsTaxable = null, // Null should be treated as taxable (default behavior)
                TypeOfUseId = 1,
                ConstructionTypeId = 1,
                FloorId = 1,
                CarpetAreaSqMeter = 100,
                ConstructionYear = "2020"
            };

            int financeYear = 2024;
            int taxZoneId = 1;
            int wardId = 1;
            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, Type = "R", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = taxZoneId, FloorId = detail.FloorId, ConstructionTypeId = detail.ConstructionTypeId, YearRangeRVId = 1, RateSquareMeter = 1000m, IsActive = true } };
            var depreciations = new List<DepreciationMasterEntity>();
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new AssessmentYearRangeEntity
                {
                    Id = 1,
                    FromYear = 2000,
                    ToYear = 2100,
                    IsActive = true
                }
            };
            var renters = new List<RenterMastEntity>();

            // Act
            var result = RateableValueCalculator.CalculateBaseValues(
                detail, financeYear, taxZoneId, wardId, typeOfUses, rates, depreciations, yearRanges, renters);

            // Assert - Should perform calculations, not return zeros
            Assert.NotNull(result);
            Assert.Equal(1, result.PropertyId);
            Assert.Equal(100, result.PropertyDetailsId);

            Assert.NotEqual("Not Taxable", result.AppliedOn);
        }

        [Fact]
        public void CalculateBaseValues_WhenIsTaxableIsTrue_PerformsCalculation()
        {
            // Arrange
            var detail = new PropertyDetailsEntity
            {
                Id = 100,
                PropertyId = 1,
                IsTaxable = true, // Explicitly true
                TypeOfUseId = 1,
                ConstructionTypeId = 1,
                FloorId = 1,
                CarpetAreaSqMeter = 10d,
                ConstructionYear = "2020"
            };

            int financeYear = 2024;
            int taxZoneId = 1;
            int wardId = 1;
            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = taxZoneId, FloorId = detail.FloorId, ConstructionTypeId = detail.ConstructionTypeId, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSquareMeter = 1200m, IsActive = true } };
            var depreciations = new List<DepreciationMasterEntity>();
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new AssessmentYearRangeEntity
                {
                    Id = 1,
                    FromYear = 2000,
                    ToYear = 2100,
                    IsActive = true
                }
            };
            var renters = new List<RenterMastEntity>();

            // Act
            var result = RateableValueCalculator.CalculateBaseValues(
                detail, financeYear, taxZoneId, wardId, typeOfUses, rates, depreciations, yearRanges, renters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PropertyId);
            Assert.Equal(100, result.PropertyDetailsId);
        }

        [Theory]
        [InlineData(false, "Not Taxable")]
        [InlineData(true, null)] // When taxable, AppliedOn will be set to something other than "Not Taxable"
        public void CalculateBaseValues_AppliedOnProperty_SetCorrectlyBasedOnIsTaxable(bool isTaxable, string? expectedAppliedOn)
        {
            // Arrange
            var detail = new PropertyDetailsEntity
            {
                Id = 100,
                PropertyId = 1,
                IsTaxable = isTaxable,
                TypeOfUseId = 1,
                ConstructionTypeId = 1,
                FloorId = 1,
                CarpetAreaSqMeter = 10d,
                ConstructionYear = "2020"
            };

            int financeYear = 2024;
            int taxZoneId = 1;
            int wardId = 1;
            // Provide minimal master data required for taxable path
            var typeOfUses = new List<TypeOfUseEntity> { new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true } };
            var rates = new List<RateEntity> { new() { TaxZoneId = taxZoneId, FloorId = detail.FloorId, ConstructionTypeId = detail.ConstructionTypeId, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSquareMeter = 1200m, IsActive = true } };
            var depreciations = new List<DepreciationMasterEntity>();
            var yearRanges = new List<AssessmentYearRangeEntity>
            {
                new AssessmentYearRangeEntity
                {
                    Id = 1,
                    FromYear = 2000,
                    ToYear = 2100,
                    IsActive = true
                }
            };
            var renters = new List<RenterMastEntity>();

            // Act
            var result = RateableValueCalculator.CalculateBaseValues(
                detail, financeYear, taxZoneId, wardId, typeOfUses, rates, depreciations, yearRanges, renters);

            // Assert
            Assert.NotNull(result);
            if (expectedAppliedOn != null)
            {
                Assert.Equal(expectedAppliedOn, result.AppliedOn);
            }
            else
            {
                // When taxable, AppliedOn should be set to something (e.g., "Rent", "Area")
                // and should NOT be "Not Taxable"
                Assert.NotEqual("Not Taxable", result.AppliedOn ?? string.Empty);
            }
        }

        [Fact]
        public void CalculateBaseValues_WhenIsTaxableIsFalse_AllNumericFieldsAreZero()
        {
            // Arrange
            var detail = new PropertyDetailsEntity
            {
                Id = 100,
                PropertyId = 1,
                IsTaxable = false,
                TypeOfUseId = 1,
                ConstructionTypeId = 1,
                FloorId = 1,
                ConstructionYear = "2020"
            };

            int financeYear = 2024;
            int taxZoneId = 1;
            int wardId = 1;
            var typeOfUses = new List<TypeOfUseEntity>();
            var rates = new List<RateEntity>();
            var depreciations = new List<DepreciationMasterEntity>();
            var yearRanges = new List<AssessmentYearRangeEntity> { new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true } };
            var renters = new List<RenterMastEntity>();


            // Act
            var result = RateableValueCalculator.CalculateBaseValues(
                detail, financeYear, taxZoneId, wardId, typeOfUses, rates, depreciations, yearRanges, renters);

            // Assert - Verify all numeric fields are zeroed out
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
    }
}
