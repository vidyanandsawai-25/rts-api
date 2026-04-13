using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for Property Old Details API and Related Entities/DTOs
/// Coverage: Repository, Service, DTOs, Entities (PropertyMastOld, PropertyDetailsOld, 
/// CreatePropertyDto, UpdatePropertyDto, PropertyDto, PlotDetailsEntity, PropertyAssessmentEntity,
/// PropertyDetailsEntity, SocietyDetailsEntity)
/// Follows the same pattern as PropertyBasicDetailsTests and PropertyKycDetailsTests
/// </summary>
public class PropertyOldDetailsTests
{
    #region PropertyMastOldEntity Tests

    public class PropertyMastOldEntityTests
    {
        [Fact]
        public void PropertyMastOldEntity_AllProperties_GetSet_WorksCorrectly()
        {
            var now = DateTime.Now;
            var entity = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPartitionNo = "1",
                OldEgovNo = "EG001",
                OldPropertyTypeId = 2,
                OldALV = 50000.50,
                OldRV = 75000.75,
                OldGeneralTax = 10000.00,
                OldTotalTax = 12000.00,
                OldZoneNo = "Z5",
                OldPlotNo = "P123",
                OldCSN = "CSN456",
                OldPlotArea = 1500.25,
                OldAssessmentYear = 2020,
                OldFloor = "G",
                OldConstructionTypeOfUseId = "CT001",
                OldUseType = "Residential",
                OldConstArea = 1200.00,
                OldOwnerName = "John Doe",
                OldOccupierName = "Jane Doe",
                OldAddress = "123 Main St",
                OldOwnerNameEnglish = "John Doe Eng",
                OldOccupierNameEnglish = "Jane Doe Eng",
                OldAddressEnglish = "123 Main Street",
                NoOfOldToilets = 2,
                OldTotalRooms = 5,
                OldSocietyName = "ABC Society",
                OldEmailId = "old@example.com",
                OldParkingAreaSqFt = 200.0,
                OldParkingAreaSqMtr = 18.58,
                OldAssessmentDate = now,
                OldFlatOrShopNumber = "101",
                OldWing = "A",
                OldMobileNo = "9921759522",
                MarkedForDeletion = false,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = now,
                UpdatedBy = 2,
                UpdatedDate = now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(549357, entity.PropertyId);
            Assert.Equal("W79", entity.OldWardNo);
            Assert.Equal("22", entity.OldPropertyNo);
            Assert.Equal("1", entity.OldPartitionNo);
            Assert.Equal("EG001", entity.OldEgovNo);
            Assert.Equal(2, entity.OldPropertyTypeId);
            Assert.Equal(50000.50, entity.OldALV);
            Assert.Equal(75000.75, entity.OldRV);
            Assert.Equal(10000.00, entity.OldGeneralTax);
            Assert.Equal(12000.00, entity.OldTotalTax);
            Assert.Equal("Z5", entity.OldZoneNo);
            Assert.Equal("P123", entity.OldPlotNo);
            Assert.Equal("CSN456", entity.OldCSN);
            Assert.Equal(1500.25, entity.OldPlotArea);
            Assert.Equal(2020, entity.OldAssessmentYear);
            Assert.Equal("G", entity.OldFloor);
            Assert.Equal("CT001", entity.OldConstructionTypeOfUseId);
            Assert.Equal("Residential", entity.OldUseType);
            Assert.Equal(1200.00, entity.OldConstArea);
            Assert.Equal("John Doe", entity.OldOwnerName);
            Assert.Equal("Jane Doe", entity.OldOccupierName);
            Assert.Equal("123 Main St", entity.OldAddress);
            Assert.Equal("John Doe Eng", entity.OldOwnerNameEnglish);
            Assert.Equal("Jane Doe Eng", entity.OldOccupierNameEnglish);
            Assert.Equal("123 Main Street", entity.OldAddressEnglish);
            Assert.Equal(2, entity.NoOfOldToilets);
            Assert.Equal(5, entity.OldTotalRooms);
            Assert.Equal("ABC Society", entity.OldSocietyName);
            Assert.Equal("old@example.com", entity.OldEmailId);
            Assert.Equal(200.0, entity.OldParkingAreaSqFt);
            Assert.Equal(18.58, entity.OldParkingAreaSqMtr);
            Assert.Equal(now, entity.OldAssessmentDate);
            Assert.Equal("101", entity.OldFlatOrShopNumber);
            Assert.Equal("A", entity.OldWing);
            Assert.Equal("9921759522", entity.OldMobileNo);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
            Assert.Equal(1, entity.CreatedBy);
            Assert.Equal(2, entity.UpdatedBy);
        }

        [Fact]
        public void PropertyMastOldEntity_OptionalFields_CanBeNull()
        {
            var entity = new PropertyMastOldEntity
            {
                Id = 1,
                IsActive = true,
                MarkedForDeletion = false
            };

            Assert.Null(entity.PropertyId);
            Assert.Null(entity.OldWardNo);
            Assert.Null(entity.OldPropertyNo);
            Assert.Null(entity.OldPartitionNo);
            Assert.Null(entity.OldEgovNo);
            Assert.Null(entity.OldPropertyTypeId);
            Assert.Null(entity.OldALV);
            Assert.Null(entity.OldRV);
            Assert.Null(entity.OldGeneralTax);
            Assert.Null(entity.OldTotalTax);
            Assert.Null(entity.OldZoneNo);
            Assert.Null(entity.OldPlotNo);
            Assert.Null(entity.OldCSN);
            Assert.Null(entity.OldPlotArea);
            Assert.Null(entity.OldAssessmentYear);
            Assert.Null(entity.OldAssessmentDate);
            Assert.Null(entity.OldFloor);
            Assert.Null(entity.OldConstArea);
            Assert.Null(entity.OldOwnerName);
            Assert.Null(entity.OldOccupierName);
            Assert.Null(entity.OldAddress);
        }

        [Fact]
        public void PropertyMastOldEntity_InheritsFromBaseEntity()
        {
            var entity = new PropertyMastOldEntity();
            Assert.IsAssignableFrom<BaseEntity>(entity);
        }

        [Fact]
        public void PropertyMastOldEntity_DefaultValues_SetCorrectly()
        {
            var entity = new PropertyMastOldEntity();

            Assert.Equal(0, entity.Id);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }
    }

    #endregion

    #region PropertyDetailsOldEntity Tests

    public class PropertyDetailsOldEntityTests
    {
        [Fact]
        public void PropertyDetailsOldEntity_AllProperties_GetSet_WorksCorrectly()
        {
            var now = DateTime.Now;
            var entity = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldFloorId = "F1",
                OldConstructionYear = "2015",
                OldConstructionTypeId = "CT001",
                OldTypeOfUseId = "TU001",
                OldCarpetAreaSqfeet = 1200.50,
                OldCarpetAreaSqMeter = 111.48,
                OldRegistration = true,
                MarkedForDeletion = false,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = now,
                UpdatedBy = 2,
                UpdatedDate = now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(549357, entity.PropertyId);
            Assert.Equal("F1", entity.OldFloorId);
            Assert.Equal("2015", entity.OldConstructionYear);
            Assert.Equal("CT001", entity.OldConstructionTypeId);
            Assert.Equal("TU001", entity.OldTypeOfUseId);
            Assert.Equal(1200.50, entity.OldCarpetAreaSqfeet);
            Assert.Equal(111.48, entity.OldCarpetAreaSqMeter);
            Assert.True(entity.OldRegistration);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void PropertyDetailsOldEntity_OptionalFields_CanBeNull()
        {
            var entity = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                IsActive = true,
                MarkedForDeletion = false
            };

            Assert.Null(entity.OldFloorId);
            Assert.Null(entity.OldConstructionYear);
            Assert.Null(entity.OldConstructionTypeId);
            Assert.Null(entity.OldTypeOfUseId);
            Assert.Null(entity.OldCarpetAreaSqfeet);
            Assert.Null(entity.OldCarpetAreaSqMeter);
            Assert.Null(entity.OldRegistration);
        }

        [Fact]
        public void PropertyDetailsOldEntity_InheritsFromBaseEntity()
        {
            var entity = new PropertyDetailsOldEntity();
            Assert.IsAssignableFrom<BaseEntity>(entity);
        }

        [Fact]
        public void PropertyDetailsOldEntity_DefaultValues_SetCorrectly()
        {
            var entity = new PropertyDetailsOldEntity();

            Assert.Equal(0, entity.Id);
            Assert.Equal(0, entity.PropertyId);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }
    }

    #endregion

    #region PropertyOldDetailsDto Tests

    public class PropertyOldDetailsDtoTests
    {
        [Fact]
        public void PropertyOldDetailsDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new PropertyOldDetailsDto
            {
                PropertyId = 549357,
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPartitionNo = "1",
                OldEgovNo = "EG001",
                OldPlotArea = 1500.25,
                OldPlotNo = "P123",
                OldRV = 75000.75,
                OldALV = 50000.50,
                OldTotalTax = 12000.00,
                OldZoneNo = "Z5",
                OldConstructionYear = "2015",
                OldCarpetAreaSqFeet = 1200.50,
                OldCarpetAreaSqMeter = 111.48,
                OldRegistration = true,
                OldConstructionTypeId = "CT001",
                OldTypeOfUseId = "TU001"
            };

            Assert.Equal(549357, dto.PropertyId);
            Assert.Equal("W79", dto.OldWardNo);
            Assert.Equal("22", dto.OldPropertyNo);
            Assert.Equal("1", dto.OldPartitionNo);
            Assert.Equal("EG001", dto.OldEgovNo);
            Assert.Equal(1500.25, dto.OldPlotArea);
            Assert.Equal("P123", dto.OldPlotNo);
            Assert.Equal(75000.75, dto.OldRV);
            Assert.Equal(50000.50, dto.OldALV);
            Assert.Equal(12000.00, dto.OldTotalTax);
            Assert.Equal("Z5", dto.OldZoneNo);
            Assert.Equal("2015", dto.OldConstructionYear);
            Assert.Equal(1200.50, dto.OldCarpetAreaSqFeet);
            Assert.Equal(111.48, dto.OldCarpetAreaSqMeter);
            Assert.True(dto.OldRegistration);
            Assert.Equal("CT001", dto.OldConstructionTypeId);
            Assert.Equal("TU001", dto.OldTypeOfUseId);
        }

        [Fact]
        public void PropertyOldDetailsDto_OptionalProperties_CanBeNull()
        {
            var dto = new PropertyOldDetailsDto
            {
                PropertyId = 549357
            };

            Assert.Null(dto.OldWardNo);
            Assert.Null(dto.OldPropertyNo);
            Assert.Null(dto.OldPartitionNo);
            Assert.Null(dto.OldEgovNo);
            Assert.Null(dto.OldPlotArea);
            Assert.Null(dto.OldPlotNo);
            Assert.Null(dto.OldRV);
            Assert.Null(dto.OldALV);
            Assert.Null(dto.OldTotalTax);
            Assert.Null(dto.OldZoneNo);
            Assert.Null(dto.OldConstructionYear);
            Assert.Null(dto.OldCarpetAreaSqFeet);
            Assert.Null(dto.OldCarpetAreaSqMeter);
            Assert.Null(dto.OldRegistration);
            Assert.Null(dto.OldConstructionTypeId);
            Assert.Null(dto.OldTypeOfUseId);
        }

        [Fact]
        public void PropertyOldDetailsDto_RequiredPropertyId_HasValue()
        {
            var dto = new PropertyOldDetailsDto
            {
                PropertyId = 549357
            };

            Assert.Equal(549357, dto.PropertyId);
            Assert.NotEqual(0, dto.PropertyId);
        }

        [Fact]
        public void PropertyOldDetailsDto_DefaultConstructor_InitializesCorrectly()
        {
            var dto = new PropertyOldDetailsDto();

            Assert.Equal(0, dto.PropertyId);
        }
    }

    #endregion

    #region UpdatePropertyOldDetailsDto Tests

    public class UpdatePropertyOldDetailsDtoTests
    {
        private static IList<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPartitionNo = "1",
                OldEgovNo = "EG001",
                OldPlotArea = 1500.25,
                OldPlotNo = "P123",
                OldRV = 75000.75,
                OldALV = 50000.50,
                OldTotalTax = 12000.00,
                OldZoneNo = "Z5",
                OldConstructionYear = "2015",
                OldCarpetAreaSqFeet = 1200.50,
                OldCarpetAreaSqMeter = 111.48,
                OldRegistration = true,
                OldConstructionTypeId = "CT001",
                OldTypeOfUseId = "TU001"
            };

            Assert.Equal("W79", dto.OldWardNo);
            Assert.Equal("22", dto.OldPropertyNo);
            Assert.Equal("1", dto.OldPartitionNo);
            Assert.Equal("EG001", dto.OldEgovNo);
            Assert.Equal(1500.25, dto.OldPlotArea);
            Assert.Equal("P123", dto.OldPlotNo);
            Assert.Equal(75000.75, dto.OldRV);
            Assert.Equal(50000.50, dto.OldALV);
            Assert.Equal(12000.00, dto.OldTotalTax);
            Assert.Equal("Z5", dto.OldZoneNo);
            Assert.Equal("2015", dto.OldConstructionYear);
            Assert.Equal(1200.50, dto.OldCarpetAreaSqFeet);
            Assert.Equal(111.48, dto.OldCarpetAreaSqMeter);
            Assert.True(dto.OldRegistration);
            Assert.Equal("CT001", dto.OldConstructionTypeId);
            Assert.Equal("TU001", dto.OldTypeOfUseId);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_AllOptional_PassesValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto();

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ValidData_PassesValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldConstructionYear = "2015",
                OldPlotArea = 1500.25
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ExceedMaxLengthOldWardNo_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = new string('A', 11) // 11 characters, max is 10
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldWardNo") && r.ErrorMessage.Contains("10"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ExceedMaxLengthOldPropertyNo_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldPropertyNo = new string('B', 11) // 11 characters, max is 10
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldPropertyNo") && r.ErrorMessage.Contains("10"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_InvalidConstructionYear_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionYear = "20155" // 5 characters, must be 4
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldConstructionYear") && r.ErrorMessage.Contains("4-digit"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_NegativeOldPlotArea_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldPlotArea = -100.0 // Negative value
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldPlotArea") && r.ErrorMessage.Contains("negative"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_NegativeOldRV_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldRV = -500.0
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldRV") && r.ErrorMessage.Contains("negative"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_NegativeOldALV_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldALV = -300.0
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldALV") && r.ErrorMessage.Contains("negative"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_NegativeOldTotalTax_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldTotalTax = -1000.0
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldTotalTax") && r.ErrorMessage.Contains("negative"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_NegativeOldCarpetAreaSqFeet_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldCarpetAreaSqFeet = -200.0
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldCarpetAreaSqFeet") && r.ErrorMessage.Contains("negative"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_NegativeOldCarpetAreaSqMeter_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldCarpetAreaSqMeter = -150.0
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldCarpetAreaSqMeter") && r.ErrorMessage.Contains("negative"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_BooleanOldRegistration_AcceptsTrueAndFalse()
        {
            var dto1 = new UpdatePropertyOldDetailsDto { OldRegistration = true };
            var dto2 = new UpdatePropertyOldDetailsDto { OldRegistration = false };
            var dto3 = new UpdatePropertyOldDetailsDto { OldRegistration = null };

            Assert.True(dto1.OldRegistration);
            Assert.False(dto2.OldRegistration);
            Assert.Null(dto3.OldRegistration);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_AllNullValues_PassesValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = null,
                OldPropertyNo = null,
                OldPlotArea = null,
                OldRV = null,
                OldRegistration = null
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ZeroValues_PassValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldPlotArea = 0.0,
                OldRV = 0.0,
                OldALV = 0.0,
                OldTotalTax = 0.0,
                OldCarpetAreaSqFeet = 0.0,
                OldCarpetAreaSqMeter = 0.0
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ExceedMaxLengthOldPartitionNo_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldPartitionNo = new string('C', 11) // 11 characters, max is 10
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldPartitionNo") && r.ErrorMessage.Contains("10"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ExceedMaxLengthOldEgovNo_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldEgovNo = new string('D', 11) // 11 characters, max is 10
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldEgovNo") && r.ErrorMessage.Contains("10"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ExceedMaxLengthOldPlotNo_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldPlotNo = new string('E', 21) // 21 characters, max is 20
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldPlotNo") && r.ErrorMessage.Contains("20"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ExceedMaxLengthOldZoneNo_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldZoneNo = new string('F', 21) // 21 characters, max is 20
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldZoneNo") && r.ErrorMessage.Contains("20"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ExceedMaxLengthOldConstructionTypeId_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionTypeId = new string('G', 8) // 8 characters, max is 7
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldConstructionTypeId") && r.ErrorMessage.Contains("7"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ExceedMaxLengthOldTypeOfUseId_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldTypeOfUseId = new string('H', 21) // 21 characters, max is 20
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldTypeOfUseId") && r.ErrorMessage.Contains("20"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ValidConstructionYear_PassesValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionYear = "2023"
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ThreeDigitConstructionYear_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionYear = "202" // Only 3 digits
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("OldConstructionYear") && r.ErrorMessage.Contains("4-digit"));
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_PositiveValues_PassValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldPlotArea = 1500.25,
                OldRV = 75000.75,
                OldALV = 50000.50,
                OldTotalTax = 12000.00,
                OldCarpetAreaSqFeet = 1200.50,
                OldCarpetAreaSqMeter = 111.48
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_MaxLengthExactValues_PassValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = new string('A', 10), // Exactly 10 characters
                OldPropertyNo = new string('B', 10), // Exactly 10 characters
                OldPartitionNo = new string('C', 10), // Exactly 10 characters
                OldEgovNo = new string('D', 10), // Exactly 10 characters
                OldPlotNo = new string('E', 20), // Exactly 20 characters
                OldZoneNo = new string('F', 20), // Exactly 20 characters
                OldConstructionTypeId = new string('G', 7), // Exactly 7 characters
                OldTypeOfUseId = new string('H', 20) // Exactly 20 characters
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }
    }

    #endregion

    #region PropertyRepository OldDetails Tests

    public class PropertyRepositoryOldDetailsTests
    {
        [Fact]
        public async Task GetOldDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var repository = new PropertyRepository(context);

            var result = await repository.GetOldDetailsAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetOldDetailsAsync_PropertyExistsButNoOldData_ReturnsEmptyDto()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Null(result.OldWardNo);
            Assert.Null(result.OldPropertyNo);
            Assert.Null(result.OldConstructionYear);
        }

        [Fact]
        public async Task GetOldDetailsAsync_WithPropertyMastOldData_ReturnsDto()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var oldMast = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPartitionNo = "1",
                OldEgovNo = "EG001",
                OldPlotArea = 1500.25,
                OldPlotNo = "P123",
                OldRV = 75000.75,
                OldALV = 50000.50,
                OldTotalTax = 12000.00,
                OldZoneNo = "Z5",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastOld.Add(oldMast);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal("W79", result.OldWardNo);
            Assert.Equal("22", result.OldPropertyNo);
            Assert.Equal("1", result.OldPartitionNo);
            Assert.Equal("EG001", result.OldEgovNo);
            Assert.Equal(1500.25, result.OldPlotArea);
            Assert.Equal("P123", result.OldPlotNo);
            Assert.Equal(75000.75, result.OldRV);
            Assert.Equal(50000.50, result.OldALV);
            Assert.Equal(12000.00, result.OldTotalTax);
            Assert.Equal("Z5", result.OldZoneNo);
        }

        [Fact]
        public async Task GetOldDetailsAsync_WithPropertyDetailsOldData_ReturnsDto()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var oldDetails = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "2015",
                OldCarpetAreaSqfeet = 1200.50,
                OldCarpetAreaSqMeter = 111.48,
                OldRegistration = true,
                OldConstructionTypeId = "CT001",
                OldTypeOfUseId = "TU001",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyDetailsOld.Add(oldDetails);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal("2015", result.OldConstructionYear);
            Assert.Equal(1200.50, result.OldCarpetAreaSqFeet);
            Assert.Equal(111.48, result.OldCarpetAreaSqMeter);
            Assert.True(result.OldRegistration);
            Assert.Equal("CT001", result.OldConstructionTypeId);
            Assert.Equal("TU001", result.OldTypeOfUseId);
        }

        [Fact]
        public async Task GetOldDetailsAsync_WithBothOldTables_ReturnsCompleteDto()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var oldMast = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPlotArea = 1500.25,
                OldRV = 75000.75,
                IsActive = true,
                MarkedForDeletion = false
            };

            var oldDetails = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "2015",
                OldCarpetAreaSqMeter = 111.48,
                OldRegistration = true,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastOld.Add(oldMast);
            context.PropertyDetailsOld.Add(oldDetails);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal("W79", result.OldWardNo);
            Assert.Equal("22", result.OldPropertyNo);
            Assert.Equal(1500.25, result.OldPlotArea);
            Assert.Equal(75000.75, result.OldRV);
            Assert.Equal("2015", result.OldConstructionYear);
            Assert.Equal(111.48, result.OldCarpetAreaSqMeter);
            Assert.True(result.OldRegistration);
        }

        [Fact]
        public async Task GetOldDetailsAsync_WithInactiveProperty_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = false, // Inactive
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldDetailsAsync(549357);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetOldDetailsAsync_WithMarkedForDeletionProperty_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = true // Marked for deletion
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldDetailsAsync(549357);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var repository = new PropertyRepository(context);

            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79",
                OldPropertyNo = "22"
            };

            var result = await repository.UpdateOldDetailsAsync(999, dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_NoOldDataExists_InsertsNewRecords()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPlotArea = 1500.25,
                OldRV = 75000.75,
                OldConstructionYear = "2015",
                OldCarpetAreaSqMeter = 111.48
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("W79", result.OldWardNo);
            Assert.Equal("22", result.OldPropertyNo);
            Assert.Equal(1500.25, result.OldPlotArea);
            Assert.Equal(75000.75, result.OldRV);
            Assert.Equal("2015", result.OldConstructionYear);
            Assert.Equal(111.48, result.OldCarpetAreaSqMeter);

            // Verify INSERT happened
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(1, oldMastCount);
            Assert.Equal(1, oldDetailsCount);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_OldDataExists_UpdatesRecords()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var oldMast = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "OLD",
                OldPropertyNo = "OLD",
                OldPlotArea = 1000.0,
                OldRV = 50000.0,
                IsActive = true,
                MarkedForDeletion = false
            };

            var oldDetails = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "OLD",
                OldCarpetAreaSqMeter = 100.0,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastOld.Add(oldMast);
            context.PropertyDetailsOld.Add(oldDetails);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "NEW",
                OldPropertyNo = "NEW",
                OldPlotArea = 2000.0,
                OldRV = 100000.0,
                OldConstructionYear = "2020",
                OldCarpetAreaSqMeter = 200.0
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("NEW", result.OldWardNo);
            Assert.Equal("NEW", result.OldPropertyNo);
            Assert.Equal(2000.0, result.OldPlotArea);
            Assert.Equal(100000.0, result.OldRV);
            Assert.Equal("2020", result.OldConstructionYear);
            Assert.Equal(200.0, result.OldCarpetAreaSqMeter);

            // Verify UPDATE happened (still 1 record each)
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(1, oldMastCount);
            Assert.Equal(1, oldDetailsCount);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_PartialUpdate_UpdatesOnlyProvidedFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var oldMast = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPlotArea = 1000.0,
                OldRV = 50000.0,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastOld.Add(oldMast);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldPlotArea = 2000.0 // Only updating plot area
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("W79", result.OldWardNo); // Unchanged
            Assert.Equal("22", result.OldPropertyNo); // Unchanged
            Assert.Equal(2000.0, result.OldPlotArea); // Updated
            Assert.Equal(50000.0, result.OldRV); // Unchanged
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_EmptyDto_DoesNotInsertRecords()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto(); // Empty DTO

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);

            // Repository does NOT insert records when DTO has no data (consistent with BasicDetails/KycDetails pattern)
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(0, oldMastCount);
            Assert.Equal(0, oldDetailsCount);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_OnlyMastData_InsertsOnlyMastRecord()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPlotArea = 1500.25
                // No PropertyDetailsOld data
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("W79", result.OldWardNo);
            Assert.Equal("22", result.OldPropertyNo);
            Assert.Equal(1500.25, result.OldPlotArea);

            // Repository only inserts records when data is provided (consistent with BasicDetails/KycDetails pattern)
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(1, oldMastCount);
            Assert.Equal(0, oldDetailsCount); // No details data provided, so no record inserted
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_OnlyDetailsData_InsertsOnlyDetailsRecord()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionYear = "2015",
                OldCarpetAreaSqMeter = 111.48,
                OldRegistration = true
                // No PropertyMastOld data
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("2015", result.OldConstructionYear);
            Assert.Equal(111.48, result.OldCarpetAreaSqMeter);
            Assert.True(result.OldRegistration);

            // Repository only inserts records when data is provided (consistent with BasicDetails/KycDetails pattern)
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(0, oldMastCount); // No mast data provided, so no record inserted
            Assert.Equal(1, oldDetailsCount);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_UpdatesAllMastFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPartitionNo = "1",
                OldEgovNo = "EG001",
                OldPlotArea = 1500.25,
                OldPlotNo = "P123",
                OldRV = 75000.75,
                OldALV = 50000.50,
                OldTotalTax = 12000.00,
                OldZoneNo = "Z5"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("W79", result.OldWardNo);
            Assert.Equal("22", result.OldPropertyNo);
            Assert.Equal("1", result.OldPartitionNo);
            Assert.Equal("EG001", result.OldEgovNo);
            Assert.Equal(1500.25, result.OldPlotArea);
            Assert.Equal("P123", result.OldPlotNo);
            Assert.Equal(75000.75, result.OldRV);
            Assert.Equal(50000.50, result.OldALV);
            Assert.Equal(12000.00, result.OldTotalTax);
            Assert.Equal("Z5", result.OldZoneNo);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_UpdatesAllDetailsFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionYear = "2015",
                OldCarpetAreaSqFeet = 1200.50,
                OldCarpetAreaSqMeter = 111.48,
                OldRegistration = true,
                OldConstructionTypeId = "CT001",
                OldTypeOfUseId = "TU001"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("2015", result.OldConstructionYear);
            Assert.Equal(1200.50, result.OldCarpetAreaSqFeet);
            Assert.Equal(111.48, result.OldCarpetAreaSqMeter);
            Assert.True(result.OldRegistration);
            Assert.Equal("CT001", result.OldConstructionTypeId);
            Assert.Equal("TU001", result.OldTypeOfUseId);
        }
    }

    #endregion

    #region PropertyService OldDetails Tests

    public class PropertyServiceOldDetailsTests
    {
        [Fact]
        public async Task GetOldDetailsAsync_CallsRepository()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var expectedDto = new PropertyOldDetailsDto
            {
                PropertyId = 549357,
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPlotArea = 1500.25
            };

            mockPropertyRepo
                .Setup(r => r.GetOldDetailsAsync(549357, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDto);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.GetOldDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal("W79", result.OldWardNo);
            Assert.Equal("22", result.OldPropertyNo);
            Assert.Equal(1500.25, result.OldPlotArea);
            mockPropertyRepo.Verify(r => r.GetOldDetailsAsync(549357, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetOldDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            mockPropertyRepo
                .Setup(r => r.GetOldDetailsAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PropertyOldDetailsDto?)null);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.GetOldDetailsAsync(999);

            Assert.Null(result);
            mockPropertyRepo.Verify(r => r.GetOldDetailsAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_CallsRepositoryAndReturnsResult()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPlotArea = 1500.25
            };

            var expectedResult = new PropertyOldDetailsDto
            {
                PropertyId = 549357,
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPlotArea = 1500.25
            };

            mockPropertyRepo
                .Setup(r => r.UpdateOldDetailsAsync(549357, dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal("W79", result.OldWardNo);
            Assert.Equal(1500.25, result.OldPlotArea);
            mockPropertyRepo.Verify(r => r.UpdateOldDetailsAsync(549357, dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79",
                OldPropertyNo = "22"
            };

            mockPropertyRepo
                .Setup(r => r.UpdateOldDetailsAsync(999, dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PropertyOldDetailsDto?)null);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.UpdateOldDetailsAsync(999, dto);

            Assert.Null(result);
            mockPropertyRepo.Verify(r => r.UpdateOldDetailsAsync(999, dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var dto = new UpdatePropertyOldDetailsDto { OldWardNo = "W79" };
            var expectedResult = new PropertyOldDetailsDto { PropertyId = 549357 };
            var cts = new CancellationTokenSource();

            mockPropertyRepo
                .Setup(r => r.UpdateOldDetailsAsync(549357, dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.UpdateOldDetailsAsync(549357, dto, cts.Token);

            Assert.NotNull(result);
            mockPropertyRepo.Verify(r => r.UpdateOldDetailsAsync(549357, dto, cts.Token), Times.Once);
        }

        [Fact]
        public async Task GetOldDetailsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var expectedDto = new PropertyOldDetailsDto { PropertyId = 549357 };
            var cts = new CancellationTokenSource();

            mockPropertyRepo
                .Setup(r => r.GetOldDetailsAsync(549357, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDto);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.GetOldDetailsAsync(549357, cts.Token);

            Assert.NotNull(result);
            mockPropertyRepo.Verify(r => r.GetOldDetailsAsync(549357, cts.Token), Times.Once);
        }

        [Fact]
        public async Task GetOldDetailsAsync_ExistingProperty_ReturnsCompleteDto()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var expectedDto = new PropertyOldDetailsDto
            {
                PropertyId = 549357,
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPartitionNo = "1",
                OldEgovNo = "EG001",
                OldPlotArea = 1500.25,
                OldPlotNo = "P123",
                OldRV = 75000.75,
                OldALV = 50000.50,
                OldTotalTax = 12000.00,
                OldZoneNo = "Z5",
                OldConstructionYear = "2015",
                OldCarpetAreaSqFeet = 1200.50,
                OldCarpetAreaSqMeter = 111.48,
                OldRegistration = true,
                OldConstructionTypeId = "CT001",
                OldTypeOfUseId = "TU001"
            };

            mockPropertyRepo
                .Setup(r => r.GetOldDetailsAsync(549357, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDto);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.GetOldDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal("W79", result.OldWardNo);
            Assert.Equal("22", result.OldPropertyNo);
            Assert.Equal("1", result.OldPartitionNo);
            Assert.Equal("EG001", result.OldEgovNo);
            Assert.Equal(1500.25, result.OldPlotArea);
            Assert.Equal("P123", result.OldPlotNo);
            Assert.Equal(75000.75, result.OldRV);
            Assert.Equal(50000.50, result.OldALV);
            Assert.Equal(12000.00, result.OldTotalTax);
            Assert.Equal("Z5", result.OldZoneNo);
            Assert.Equal("2015", result.OldConstructionYear);
            Assert.Equal(1200.50, result.OldCarpetAreaSqFeet);
            Assert.Equal(111.48, result.OldCarpetAreaSqMeter);
            Assert.True(result.OldRegistration);
            Assert.Equal("CT001", result.OldConstructionTypeId);
            Assert.Equal("TU001", result.OldTypeOfUseId);
        }
    }

    #endregion

    #region Edge Case Tests

    public class OldDetailsEdgeCaseTests
    {
        [Fact]
        public async Task UpdateOldDetailsAsync_MarkedForDeletionTrue_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = true // Marked for deletion
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.Null(result); // Should not find property marked for deletion
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_IsActiveFalse_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = false, // Inactive
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.Null(result); // Should not find inactive property
        }

        [Fact]
        public async Task GetOldDetailsAsync_OldMastInactive_NotIncluded()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldMast = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "W79",
                IsActive = false, // Inactive
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastOld.Add(oldMast);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Null(result.OldWardNo); // Inactive old data should not be included
        }

        [Fact]
        public async Task GetOldDetailsAsync_OldDetailsInactive_NotIncluded()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldDetails = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "2015",
                IsActive = false, // Inactive
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyDetailsOld.Add(oldDetails);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Null(result.OldConstructionYear); // Inactive old details should not be included
        }

        [Fact]
        public async Task GetOldDetailsAsync_OldMastMarkedForDeletion_NotIncluded()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldMast = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "W79",
                IsActive = true,
                MarkedForDeletion = true // Marked for deletion
            };

            context.PropertyMast.Add(property);
            context.PropertyMastOld.Add(oldMast);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Null(result.OldWardNo); // Marked for deletion should not be included
        }

        [Fact]
        public async Task GetOldDetailsAsync_OldDetailsMarkedForDeletion_NotIncluded()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldDetails = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "2015",
                IsActive = true,
                MarkedForDeletion = true // Marked for deletion
            };

            context.PropertyMast.Add(property);
            context.PropertyDetailsOld.Add(oldDetails);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Null(result.OldConstructionYear); // Marked for deletion should not be included
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_MultipleOldRecords_UpdatesFirstOne()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldMast1 = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "FIRST",
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldMast2 = new PropertyMastOldEntity
            {
                Id = 2,
                PropertyId = 549357,
                OldWardNo = "SECOND",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastOld.AddRange(oldMast1, oldMast2);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "UPDATED"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("UPDATED", result.OldWardNo);

            // Verify the first record was updated, second unchanged
            var firstRecord = await context.PropertyMastOld.FirstAsync(x => x.Id == 1);
            var secondRecord = await context.PropertyMastOld.FirstAsync(x => x.Id == 2);
            Assert.Equal("UPDATED", firstRecord.OldWardNo);
            Assert.Equal("SECOND", secondRecord.OldWardNo);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_MultipleOldDetailsRecords_UpdatesFirstOne()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldDetails1 = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "2010",
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldDetails2 = new PropertyDetailsOldEntity
            {
                Id = 2,
                PropertyId = 549357,
                OldConstructionYear = "2015",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyDetailsOld.AddRange(oldDetails1, oldDetails2);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionYear = "2023"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("2023", result.OldConstructionYear);

            // Verify the first record was updated, second unchanged
            var firstRecord = await context.PropertyDetailsOld.FirstAsync(x => x.Id == 1);
            var secondRecord = await context.PropertyDetailsOld.FirstAsync(x => x.Id == 2);
            Assert.Equal("2023", firstRecord.OldConstructionYear);
            Assert.Equal("2015", secondRecord.OldConstructionYear);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_ExistingMastRecord_UpdatesAllMastFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldMast = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "OLD_WARD",
                OldPropertyNo = "OLD_PROP",
                OldPartitionNo = "OLD_PART",
                OldEgovNo = "OLD_EGOV",
                OldPlotArea = 100.0,
                OldPlotNo = "OLD_PLOT",
                OldRV = 1000.0,
                OldALV = 500.0,
                OldTotalTax = 200.0,
                OldZoneNo = "OLD_ZONE",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastOld.Add(oldMast);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "NEW_WARD",
                OldPropertyNo = "NEW_PROP",
                OldPartitionNo = "NEW_PART",
                OldEgovNo = "NEW_EGOV",
                OldPlotArea = 2000.0,
                OldPlotNo = "NEW_PLOT",
                OldRV = 50000.0,
                OldALV = 30000.0,
                OldTotalTax = 5000.0,
                OldZoneNo = "NEW_ZONE"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("NEW_WARD", result.OldWardNo);
            Assert.Equal("NEW_PROP", result.OldPropertyNo);
            Assert.Equal("NEW_PART", result.OldPartitionNo);
            Assert.Equal("NEW_EGOV", result.OldEgovNo);
            Assert.Equal(2000.0, result.OldPlotArea);
            Assert.Equal("NEW_PLOT", result.OldPlotNo);
            Assert.Equal(50000.0, result.OldRV);
            Assert.Equal(30000.0, result.OldALV);
            Assert.Equal(5000.0, result.OldTotalTax);
            Assert.Equal("NEW_ZONE", result.OldZoneNo);

            // Verify still only 1 record (UPDATE, not INSERT)
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            Assert.Equal(1, oldMastCount);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_ExistingDetailsRecord_UpdatesAllDetailsFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldDetails = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "2010",
                OldCarpetAreaSqfeet = 500.0,
                OldCarpetAreaSqMeter = 46.45,
                OldRegistration = false,
                OldConstructionTypeId = "OLD_CT",
                OldTypeOfUseId = "OLD_TU",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyDetailsOld.Add(oldDetails);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionYear = "2023",
                OldCarpetAreaSqFeet = 1500.0,
                OldCarpetAreaSqMeter = 139.35,
                OldRegistration = true,
                OldConstructionTypeId = "NEW_CT",
                OldTypeOfUseId = "NEW_TU"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("2023", result.OldConstructionYear);
            Assert.Equal(1500.0, result.OldCarpetAreaSqFeet);
            Assert.Equal(139.35, result.OldCarpetAreaSqMeter);
            Assert.True(result.OldRegistration);
            Assert.Equal("NEW_CT", result.OldConstructionTypeId);
            Assert.Equal("NEW_TU", result.OldTypeOfUseId);

            // Verify still only 1 record (UPDATE, not INSERT)
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(1, oldDetailsCount);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_WithCancellationToken_PassesTokenCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79"
            };
            var cts = new CancellationTokenSource();

            var result = await repository.UpdateOldDetailsAsync(549357, dto, cts.Token);

            Assert.NotNull(result);
            Assert.Equal("W79", result.OldWardNo);
        }

        [Fact]
        public async Task GetOldDetailsAsync_WithCancellationToken_PassesTokenCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var cts = new CancellationTokenSource();

            var result = await repository.GetOldDetailsAsync(549357, cts.Token);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_BothExistingRecords_UpdatesBothTables()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldMast = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "OLD",
                OldRV = 1000.0,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldDetails = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "2010",
                OldRegistration = false,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastOld.Add(oldMast);
            context.PropertyDetailsOld.Add(oldDetails);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "NEW",
                OldRV = 5000.0,
                OldConstructionYear = "2023",
                OldRegistration = true
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("NEW", result.OldWardNo);
            Assert.Equal(5000.0, result.OldRV);
            Assert.Equal("2023", result.OldConstructionYear);
            Assert.True(result.OldRegistration);

            // Verify counts remain 1 (updates, not inserts)
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(1, oldMastCount);
            Assert.Equal(1, oldDetailsCount);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_PartialDetailsUpdate_OnlyUpdatesProvidedFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldDetails = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "2015",
                OldCarpetAreaSqfeet = 1000.0,
                OldCarpetAreaSqMeter = 92.9,
                OldRegistration = true,
                OldConstructionTypeId = "CT001",
                OldTypeOfUseId = "TU001",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyDetailsOld.Add(oldDetails);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldCarpetAreaSqFeet = 1500.0 // Only update this field
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("2015", result.OldConstructionYear); // Unchanged
            Assert.Equal(1500.0, result.OldCarpetAreaSqFeet); // Updated
            Assert.Equal(92.9, result.OldCarpetAreaSqMeter); // Unchanged
            Assert.True(result.OldRegistration); // Unchanged
            Assert.Equal("CT001", result.OldConstructionTypeId); // Unchanged
            Assert.Equal("TU001", result.OldTypeOfUseId); // Unchanged
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_InsertsOnlyMastWhenOnlyMastDataProvided_NoExistingRecords()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPartitionNo = "1",
                OldEgovNo = "EG001",
                OldPlotArea = 1500.25,
                OldPlotNo = "P123",
                OldRV = 75000.75,
                OldALV = 50000.50,
                OldTotalTax = 12000.00,
                OldZoneNo = "Z5"
                // No PropertyDetailsOld fields
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            
            // Verify PropertyMastOld was inserted
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            Assert.Equal(1, oldMastCount);

            // Verify PropertyDetailsOld was NOT inserted
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(0, oldDetailsCount);

            // Verify inserted values
            var insertedMast = await context.PropertyMastOld.FirstAsync();
            Assert.Equal(549357, insertedMast.PropertyId);
            Assert.Equal("W79", insertedMast.OldWardNo);
            Assert.Equal("22", insertedMast.OldPropertyNo);
            Assert.Equal("1", insertedMast.OldPartitionNo);
            Assert.Equal("EG001", insertedMast.OldEgovNo);
            Assert.Equal(1500.25, insertedMast.OldPlotArea);
            Assert.Equal("P123", insertedMast.OldPlotNo);
            Assert.Equal(75000.75, insertedMast.OldRV);
            Assert.Equal(50000.50, insertedMast.OldALV);
            Assert.Equal(12000.00, insertedMast.OldTotalTax);
            Assert.Equal("Z5", insertedMast.OldZoneNo);
            Assert.True(insertedMast.IsActive);
            Assert.False(insertedMast.MarkedForDeletion);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_InsertsOnlyDetailsWhenOnlyDetailsDataProvided_NoExistingRecords()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                // No PropertyMastOld fields
                OldConstructionYear = "2015",
                OldCarpetAreaSqFeet = 1200.50,
                OldCarpetAreaSqMeter = 111.48,
                OldRegistration = true,
                OldConstructionTypeId = "CT001",
                OldTypeOfUseId = "TU001"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);

            // Verify PropertyMastOld was NOT inserted
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            Assert.Equal(0, oldMastCount);

            // Verify PropertyDetailsOld was inserted
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(1, oldDetailsCount);

            // Verify inserted values
            var insertedDetails = await context.PropertyDetailsOld.FirstAsync();
            Assert.Equal(549357, insertedDetails.PropertyId);
            Assert.Equal("2015", insertedDetails.OldConstructionYear);
            Assert.Equal(1200.50, insertedDetails.OldCarpetAreaSqfeet);
            Assert.Equal(111.48, insertedDetails.OldCarpetAreaSqMeter);
            Assert.True(insertedDetails.OldRegistration);
            Assert.Equal("CT001", insertedDetails.OldConstructionTypeId);
            Assert.Equal("TU001", insertedDetails.OldTypeOfUseId);
            Assert.True(insertedDetails.IsActive);
            Assert.False(insertedDetails.MarkedForDeletion);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_InsertsBothWhenBothDataProvided_NoExistingRecords()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                // PropertyMastOld fields
                OldWardNo = "W79",
                OldPropertyNo = "22",
                OldPlotArea = 1500.25,
                OldRV = 75000.75,
                // PropertyDetailsOld fields
                OldConstructionYear = "2015",
                OldCarpetAreaSqMeter = 111.48,
                OldRegistration = true
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);

            // Verify both tables were populated
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(1, oldMastCount);
            Assert.Equal(1, oldDetailsCount);

            // Verify Mast values
            var insertedMast = await context.PropertyMastOld.FirstAsync();
            Assert.Equal("W79", insertedMast.OldWardNo);
            Assert.Equal("22", insertedMast.OldPropertyNo);
            Assert.Equal(1500.25, insertedMast.OldPlotArea);
            Assert.Equal(75000.75, insertedMast.OldRV);

            // Verify Details values
            var insertedDetails = await context.PropertyDetailsOld.FirstAsync();
            Assert.Equal("2015", insertedDetails.OldConstructionYear);
            Assert.Equal(111.48, insertedDetails.OldCarpetAreaSqMeter);
            Assert.True(insertedDetails.OldRegistration);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_UpdatesExistingMast_InsertsNewDetails()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldMast = new PropertyMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldWardNo = "OLD",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastOld.Add(oldMast);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "NEW",
                OldConstructionYear = "2023"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("NEW", result.OldWardNo);
            Assert.Equal("2023", result.OldConstructionYear);

            // Verify mast was updated (still 1 record)
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            Assert.Equal(1, oldMastCount);

            // Verify details was inserted (1 new record)
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(1, oldDetailsCount);
        }

        [Fact]
        public async Task UpdateOldDetailsAsync_InsertsNewMast_UpdatesExistingDetails()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var oldDetails = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "2010",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyDetailsOld.Add(oldDetails);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldWardNo = "W79",
                OldConstructionYear = "2023"
            };

            var result = await repository.UpdateOldDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("W79", result.OldWardNo);
            Assert.Equal("2023", result.OldConstructionYear);

            // Verify mast was inserted (1 new record)
            var oldMastCount = await context.PropertyMastOld.CountAsync();
            Assert.Equal(1, oldMastCount);

            // Verify details was updated (still 1 record)
            var oldDetailsCount = await context.PropertyDetailsOld.CountAsync();
            Assert.Equal(1, oldDetailsCount);
        }
    }

    #endregion

    #region Additional Validation Tests

    public class AdditionalValidationTests
    {
        [Fact]
        public void UpdatePropertyOldDetailsDto_AllFieldsEmpty_IsValid()
        {
            var dto = new UpdatePropertyOldDetailsDto();

            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(dto, serviceProvider: null, items: null);
            Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);

            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_ValidYear_PassesValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionYear = "1990"
            };

            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(dto, serviceProvider: null, items: null);
            Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);

            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_EmptyStringYear_IsAcceptedAsOptional()
        {
            // Empty string is acceptable since the field is optional and doesn't violate StringLength
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionYear = ""
            };

            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(dto, serviceProvider: null, items: null);
            Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);

            // Empty string passes validation because:
            // 1. StringLength allows 0-4 characters
            // 2. RegularExpression allows empty/null (it only validates non-empty values)
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_NonNumericYear_FailsValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldConstructionYear = "ABCD"
            };

            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(dto, serviceProvider: null, items: null);
            Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);

            Assert.NotEmpty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_VeryLargePositiveValues_PassValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldPlotArea = double.MaxValue / 2,
                OldRV = double.MaxValue / 2,
                OldALV = double.MaxValue / 2,
                OldTotalTax = double.MaxValue / 2,
                OldCarpetAreaSqFeet = double.MaxValue / 2,
                OldCarpetAreaSqMeter = double.MaxValue / 2
            };

            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(dto, serviceProvider: null, items: null);
            Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);

            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyOldDetailsDto_SmallDecimalValues_PassValidation()
        {
            var dto = new UpdatePropertyOldDetailsDto
            {
                OldPlotArea = 0.001,
                OldRV = 0.001,
                OldALV = 0.001,
                OldTotalTax = 0.001,
                OldCarpetAreaSqFeet = 0.001,
                OldCarpetAreaSqMeter = 0.001
            };

            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(dto, serviceProvider: null, items: null);
            Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);

            Assert.Empty(results);
        }

        [Fact]
        public void PropertyOldDetailsDto_DefaultValues()
        {
            var dto = new PropertyOldDetailsDto();

            Assert.Equal(0, dto.PropertyId);
            Assert.Null(dto.OldWardNo);
            Assert.Null(dto.OldPropertyNo);
            Assert.Null(dto.OldPartitionNo);
            Assert.Null(dto.OldEgovNo);
            Assert.Null(dto.OldPlotArea);
            Assert.Null(dto.OldPlotNo);
            Assert.Null(dto.OldRV);
            Assert.Null(dto.OldALV);
            Assert.Null(dto.OldTotalTax);
            Assert.Null(dto.OldZoneNo);
            Assert.Null(dto.OldConstructionYear);
            Assert.Null(dto.OldCarpetAreaSqFeet);
            Assert.Null(dto.OldCarpetAreaSqMeter);
            Assert.Null(dto.OldRegistration);
            Assert.Null(dto.OldConstructionTypeId);
            Assert.Null(dto.OldTypeOfUseId);
        }
    }

    #endregion

    #region CreatePropertyDto Tests

    public class CreatePropertyDtoTests
    {
        private static IList<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void CreatePropertyDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                PropertyNo = "22",
                PartitionNo = "1",
                PropertyTypeId = 2,
                UPICId = "UPIC-123",
                OpenPlot = true,
                CSN = "CSN456",
                SubZoneNo = "SZ01",
                PlotNo = "P123",
                CategoryId = 1,
                Type = "R",
                PartType = "MAIN",
                OwnerTitle = "Mr",
                OwnerName = "John Doe",
                OwnerTitleEnglish = "Mr",
                OwnerNameEnglish = "John Doe Eng",
                OccupierTitle = "Mrs",
                OccupierName = "Jane Doe",
                OccupierTitleEnglish = "Mrs",
                OccupierNameEnglish = "Jane Doe Eng",
                FlatOrShopNo = "101",
                FlatOrShopName = "Flat 101",
                FlatOrShopNoEnglish = "101E",
                FlatOrShopNameEnglish = "Flat 101 English",
                Address = "123 Main Street",
                Location = "City Center",
                AddressEnglish = "123 Main Street English",
                LocationEnglish = "City Center English",
                MobileNo = "9876543210",
                EmailId = "test@example.com",
                SocietyDetailId = 5,
                MarkedForDeletion = false
            };

            Assert.Equal(10, dto.TaxZoneId);
            Assert.Equal(79, dto.WardId);
            Assert.Equal("22", dto.PropertyNo);
            Assert.Equal("1", dto.PartitionNo);
            Assert.Equal(2, dto.PropertyTypeId);
            Assert.Equal("UPIC-123", dto.UPICId);
            Assert.True(dto.OpenPlot);
            Assert.Equal("CSN456", dto.CSN);
            Assert.Equal("SZ01", dto.SubZoneNo);
            Assert.Equal("P123", dto.PlotNo);
            Assert.Equal(1, dto.CategoryId);
            Assert.Equal("R", dto.Type);
            Assert.Equal("MAIN", dto.PartType);
            Assert.Equal("Mr", dto.OwnerTitle);
            Assert.Equal("John Doe", dto.OwnerName);
            Assert.Equal("Mr", dto.OwnerTitleEnglish);
            Assert.Equal("John Doe Eng", dto.OwnerNameEnglish);
            Assert.Equal("Mrs", dto.OccupierTitle);
            Assert.Equal("Jane Doe", dto.OccupierName);
            Assert.Equal("Mrs", dto.OccupierTitleEnglish);
            Assert.Equal("Jane Doe Eng", dto.OccupierNameEnglish);
            Assert.Equal("101", dto.FlatOrShopNo);
            Assert.Equal("Flat 101", dto.FlatOrShopName);
            Assert.Equal("101E", dto.FlatOrShopNoEnglish);
            Assert.Equal("Flat 101 English", dto.FlatOrShopNameEnglish);
            Assert.Equal("123 Main Street", dto.Address);
            Assert.Equal("City Center", dto.Location);
            Assert.Equal("123 Main Street English", dto.AddressEnglish);
            Assert.Equal("City Center English", dto.LocationEnglish);
            Assert.Equal("9876543210", dto.MobileNo);
            Assert.Equal("test@example.com", dto.EmailId);
            Assert.Equal(5, dto.SocietyDetailId);
            Assert.False(dto.MarkedForDeletion);
        }

        [Fact]
        public void CreatePropertyDto_StringProperties_AutoTrim()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                PropertyNo = "  22  ",
                PartitionNo = "  1  ",
                UPICId = "  UPIC-123  ",
                CSN = "  CSN456  ",
                SubZoneNo = "  SZ01  ",
                PlotNo = "  P123  ",
                Type = "  R  ",
                PartType = "  MAIN  ",
                OwnerTitle = "  Mr  ",
                OwnerName = "  John Doe  ",
                OwnerTitleEnglish = "  Mr  ",
                OwnerNameEnglish = "  John Doe Eng  ",
                OccupierTitle = "  Mrs  ",
                OccupierName = "  Jane Doe  ",
                OccupierTitleEnglish = "  Mrs  ",
                OccupierNameEnglish = "  Jane Doe Eng  ",
                FlatOrShopNo = "  101  ",
                FlatOrShopName = "  Flat 101  ",
                FlatOrShopNoEnglish = "  101E  ",
                FlatOrShopNameEnglish = "  Flat 101 English  ",
                Address = "  123 Main Street  ",
                Location = "  City Center  ",
                AddressEnglish = "  123 Main Street English  ",
                LocationEnglish = "  City Center English  ",
                MobileNo = "  9876543210  ",
                EmailId = "  TEST@EXAMPLE.COM  "
            };

            // Verify auto-trim on all string properties
            Assert.Equal("22", dto.PropertyNo);
            Assert.Equal("1", dto.PartitionNo);
            Assert.Equal("UPIC-123", dto.UPICId);
            Assert.Equal("CSN456", dto.CSN);
            Assert.Equal("SZ01", dto.SubZoneNo);
            Assert.Equal("P123", dto.PlotNo);
            Assert.Equal("R", dto.Type);
            Assert.Equal("MAIN", dto.PartType);
            Assert.Equal("Mr", dto.OwnerTitle);
            Assert.Equal("John Doe", dto.OwnerName);
            Assert.Equal("Mr", dto.OwnerTitleEnglish);
            Assert.Equal("John Doe Eng", dto.OwnerNameEnglish);
            Assert.Equal("Mrs", dto.OccupierTitle);
            Assert.Equal("Jane Doe", dto.OccupierName);
            Assert.Equal("Mrs", dto.OccupierTitleEnglish);
            Assert.Equal("Jane Doe Eng", dto.OccupierNameEnglish);
            Assert.Equal("101", dto.FlatOrShopNo);
            Assert.Equal("Flat 101", dto.FlatOrShopName);
            Assert.Equal("101E", dto.FlatOrShopNoEnglish);
            Assert.Equal("Flat 101 English", dto.FlatOrShopNameEnglish);
            Assert.Equal("123 Main Street", dto.Address);
            Assert.Equal("City Center", dto.Location);
            Assert.Equal("123 Main Street English", dto.AddressEnglish);
            Assert.Equal("City Center English", dto.LocationEnglish);
            Assert.Equal("9876543210", dto.MobileNo);
            Assert.Equal("test@example.com", dto.EmailId); // Also converts to lowercase
        }

        [Fact]
        public void CreatePropertyDto_WhitespaceOnly_SetsToNull()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                PropertyNo = "   ",
                PartitionNo = "   ",
                UPICId = "   ",
                CSN = "   ",
                SubZoneNo = "   ",
                PlotNo = "   ",
                Type = "   ",
                PartType = "   ",
                OwnerTitle = "   ",
                OwnerName = "   ",
                OccupierTitle = "   ",
                OccupierName = "   ",
                FlatOrShopNo = "   ",
                FlatOrShopName = "   ",
                Address = "   ",
                Location = "   ",
                MobileNo = "   ",
                EmailId = "   "
            };

            Assert.Null(dto.PropertyNo);
            Assert.Null(dto.PartitionNo);
            Assert.Null(dto.UPICId);
            Assert.Null(dto.CSN);
            Assert.Null(dto.SubZoneNo);
            Assert.Null(dto.PlotNo);
            Assert.Null(dto.Type);
            Assert.Null(dto.PartType);
            Assert.Null(dto.OwnerTitle);
            Assert.Null(dto.OwnerName);
            Assert.Null(dto.OccupierTitle);
            Assert.Null(dto.OccupierName);
            Assert.Null(dto.FlatOrShopNo);
            Assert.Null(dto.FlatOrShopName);
            Assert.Null(dto.Address);
            Assert.Null(dto.Location);
            Assert.Null(dto.MobileNo);
            Assert.Null(dto.EmailId);
        }

        [Fact]
        public void CreatePropertyDto_RequiredFields_FailValidationWhenMissing()
        {
            var dto = new CreatePropertyDto(); // Missing TaxZoneId and WardId

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("TaxZoneId"));
            Assert.Contains(results, r => r.ErrorMessage!.Contains("WardId"));
        }

        [Fact]
        public void CreatePropertyDto_ValidRequiredFields_PassValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void CreatePropertyDto_InvalidPropertyTypeId_FailsValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                PropertyTypeId = 0 // Invalid - must be >= 1
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("PropertyTypeId"));
        }

        [Fact]
        public void CreatePropertyDto_InvalidCategoryId_FailsValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                CategoryId = 0 // Invalid - must be >= 1
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("CategoryId"));
        }

        [Fact]
        public void CreatePropertyDto_InvalidSocietyDetailId_FailsValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                SocietyDetailId = 0 // Invalid - must be >= 1
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("SocietyDetailId"));
        }

        [Fact]
        public void CreatePropertyDto_PropertyNoExceedsMaxLength_FailsValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                PropertyNo = new string('A', 11) // Max is 10
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
        }

        [Fact]
        public void CreatePropertyDto_InvalidUPICIdFormat_FailsValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                UPICId = "UPIC@#$%" // Invalid characters
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
        }

        [Fact]
        public void CreatePropertyDto_ValidUPICIdFormat_PassesValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                UPICId = "UPIC-123_ABC"
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void CreatePropertyDto_InvalidMobileNoFormat_FailsValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                MobileNo = "abc123" // Invalid - contains letters
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
        }

        [Fact]
        public void CreatePropertyDto_ValidMobileNoFormats_PassValidation()
        {
            var dto1 = new CreatePropertyDto { TaxZoneId = 10, WardId = 79, MobileNo = "9876543210" };
            var dto2 = new CreatePropertyDto { TaxZoneId = 10, WardId = 79, MobileNo = "+91-98765" };
            var dto3 = new CreatePropertyDto { TaxZoneId = 10, WardId = 79, MobileNo = "(123) 456" };

            Assert.Empty(Validate(dto1));
            Assert.Empty(Validate(dto2));
            Assert.Empty(Validate(dto3));
        }

        [Fact]
        public void CreatePropertyDto_InvalidEmailFormat_FailsValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                EmailId = "invalid-email"
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
        }

        [Fact]
        public void CreatePropertyDto_ValidEmailFormat_PassesValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                EmailId = "test@example.com"
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void CreatePropertyDto_DefaultMarkedForDeletion_IsFalse()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79
            };

            Assert.False(dto.MarkedForDeletion);
        }

        [Fact]
        public void CreatePropertyDto_OptionalFields_CanBeNull()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79
            };

            Assert.Null(dto.PropertyNo);
            Assert.Null(dto.PartitionNo);
            Assert.Null(dto.PropertyTypeId);
            Assert.Null(dto.UPICId);
            Assert.Null(dto.OpenPlot);
            Assert.Null(dto.CSN);
            Assert.Null(dto.CategoryId);
            Assert.Null(dto.OwnerTitle);
            Assert.Null(dto.OwnerName);
            Assert.Null(dto.MobileNo);
            Assert.Null(dto.EmailId);
            Assert.Null(dto.SocietyDetailId);
        }

        [Fact]
        public void CreatePropertyDto_AllStringFieldsMaxLength_PassValidation()
        {
            var dto = new CreatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                PropertyNo = new string('A', 10),
                PartitionNo = new string('B', 10),
                UPICId = new string('C', 30),
                CSN = new string('D', 30),
                SubZoneNo = new string('E', 20),
                PlotNo = new string('F', 20),
                Type = new string('G', 5),
                PartType = new string('H', 20),
                OwnerTitle = new string('I', 20),
                OwnerName = new string('J', 1000),
                FlatOrShopNo = new string('K', 100),
                FlatOrShopName = new string('L', 200),
                Address = new string('M', 500),
                Location = new string('N', 200),
                MobileNo = "1234567890123",
                EmailId = "a@b.co"
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }
    }

    #endregion

    #region UpdatePropertyDto Tests

    public class UpdatePropertyDtoTests
    {
        private static IList<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void UpdatePropertyDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new UpdatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                PropertyNo = "22",
                PartitionNo = "1",
                PropertyTypeId = 2,
                UPICId = "UPIC-123",
                OpenPlot = true,
                CSN = "CSN456",
                SubZoneNo = "SZ01",
                PlotNo = "P123",
                CategoryId = 1,
                Type = "R",
                PartType = "MAIN",
                OwnerTitle = "Mr",
                OwnerName = "John Doe",
                OwnerTitleEnglish = "Mr",
                OwnerNameEnglish = "John Doe Eng",
                OccupierTitle = "Mrs",
                OccupierName = "Jane Doe",
                OccupierTitleEnglish = "Mrs",
                OccupierNameEnglish = "Jane Doe Eng",
                FlatOrShopNo = "101",
                FlatOrShopName = "Flat 101",
                FlatOrShopNoEnglish = "101E",
                FlatOrShopNameEnglish = "Flat 101 English",
                Address = "123 Main Street",
                Location = "City Center",
                AddressEnglish = "123 Main Street English",
                LocationEnglish = "City Center English",
                MobileNo = "9876543210",
                EmailId = "test@example.com",
                SocietyDetailId = 5,
                MarkedForDeletion = true
            };

            Assert.Equal(10, dto.TaxZoneId);
            Assert.Equal(79, dto.WardId);
            Assert.Equal("22", dto.PropertyNo);
            Assert.Equal("1", dto.PartitionNo);
            Assert.Equal(2, dto.PropertyTypeId);
            Assert.Equal("UPIC-123", dto.UPICId);
            Assert.True(dto.OpenPlot);
            Assert.Equal("CSN456", dto.CSN);
            Assert.Equal("SZ01", dto.SubZoneNo);
            Assert.Equal("P123", dto.PlotNo);
            Assert.Equal(1, dto.CategoryId);
            Assert.Equal("R", dto.Type);
            Assert.Equal("MAIN", dto.PartType);
            Assert.Equal("Mr", dto.OwnerTitle);
            Assert.Equal("John Doe", dto.OwnerName);
            Assert.Equal("Mr", dto.OwnerTitleEnglish);
            Assert.Equal("John Doe Eng", dto.OwnerNameEnglish);
            Assert.Equal("Mrs", dto.OccupierTitle);
            Assert.Equal("Jane Doe", dto.OccupierName);
            Assert.Equal("Mrs", dto.OccupierTitleEnglish);
            Assert.Equal("Jane Doe Eng", dto.OccupierNameEnglish);
            Assert.Equal("101", dto.FlatOrShopNo);
            Assert.Equal("Flat 101", dto.FlatOrShopName);
            Assert.Equal("101E", dto.FlatOrShopNoEnglish);
            Assert.Equal("Flat 101 English", dto.FlatOrShopNameEnglish);
            Assert.Equal("123 Main Street", dto.Address);
            Assert.Equal("City Center", dto.Location);
            Assert.Equal("123 Main Street English", dto.AddressEnglish);
            Assert.Equal("City Center English", dto.LocationEnglish);
            Assert.Equal("9876543210", dto.MobileNo);
            Assert.Equal("test@example.com", dto.EmailId);
            Assert.Equal(5, dto.SocietyDetailId);
            Assert.True(dto.MarkedForDeletion);
        }

        [Fact]
        public void UpdatePropertyDto_StringProperties_AutoTrim()
        {
            var dto = new UpdatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                PropertyNo = "  22  ",
                PartitionNo = "  1  ",
                UPICId = "  UPIC-123  ",
                CSN = "  CSN456  ",
                SubZoneNo = "  SZ01  ",
                PlotNo = "  P123  ",
                Type = "  R  ",
                PartType = "  MAIN  ",
                OwnerTitle = "  Mr  ",
                OwnerName = "  John Doe  ",
                OwnerTitleEnglish = "  Mr  ",
                OwnerNameEnglish = "  John Doe Eng  ",
                OccupierTitle = "  Mrs  ",
                OccupierName = "  Jane Doe  ",
                OccupierTitleEnglish = "  Mrs  ",
                OccupierNameEnglish = "  Jane Doe Eng  ",
                FlatOrShopNo = "  101  ",
                FlatOrShopName = "  Flat 101  ",
                FlatOrShopNoEnglish = "  101E  ",
                FlatOrShopNameEnglish = "  Flat 101 English  ",
                Address = "  123 Main Street  ",
                Location = "  City Center  ",
                AddressEnglish = "  123 Main Street English  ",
                LocationEnglish = "  City Center English  ",
                MobileNo = "  9876543210  ",
                EmailId = "  TEST@EXAMPLE.COM  "
            };

            Assert.Equal("22", dto.PropertyNo);
            Assert.Equal("1", dto.PartitionNo);
            Assert.Equal("UPIC-123", dto.UPICId);
            Assert.Equal("CSN456", dto.CSN);
            Assert.Equal("SZ01", dto.SubZoneNo);
            Assert.Equal("P123", dto.PlotNo);
            Assert.Equal("R", dto.Type);
            Assert.Equal("MAIN", dto.PartType);
            Assert.Equal("Mr", dto.OwnerTitle);
            Assert.Equal("John Doe", dto.OwnerName);
            Assert.Equal("Mr", dto.OwnerTitleEnglish);
            Assert.Equal("John Doe Eng", dto.OwnerNameEnglish);
            Assert.Equal("Mrs", dto.OccupierTitle);
            Assert.Equal("Jane Doe", dto.OccupierName);
            Assert.Equal("Mrs", dto.OccupierTitleEnglish);
            Assert.Equal("Jane Doe Eng", dto.OccupierNameEnglish);
            Assert.Equal("101", dto.FlatOrShopNo);
            Assert.Equal("Flat 101", dto.FlatOrShopName);
            Assert.Equal("101E", dto.FlatOrShopNoEnglish);
            Assert.Equal("Flat 101 English", dto.FlatOrShopNameEnglish);
            Assert.Equal("123 Main Street", dto.Address);
            Assert.Equal("City Center", dto.Location);
            Assert.Equal("123 Main Street English", dto.AddressEnglish);
            Assert.Equal("City Center English", dto.LocationEnglish);
            Assert.Equal("9876543210", dto.MobileNo);
            Assert.Equal("test@example.com", dto.EmailId);
        }

        [Fact]
        public void UpdatePropertyDto_WhitespaceOnly_SetsToNull()
        {
            var dto = new UpdatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                PropertyNo = "   ",
                PartitionNo = "   ",
                UPICId = "   ",
                CSN = "   ",
                SubZoneNo = "   ",
                PlotNo = "   ",
                Type = "   ",
                PartType = "   ",
                OwnerTitle = "   ",
                OwnerName = "   ",
                OwnerTitleEnglish = "   ",
                OwnerNameEnglish = "   ",
                OccupierTitle = "   ",
                OccupierName = "   ",
                OccupierTitleEnglish = "   ",
                OccupierNameEnglish = "   ",
                FlatOrShopNo = "   ",
                FlatOrShopName = "   ",
                FlatOrShopNoEnglish = "   ",
                FlatOrShopNameEnglish = "   ",
                Address = "   ",
                Location = "   ",
                AddressEnglish = "   ",
                LocationEnglish = "   ",
                MobileNo = "   ",
                EmailId = "   "
            };

            Assert.Null(dto.PropertyNo);
            Assert.Null(dto.PartitionNo);
            Assert.Null(dto.UPICId);
            Assert.Null(dto.CSN);
            Assert.Null(dto.SubZoneNo);
            Assert.Null(dto.PlotNo);
            Assert.Null(dto.Type);
            Assert.Null(dto.PartType);
            Assert.Null(dto.OwnerTitle);
            Assert.Null(dto.OwnerName);
            Assert.Null(dto.OwnerTitleEnglish);
            Assert.Null(dto.OwnerNameEnglish);
            Assert.Null(dto.OccupierTitle);
            Assert.Null(dto.OccupierName);
            Assert.Null(dto.OccupierTitleEnglish);
            Assert.Null(dto.OccupierNameEnglish);
            Assert.Null(dto.FlatOrShopNo);
            Assert.Null(dto.FlatOrShopName);
            Assert.Null(dto.FlatOrShopNoEnglish);
            Assert.Null(dto.FlatOrShopNameEnglish);
            Assert.Null(dto.Address);
            Assert.Null(dto.Location);
            Assert.Null(dto.AddressEnglish);
            Assert.Null(dto.LocationEnglish);
            Assert.Null(dto.MobileNo);
            Assert.Null(dto.EmailId);
        }

        [Fact]
        public void UpdatePropertyDto_RequiredFields_FailValidationWhenMissing()
        {
            var dto = new UpdatePropertyDto();

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("TaxZoneId"));
            Assert.Contains(results, r => r.ErrorMessage!.Contains("WardId"));
        }

        [Fact]
        public void UpdatePropertyDto_ValidRequiredFields_PassValidation()
        {
            var dto = new UpdatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertyDto_InvalidPropertyTypeId_FailsValidation()
        {
            var dto = new UpdatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                PropertyTypeId = 0
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
        }

        [Fact]
        public void UpdatePropertyDto_InvalidEmailFormat_FailsValidation()
        {
            var dto = new UpdatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79,
                EmailId = "invalid-email"
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
        }

        [Fact]
        public void UpdatePropertyDto_DefaultMarkedForDeletion_IsFalse()
        {
            var dto = new UpdatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79
            };

            Assert.False(dto.MarkedForDeletion);
        }

        [Fact]
        public void UpdatePropertyDto_OptionalFields_CanBeNull()
        {
            var dto = new UpdatePropertyDto
            {
                TaxZoneId = 10,
                WardId = 79
            };

            Assert.Null(dto.PropertyNo);
            Assert.Null(dto.PartitionNo);
            Assert.Null(dto.PropertyTypeId);
            Assert.Null(dto.UPICId);
            Assert.Null(dto.OpenPlot);
            Assert.Null(dto.CSN);
            Assert.Null(dto.CategoryId);
            Assert.Null(dto.SocietyDetailId);
        }
    }

    #endregion

    #region PropertyDto Tests

    public class PropertyDtoTests
    {
        [Fact]
        public void PropertyDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new PropertyDto
            {
                Id = 549357,
                TaxZoneId = 10,
                WardId = 79,
                PropertyNo = "22",
                PartitionNo = "1",
                PropertyTypeId = 2,
                UPICId = "UPIC-123",
                OpenPlot = true,
                CSN = "CSN456",
                SubZoneNo = "SZ01",
                PlotNo = "P123",
                CategoryId = 1,
                Type = "R",
                PartType = "MAIN",
                OwnerTitle = "Mr",
                OwnerName = "John Doe",
                OwnerTitleEnglish = "Mr",
                OwnerNameEnglish = "John Doe Eng",
                OccupierTitle = "Mrs",
                OccupierName = "Jane Doe",
                OccupierTitleEnglish = "Mrs",
                OccupierNameEnglish = "Jane Doe Eng",
                FlatOrShopNo = "101",
                FlatOrShopName = "Flat 101",
                FlatOrShopNoEnglish = "101E",
                FlatOrShopNameEnglish = "Flat 101 English",
                Address = "123 Main Street",
                Location = "City Center",
                AddressEnglish = "123 Main Street English",
                LocationEnglish = "City Center English",
                MobileNo = "9876543210",
                EmailId = "test@example.com",
                SocietyDetailId = 5,
                MarkedForDeletion = false
            };

            Assert.Equal(549357, dto.Id);
            Assert.Equal(10, dto.TaxZoneId);
            Assert.Equal(79, dto.WardId);
            Assert.Equal("22", dto.PropertyNo);
            Assert.Equal("1", dto.PartitionNo);
        }

        [Fact]
        public void PropertyDto_DisplayProperty_WithBothPropertyNoAndPartitionNo()
        {
            var dto = new PropertyDto
            {
                PropertyNo = "22",
                PartitionNo = "1"
            };

            Assert.Equal("22-1", dto.DisplayProperty);
        }

        [Fact]
        public void PropertyDto_DisplayProperty_WithOnlyPropertyNo()
        {
            var dto = new PropertyDto
            {
                PropertyNo = "22",
                PartitionNo = null
            };

            Assert.Equal("22", dto.DisplayProperty);
        }

        [Fact]
        public void PropertyDto_DisplayProperty_WithOnlyPartitionNo()
        {
            var dto = new PropertyDto
            {
                PropertyNo = null,
                PartitionNo = "1"
            };

            Assert.Equal("-1", dto.DisplayProperty);
        }

        [Fact]
        public void PropertyDto_DisplayProperty_WithNeitherPropertyNoNorPartitionNo()
        {
            var dto = new PropertyDto
            {
                PropertyNo = null,
                PartitionNo = null
            };

            Assert.Equal(string.Empty, dto.DisplayProperty);
        }

        [Fact]
        public void PropertyDto_DisplayProperty_WithEmptyPropertyNo()
        {
            var dto = new PropertyDto
            {
                PropertyNo = "",
                PartitionNo = "1"
            };

            Assert.Equal("-1", dto.DisplayProperty);
        }

        [Fact]
        public void PropertyDto_DisplayProperty_WithWhitespacePropertyNo()
        {
            var dto = new PropertyDto
            {
                PropertyNo = "   ",
                PartitionNo = "1"
            };

            Assert.Equal("-1", dto.DisplayProperty);
        }

        [Fact]
        public void PropertyDto_DisplayProperty_WithEmptyPartitionNo()
        {
            var dto = new PropertyDto
            {
                PropertyNo = "22",
                PartitionNo = ""
            };

            Assert.Equal("22", dto.DisplayProperty);
        }

        [Fact]
        public void PropertyDto_DisplayProperty_WithWhitespacePartitionNo()
        {
            var dto = new PropertyDto
            {
                PropertyNo = "22",
                PartitionNo = "   "
            };

            Assert.Equal("22", dto.DisplayProperty);
        }
    }

    #endregion

    #region PlotDetailsEntity Tests

    public class PlotDetailsEntityTests
    {
        [Fact]
        public void PlotDetailsEntity_AllProperties_GetSet_WorksCorrectly()
        {
            var entity = new PlotDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                PlotArea = 1500.25,
                PlotTaxableAreaSqFt = 1200.0,
                OpenPlotType = "R",
                OpenPlotRenterName = "John Doe",
                OpenPlotLength = 50.0,
                OpenPlotWidth = 30.0,
                PlotTaxableAreaSqMtr = 111.48,
                PlotAreaSqMtr = 139.35,
                OpenPlotSubmissionType = "Standard",
                PlotAreaMtrLength = 15.24,
                PlotAreaMtrWidth = 9.14,
                PlotAreaFtLength = 50.0,
                PlotAreaFtWidth = 30.0,
                MarkedForDeletion = false,
                IsActive = true
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(549357, entity.PropertyId);
            Assert.Equal(1500.25, entity.PlotArea);
            Assert.Equal(1200.0, entity.PlotTaxableAreaSqFt);
            Assert.Equal("R", entity.OpenPlotType);
            Assert.Equal("John Doe", entity.OpenPlotRenterName);
            Assert.Equal(50.0, entity.OpenPlotLength);
            Assert.Equal(30.0, entity.OpenPlotWidth);
            Assert.Equal(111.48, entity.PlotTaxableAreaSqMtr);
            Assert.Equal(139.35, entity.PlotAreaSqMtr);
            Assert.Equal("Standard", entity.OpenPlotSubmissionType);
            Assert.Equal(15.24, entity.PlotAreaMtrLength);
            Assert.Equal(9.14, entity.PlotAreaMtrWidth);
            Assert.Equal(50.0, entity.PlotAreaFtLength);
            Assert.Equal(30.0, entity.PlotAreaFtWidth);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void PlotDetailsEntity_OptionalFields_CanBeNull()
        {
            var entity = new PlotDetailsEntity
            {
                Id = 1,
                IsActive = true,
                MarkedForDeletion = false
            };

            Assert.Null(entity.PropertyId);
            Assert.Null(entity.PlotArea);
            Assert.Null(entity.PlotTaxableAreaSqFt);
            Assert.Null(entity.OpenPlotType);
            Assert.Null(entity.OpenPlotRenterName);
            Assert.Null(entity.OpenPlotLength);
            Assert.Null(entity.OpenPlotWidth);
            Assert.Null(entity.PlotTaxableAreaSqMtr);
            Assert.Null(entity.PlotAreaSqMtr);
            Assert.Null(entity.OpenPlotSubmissionType);
            Assert.Null(entity.PlotAreaMtrLength);
            Assert.Null(entity.PlotAreaMtrWidth);
            Assert.Null(entity.PlotAreaFtLength);
            Assert.Null(entity.PlotAreaFtWidth);
        }

        [Fact]
        public void PlotDetailsEntity_InheritsFromBaseEntity()
        {
            var entity = new PlotDetailsEntity();
            Assert.IsAssignableFrom<BaseEntity>(entity);
        }

        [Fact]
        public void PlotDetailsEntity_DefaultValues_SetCorrectly()
        {
            var entity = new PlotDetailsEntity();

            Assert.Equal(0, entity.Id);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }
    }

    #endregion

    #region PropertyAssessmentEntity Tests

    public class PropertyAssessmentEntityTests
    {
        [Fact]
        public void PropertyAssessmentEntity_AllProperties_GetSet_WorksCorrectly()
        {
            var now = DateTime.Now;
            var entity = new PropertyAssessmentEntity
            {
                Id = 1,
                PropertyId = 549357,
                OwnerTypeId = 2,
                AssessmentRemark = "Assessment remark",
                SurveyRemark = "Survey remark",
                FlatSystemRemark = "Flat system remark",
                CombPropRemark = "Combined property remark",
                AdharCardNo = "123456789012",
                RenterMobileNo = "8765432109",
                AssessmentNo = "A001",
                PrarupYadiPublishDate = now,
                AntimYadiPublishDate = now.AddDays(30),
                PropertyRegDate = now.AddDays(-365),
                ApplyTaxesFrom = 2023,
                PartOCDate = now.AddDays(-180),
                BHK = "3BHK",
                BlockNo = "B01",
                UsageCategoryId = 1,
                AlternativeEmailId = "alt@example.com",
                TotalBuiltupAreaSqFeet = 1500.0,
                TotalBuiltupAreaSqMeter = 139.35,
                Latitude = "18.5204",
                Longitude = "73.8567",
                NoOfResidentialToilets = 2,
                NoOfCommercialToilets = 1,
                MarkedForDeletion = false,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = now,
                UpdatedBy = 2,
                UpdatedDate = now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(549357, entity.PropertyId);
            Assert.Equal(2, entity.OwnerTypeId);
            Assert.Equal("Assessment remark", entity.AssessmentRemark);
            Assert.Equal("Survey remark", entity.SurveyRemark);
            Assert.Equal("Flat system remark", entity.FlatSystemRemark);
            Assert.Equal("Combined property remark", entity.CombPropRemark);
            Assert.Equal("123456789012", entity.AdharCardNo);
            Assert.Equal("8765432109", entity.RenterMobileNo);
            Assert.Equal("A001", entity.AssessmentNo);
            Assert.Equal(now, entity.PrarupYadiPublishDate);
            Assert.Equal(now.AddDays(30), entity.AntimYadiPublishDate);
            Assert.Equal(now.AddDays(-365), entity.PropertyRegDate);
            Assert.Equal((short)2023, entity.ApplyTaxesFrom);
            Assert.Equal(now.AddDays(-180), entity.PartOCDate);
            Assert.Equal("3BHK", entity.BHK);
            Assert.Equal("B01", entity.BlockNo);
            Assert.Equal(1, entity.UsageCategoryId);
            Assert.Equal("alt@example.com", entity.AlternativeEmailId);
            Assert.Equal(1500.0, entity.TotalBuiltupAreaSqFeet);
            Assert.Equal(139.35, entity.TotalBuiltupAreaSqMeter);
            Assert.Equal("18.5204", entity.Latitude);
            Assert.Equal("73.8567", entity.Longitude);
            Assert.Equal(2, entity.NoOfResidentialToilets);
            Assert.Equal(1, entity.NoOfCommercialToilets);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
            Assert.Equal(1, entity.CreatedBy);
            Assert.Equal(2, entity.UpdatedBy);
        }

        [Fact]
        public void PropertyAssessmentEntity_OptionalFields_CanBeNull()
        {
            var entity = new PropertyAssessmentEntity
            {
                Id = 1,
                PropertyId = 549357,
                IsActive = true,
                MarkedForDeletion = false
            };

            Assert.Null(entity.OwnerTypeId);
            Assert.Null(entity.AssessmentRemark);
            Assert.Null(entity.SurveyRemark);
            Assert.Null(entity.FlatSystemRemark);
            Assert.Null(entity.CombPropRemark);
            Assert.Null(entity.AdharCardNo);
            Assert.Null(entity.RenterMobileNo);
            Assert.Null(entity.AssessmentNo);
            Assert.Null(entity.PrarupYadiPublishDate);
            Assert.Null(entity.AntimYadiPublishDate);
            Assert.Null(entity.PropertyRegDate);
            Assert.Null(entity.ApplyTaxesFrom);
            Assert.Null(entity.PartOCDate);
            Assert.Null(entity.BHK);
            Assert.Null(entity.BlockNo);
            Assert.Null(entity.UsageCategoryId);
            Assert.Null(entity.AlternativeEmailId);
            Assert.Null(entity.TotalBuiltupAreaSqFeet);
            Assert.Null(entity.TotalBuiltupAreaSqMeter);
            Assert.Null(entity.Latitude);
            Assert.Null(entity.Longitude);
            Assert.Null(entity.NoOfResidentialToilets);
            Assert.Null(entity.NoOfCommercialToilets);
        }

        [Fact]
        public void PropertyAssessmentEntity_InheritsFromBaseEntity()
        {
            var entity = new PropertyAssessmentEntity();
            Assert.IsAssignableFrom<BaseEntity>(entity);
        }

        [Fact]
        public void PropertyAssessmentEntity_DefaultValues_SetCorrectly()
        {
            var entity = new PropertyAssessmentEntity();

            Assert.Equal(0, entity.Id);
            Assert.Equal(0, entity.PropertyId);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }
    }

    #endregion

    #region PropertyDetailsEntity Tests

    public class PropertyDetailsEntityTests
    {
        [Fact]
        public void PropertyDetailsEntity_AllProperties_GetSet_WorksCorrectly()
        {
            var now = DateTime.Now;
            var entity = new PropertyDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                FloorId = 2,
                SubFloorId = 1,
                ConstructionYear = "2015",
                AssessmentYear = "2023",
                ConstructionTypeId = 3,
                TypeOfUseId = 4,
                CarpetAreaSqMeter = 111.48,
                CarpetAreaSqFeet = 1200.0,
                BuiltupAreaSqMeter = 139.35,
                BuiltupAreaSqFeet = 1500.0,
                NoOfRooms = 5,
                RenterYesNO = true,
                RentMonthly = 25000.0,
                RentYearly = 300000.0,
                NonCalculateRentMonthly = 5000.0,
                RenterNameEnglish = "John Doe English",
                RenterName = "John Doe",
                AgreementFromDate = now.AddYears(-1),
                AgreementDate = now.AddYears(-1).AddDays(15),
                AgreementToDate = now.AddYears(1),
                SubTypeOfUseId = 2,
                TaxLiability = "Liable",
                IsTaxable = true,
                OccupancyDate = now.AddYears(-2),
                OccupancyApplyOrNot = true,
                OccupancyNumber = "OCC-001",
                MarkedForDeletion = false,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = now,
                UpdatedBy = 2,
                UpdatedDate = now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(549357, entity.PropertyId);
            Assert.Equal(2, entity.FloorId);
            Assert.Equal(1, entity.SubFloorId);
            Assert.Equal("2015", entity.ConstructionYear);
            Assert.Equal("2023", entity.AssessmentYear);
            Assert.Equal(3, entity.ConstructionTypeId);
            Assert.Equal(4, entity.TypeOfUseId);
            Assert.Equal(111.48, entity.CarpetAreaSqMeter);
            Assert.Equal(1200.0, entity.CarpetAreaSqFeet);
            Assert.Equal(139.35, entity.BuiltupAreaSqMeter);
            Assert.Equal(1500.0, entity.BuiltupAreaSqFeet);
            Assert.Equal(5, entity.NoOfRooms);
            Assert.True(entity.RenterYesNO);
            Assert.Equal(25000.0, entity.RentMonthly);
            Assert.Equal(300000.0, entity.RentYearly);
            Assert.Equal(5000.0, entity.NonCalculateRentMonthly);
            Assert.Equal("John Doe English", entity.RenterNameEnglish);
            Assert.Equal("John Doe", entity.RenterName);
            Assert.Equal(now.AddYears(-1), entity.AgreementFromDate);
            Assert.Equal(now.AddYears(-1).AddDays(15), entity.AgreementDate);
            Assert.Equal(now.AddYears(1), entity.AgreementToDate);
            Assert.Equal(2, entity.SubTypeOfUseId);
            Assert.Equal("Liable", entity.TaxLiability);
            Assert.True(entity.IsTaxable);
            Assert.Equal(now.AddYears(-2), entity.OccupancyDate);
            Assert.True(entity.OccupancyApplyOrNot);
            Assert.Equal("OCC-001", entity.OccupancyNumber);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void PropertyDetailsEntity_OptionalFields_CanBeNull()
        {
            var entity = new PropertyDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                FloorId = 2,
                ConstructionTypeId = 3,
                TypeOfUseId = 4,
                IsActive = true,
                MarkedForDeletion = false
            };

            Assert.Null(entity.SubFloorId);
            Assert.Null(entity.ConstructionYear);
            Assert.Null(entity.AssessmentYear);
            Assert.Null(entity.CarpetAreaSqMeter);
            Assert.Null(entity.CarpetAreaSqFeet);
            Assert.Null(entity.BuiltupAreaSqMeter);
            Assert.Null(entity.BuiltupAreaSqFeet);
            Assert.Null(entity.NoOfRooms);
            Assert.Null(entity.RenterYesNO);
            Assert.Null(entity.RentMonthly);
            Assert.Null(entity.RentYearly);
            Assert.Null(entity.NonCalculateRentMonthly);
            Assert.Null(entity.RenterNameEnglish);
            Assert.Null(entity.RenterName);
            Assert.Null(entity.AgreementFromDate);
            Assert.Null(entity.AgreementDate);
            Assert.Null(entity.AgreementToDate);
            Assert.Null(entity.SubTypeOfUseId);
            Assert.Null(entity.TaxLiability);
            Assert.Null(entity.IsTaxable);
            Assert.Null(entity.OccupancyDate);
            Assert.Null(entity.OccupancyApplyOrNot);
            Assert.Null(entity.OccupancyNumber);
        }

        [Fact]
        public void PropertyDetailsEntity_InheritsFromBaseEntity()
        {
            var entity = new PropertyDetailsEntity();
            Assert.IsAssignableFrom<BaseEntity>(entity);
        }

        [Fact]
        public void PropertyDetailsEntity_DefaultValues_SetCorrectly()
        {
            var entity = new PropertyDetailsEntity();

            Assert.Equal(0, entity.Id);
            Assert.Equal(0, entity.PropertyId);
            Assert.Equal(0, entity.FloorId);
            Assert.Equal(0, entity.ConstructionTypeId);
            Assert.Equal(0, entity.TypeOfUseId);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }
    }

    #endregion

    #region SocietyDetailsEntity Tests

    public class SocietyDetailsEntityTests
    {
        [Fact]
        public void SocietyDetailsEntity_AllProperties_GetSet_WorksCorrectly()
        {
            var now = DateTime.Now;
            var entity = new SocietyDetailsEntity
            {
                Id = 1,
                WingName = "West Wing",
                WingId = 5,
                SecretaryName = "Secretary Name",
                SocietyName = "ABC Society",
                ManagerName = "Manager Name",
                SecretaryNameEnglish = "Secretary Name English",
                SocietyNameEnglish = "ABC Society English",
                ManagerNameEnglish = "Manager Name English",
                ManagerMobileNo = "9876543210",
                SecretaryMobileNo = "8765432109",
                MarkedForDeletion = false,
                PropertyId = 549357,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = now,
                UpdatedBy = 2,
                UpdatedDate = now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal("West Wing", entity.WingName);
            Assert.Equal(5, entity.WingId);
            Assert.Equal("Secretary Name", entity.SecretaryName);
            Assert.Equal("ABC Society", entity.SocietyName);
            Assert.Equal("Manager Name", entity.ManagerName);
            Assert.Equal("Secretary Name English", entity.SecretaryNameEnglish);
            Assert.Equal("ABC Society English", entity.SocietyNameEnglish);
            Assert.Equal("Manager Name English", entity.ManagerNameEnglish);
            Assert.Equal("9876543210", entity.ManagerMobileNo);
            Assert.Equal("8765432109", entity.SecretaryMobileNo);
            Assert.False(entity.MarkedForDeletion);
            Assert.Equal(549357, entity.PropertyId);
            Assert.True(entity.IsActive);
            Assert.Equal(1, entity.CreatedBy);
            Assert.Equal(2, entity.UpdatedBy);
        }

        [Fact]
        public void SocietyDetailsEntity_OptionalFields_CanBeNull()
        {
            var entity = new SocietyDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                IsActive = true,
                MarkedForDeletion = false
            };

            Assert.Null(entity.WingName);
            Assert.Null(entity.WingId);
            Assert.Null(entity.SecretaryName);
            Assert.Null(entity.SocietyName);
            Assert.Null(entity.ManagerName);
            Assert.Null(entity.SecretaryNameEnglish);
            Assert.Null(entity.SocietyNameEnglish);
            Assert.Null(entity.ManagerNameEnglish);
            Assert.Null(entity.ManagerMobileNo);
            Assert.Null(entity.SecretaryMobileNo);
        }

        [Fact]
        public void SocietyDetailsEntity_InheritsFromBaseEntity()
        {
            var entity = new SocietyDetailsEntity();
            Assert.IsAssignableFrom<BaseEntity>(entity);
        }

        [Fact]
        public void SocietyDetailsEntity_DefaultValues_SetCorrectly()
        {
            var entity = new SocietyDetailsEntity();

            Assert.Equal(0, entity.Id);
            Assert.Null(entity.PropertyId);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }
    }

    #endregion

    #region Additional Entity Tests

    public class AdditionalEntityTests
    {
        [Fact]
        public void PropertyMastOldEntity_AllExtendedProperties_GetSet_WorksCorrectly()
        {
            var now = DateTime.Now;
            var entity = new PropertyMastOldEntity
            {
                OldCSN = "CSN123456",
                OldFloor = "Ground",
                OldConstructionTypeOfUseId = "CTOU01",
                OldUseType = "Commercial",
                OldConstArea = 2500.75,
                CreatedBy = 1,
                CreatedDate = now,
                UpdatedBy = 2,
                UpdatedDate = now
            };

            Assert.Equal("CSN123456", entity.OldCSN);
            Assert.Equal("Ground", entity.OldFloor);
            Assert.Equal("CTOU01", entity.OldConstructionTypeOfUseId);
            Assert.Equal("Commercial", entity.OldUseType);
            Assert.Equal(2500.75, entity.OldConstArea);
            Assert.Equal(1, entity.CreatedBy);
            Assert.Equal(now, entity.CreatedDate);
            Assert.Equal(2, entity.UpdatedBy);
            Assert.Equal(now, entity.UpdatedDate);
        }

        [Fact]
        public void PropertyDetailsOldEntity_AllExtendedProperties_GetSet_WorksCorrectly()
        {
            var now = DateTime.Now;
            var entity = new PropertyDetailsOldEntity
            {
                OldFloorId = "F1",
                CreatedBy = 1,
                CreatedDate = now,
                UpdatedBy = 2,
                UpdatedDate = now
            };

            Assert.Equal("F1", entity.OldFloorId);
            Assert.Equal(1, entity.CreatedBy);
            Assert.Equal(now, entity.CreatedDate);
            Assert.Equal(2, entity.UpdatedBy);
            Assert.Equal(now, entity.UpdatedDate);
        }

        [Fact]
        public void PropertyMastOldEntity_NumericBoundaryValues_WorkCorrectly()
        {
            var entity = new PropertyMastOldEntity
            {
                OldPropertyTypeId = int.MaxValue,
                OldAssessmentYear = int.MaxValue,
                NoOfOldToilets = int.MaxValue,
                OldTotalRooms = int.MaxValue
            };

            Assert.Equal(int.MaxValue, entity.OldPropertyTypeId);
            Assert.Equal(int.MaxValue, entity.OldAssessmentYear);
            Assert.Equal(int.MaxValue, entity.NoOfOldToilets);
            Assert.Equal(int.MaxValue, entity.OldTotalRooms);
        }

        [Fact]
        public void PropertyMastOldEntity_DoubleBoundaryValues_WorkCorrectly()
        {
            var entity = new PropertyMastOldEntity
            {
                OldALV = double.MaxValue,
                OldRV = double.MinValue,
                OldGeneralTax = 0.0,
                OldTotalTax = double.Epsilon
            };

            Assert.Equal(double.MaxValue, entity.OldALV);
            Assert.Equal(double.MinValue, entity.OldRV);
            Assert.Equal(0.0, entity.OldGeneralTax);
            Assert.Equal(double.Epsilon, entity.OldTotalTax);
        }

        [Fact]
        public void PropertyDetailsOldEntity_DoubleBoundaryValues_WorkCorrectly()
        {
            var entity = new PropertyDetailsOldEntity
            {
                OldCarpetAreaSqfeet = double.MaxValue,
                OldCarpetAreaSqMeter = double.MinValue
            };

            Assert.Equal(double.MaxValue, entity.OldCarpetAreaSqfeet);
            Assert.Equal(double.MinValue, entity.OldCarpetAreaSqMeter);
        }
    }

    #endregion

    #region PropertyRepository Extended Coverage Tests

    public class PropertyRepositoryExtendedCoverageTests
    {
        [Fact]
        public async Task GetBasicDetailsAsync_WithSocietyDetails_ReturnsSocietyWingInfo()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            // Setup master data
            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "Tax Zone 10", IsActive = true };

            // Setup society details
            var society = new SocietyDetailsEntity
            {
                Id = 100,
                WingId = 5,
                WingName = "West Wing",
                IsActive = true
            };

            // Property with SocietyDetailId
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                SocietyDetailId = 100,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.SocietyDetailsMast.Add(society);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(5, result.WingId);
            Assert.Equal("West Wing", result.WingName);
        }

        [Fact]
        public async Task GetBasicDetailsAsync_WithPropertyDetails_ReturnsSummedAreas()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            // Add multiple PropertyDetails records to test aggregation
            var detail1 = new PropertyDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                CarpetAreaSqMeter = 100.0,
                BuiltupAreaSqMeter = 120.0,
                CarpetAreaSqFeet = 1076.39,
                BuiltupAreaSqFeet = 1291.67,
                IsActive = true
            };

            var detail2 = new PropertyDetailsEntity
            {
                Id = 2,
                PropertyId = 549357,
                CarpetAreaSqMeter = 50.0,
                BuiltupAreaSqMeter = 60.0,
                CarpetAreaSqFeet = 538.20,
                BuiltupAreaSqFeet = 645.83,
                IsActive = true
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            context.PropertyDetails.Add(detail1);
            context.PropertyDetails.Add(detail2);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(150.0, result.TotalCarpetAreaSqMeter);
            Assert.Equal(180.0, result.TotalBuiltupAreaSqMeter);
            Assert.NotNull(result.TotalCarpetAreaSqFeet);
            Assert.NotNull(result.TotalBuiltupAreaSqFeet);
            Assert.True(Math.Abs(1614.59 - result.TotalCarpetAreaSqFeet.Value) < 0.01);
            Assert.True(Math.Abs(1937.5 - result.TotalBuiltupAreaSqFeet.Value) < 0.01);
        }

        [Fact]
        public async Task GetBasicDetailsAsync_WithPlotDetails_ReturnsPlotInfo()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var plot = new PlotDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                PlotArea = 2500.50,
                PlotAreaFtLength = 50.0,
                PlotAreaFtWidth = 50.0,
                PlotAreaMtrLength = 15.24,
                PlotAreaMtrWidth = 15.24,
                IsActive = true
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            context.PlotDetails.Add(plot);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(2500.50, result.PlotArea);
            Assert.Equal(50.0, result.PlotAreaFtLength);
            Assert.Equal(50.0, result.PlotAreaFtWidth);
            Assert.Equal(15.24, result.PlotAreaMtrLength);
            Assert.Equal(15.24, result.PlotAreaMtrWidth);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_WithSocietyDetails_UpdatesSocietyWingInfo()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };

            var society = new SocietyDetailsEntity
            {
                Id = 100,
                WingId = 1,
                WingName = "Old Wing",
                IsActive = true
            };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                SocietyDetailId = 100,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.SocietyDetailsMast.Add(society);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                WingId = 5,
                WingName = "New West Wing"
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(5, result.WingId);
            Assert.Equal("New West Wing", result.WingName);

            // Verify society was updated
            var updatedSociety = await context.SocietyDetailsMast.FindAsync(100);
            Assert.NotNull(updatedSociety);
            Assert.Equal(5, updatedSociety.WingId);
            Assert.Equal("New West Wing", updatedSociety.WingName);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_WithAllOptionalFields_UpdatesAllFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var category = new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Residential", IsActive = true };
            var propertyType = new PropertyTypeMasterEntity { Id = 2, PropertyDescription = "Apartment", IsActive = true };
            var wing = new WingEntity { Id = 5, WingNo = "A", IsActive = true }; // Add WingEntity for WingNo lookup

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyCategoryMaster.Add(category);
            context.PropertyTypeMasters.Add(propertyType);
            context.Set<WingEntity>().Add(wing);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                CategoryId = 1,
                PropertyTypeId = 2,
                PartitionNo = "P1",
                FlatOrShopNo = "F101",
                PlotNo = "PL123",
                SurveyNo = "SRV456",
                UPICId = "UPIC789",
                SubZoneNo = "SZ01",
                WingNo = "A",
                WingId = 5,
                NoOfResidentialToilets = 2,
                NoOfCommercialToilets = 1,
                PlotArea = 1500.0,
                PlotAreaFtLength = 40.0,
                PlotAreaFtWidth = 37.5,
                PlotAreaMtrLength = 12.19,
                PlotAreaMtrWidth = 11.43
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(1, result.CategoryId);
            Assert.Equal(2, result.PropertyTypeId);
            Assert.Equal("P1", result.PartitionNo);
            Assert.Equal("F101", result.FlatOrShopNo);
            Assert.Equal("PL123", result.PlotNo);
            Assert.Equal("SRV456", result.SurveyNo);
            Assert.Equal("UPIC789", result.UPICId);
            Assert.Equal("SZ01", result.SubZoneNo);
            Assert.Equal("A", result.WingNo);
            Assert.Equal(2, result.NoOfResidentialToilets);
            Assert.Equal(1, result.NoOfCommercialToilets);
            Assert.Equal(1500.0, result.PlotArea);
            Assert.Equal(40.0, result.PlotAreaFtLength);
            Assert.Equal(37.5, result.PlotAreaFtWidth);
            Assert.Equal(12.19, result.PlotAreaMtrLength);
            Assert.Equal(11.43, result.PlotAreaMtrWidth);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_NoAssessmentData_DoesNotInsertAssessment()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10
                // No assessment data (WingNo, NoOfResidentialToilets, etc.)
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);

            // Verify NO INSERT happened for assessment
            var assessmentCount = await context.PropertyMastDetails.CountAsync();
            Assert.Equal(0, assessmentCount);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_PlotDetailsExists_UpdatesAllPlotFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var plot = new PlotDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                PlotArea = 1000.0,
                PlotAreaFtLength = 30.0,
                PlotAreaFtWidth = 33.33,
                PlotAreaMtrLength = 9.14,
                PlotAreaMtrWidth = 10.16,
                IsActive = true
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            context.PlotDetails.Add(plot);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                PlotArea = 2000.0,
                PlotAreaFtLength = 50.0,
                PlotAreaFtWidth = 40.0,
                PlotAreaMtrLength = 15.24,
                PlotAreaMtrWidth = 12.19
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(2000.0, result.PlotArea);
            Assert.Equal(50.0, result.PlotAreaFtLength);
            Assert.Equal(40.0, result.PlotAreaFtWidth);
            Assert.Equal(15.24, result.PlotAreaMtrLength);
            Assert.Equal(12.19, result.PlotAreaMtrWidth);

            // Verify UPDATE happened (still 1 record)
            var plotCount = await context.PlotDetails.CountAsync();
            Assert.Equal(1, plotCount);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_WithWingIdOnly_UpdatesSocietyDetails()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };

            var society = new SocietyDetailsEntity
            {
                Id = 100,
                WingId = 1,
                WingName = null,
                IsActive = true
            };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                SocietyDetailId = 100,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.SocietyDetailsMast.Add(society);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                WingId = 10 // Only WingId, no WingName
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(10, result.WingId);

            // Verify society WingId was updated
            var updatedSociety = await context.SocietyDetailsMast.FindAsync(100);
            Assert.NotNull(updatedSociety);
            Assert.Equal(10, updatedSociety.WingId);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_WithWingNameOnly_UpdatesSocietyDetails()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };

            var society = new SocietyDetailsEntity
            {
                Id = 100,
                WingId = 1,
                WingName = "Old Wing",
                IsActive = true
            };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                SocietyDetailId = 100,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.SocietyDetailsMast.Add(society);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                WingName = "Updated Wing Name" // Only WingName, no WingId
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("Updated Wing Name", result.WingName);

            // Verify society WingName was updated
            var updatedSociety = await context.SocietyDetailsMast.FindAsync(100);
            Assert.NotNull(updatedSociety);
            Assert.Equal("Updated Wing Name", updatedSociety.WingName);
        }

        [Fact]
        public async Task GetBasicDetailsAsync_WithAssessmentWingNo_ReturnsWingNoFromAssessment()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var wing = new WingEntity { Id = 1, WingNo = "Assessment Wing", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                SocietyDetailId = 1, // Linked to society
                IsActive = true,
                MarkedForDeletion = false
            };

            var society = new SocietyDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                WingId = 1, // Linked to WingEntity
                IsActive = true,
                MarkedForDeletion = false
            };

            var assessment = new PropertyAssessmentEntity
            {
                Id = 1,
                PropertyId = 549357,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.Set<WingEntity>().Add(wing);
            context.PropertyMast.Add(property);
            context.SocietyDetailsMast.Add(society);
            context.PropertyMastDetails.Add(assessment);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal("Assessment Wing", result.WingNo); // Should return WingNo from SocietyDetailsMast via WingEntity
        }

        [Fact]
        public async Task GetBasicDetailsAsync_InactiveSociety_DoesNotReturnSocietyInfo()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };

            var society = new SocietyDetailsEntity
            {
                Id = 100,
                WingId = 5,
                WingName = "Inactive Wing",
                IsActive = false // Inactive
            };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                SocietyDetailId = 100,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.SocietyDetailsMast.Add(society);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Null(result.WingId); // Should be null because society is inactive
            Assert.Null(result.WingName);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_AssessmentExists_UpdatesAllAssessmentFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var wing = new WingEntity { Id = 5, WingNo = "NEW", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var assessment = new PropertyAssessmentEntity
            {
                Id = 1,
                PropertyId = 549357,
                NoOfResidentialToilets = 1,
                NoOfCommercialToilets = 0,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.Set<WingEntity>().Add(wing);
            context.PropertyMast.Add(property);
            context.PropertyMastDetails.Add(assessment);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                WingId = 5,
                WingNo = "NEW",
                NoOfResidentialToilets = 3,
                NoOfCommercialToilets = 2
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("NEW", result.WingNo);
            Assert.Equal(3, result.NoOfResidentialToilets);
            Assert.Equal(2, result.NoOfCommercialToilets);

            // Verify UPDATE happened (still 1 record)
            var assessmentCount = await context.PropertyMastDetails.CountAsync();
            Assert.Equal(1, assessmentCount);
            
            // Verify Society was created and linked to WingEntity
            var society = await context.SocietyDetailsMast.FirstOrDefaultAsync(s => s.PropertyId == 549357);
            Assert.NotNull(society);
            Assert.Equal(5, society.WingId);
        }
    }

    #endregion
}
