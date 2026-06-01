using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for Property Old Details functionality
/// Target: 100% line coverage and 100% branch coverage
/// </summary>
public class PropertyOldDetailsComprehensiveTests
{
    #region Entity Tests - PropertyDetailsOldEntity

    [Fact]
    public void PropertyDetailsOldEntity_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var now = DateTime.Now;
        var entity = new PropertyDetailsOldEntity
        {
            Id = 1,
            PropertyMastOldId = 100,
            OldFloorId = 5,
            OldSubFloorId = 12,
            OldConstructionYear = "2015",
            OldAssessmentYear = "2020",
            OldConstructionTypeId = 2,
            OldTypeOfUseId = 3,
            OldSubTypeOfUseId = 7,
            OldCarpetAreaSqMeter = 111.48,
            OldCarpetAreaSqFeet = 1200.50,
            OldBuiltupAreaSqMeter = 130.50,
            OldBuiltupAreaSqFeet = 1400.75,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal(100, entity.PropertyMastOldId);
        Assert.Equal(5, entity.OldFloorId);
        Assert.Equal(12, entity.OldSubFloorId);
        Assert.Equal("2015", entity.OldConstructionYear);
        Assert.Equal("2020", entity.OldAssessmentYear);
        Assert.Equal(2, entity.OldConstructionTypeId);
        Assert.Equal(3, entity.OldTypeOfUseId);
        Assert.Equal(7, entity.OldSubTypeOfUseId);
        Assert.Equal(111.48, entity.OldCarpetAreaSqMeter);
        Assert.Equal(1200.50, entity.OldCarpetAreaSqFeet);
        Assert.Equal(130.50, entity.OldBuiltupAreaSqMeter);
        Assert.Equal(1400.75, entity.OldBuiltupAreaSqFeet);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now, entity.UpdatedDate);
    }

    [Fact]
    public void PropertyDetailsOldEntity_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var entity = new PropertyDetailsOldEntity();

        // Assert
        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.PropertyMastOldId);
        // Nullable int properties default to null
        Assert.Null(entity.OldFloorId);
        Assert.Null(entity.OldConstructionTypeId);
        Assert.Null(entity.OldTypeOfUseId);
        // Nullable properties
        Assert.Null(entity.OldSubFloorId);
        Assert.Null(entity.OldConstructionYear);
        Assert.Null(entity.OldAssessmentYear);
        Assert.Null(entity.OldSubTypeOfUseId);
        Assert.Null(entity.OldCarpetAreaSqMeter);
        Assert.Null(entity.OldCarpetAreaSqFeet);
        Assert.Null(entity.OldBuiltupAreaSqMeter);
        Assert.Null(entity.OldBuiltupAreaSqFeet);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyDetailsOldEntity_RequiredAndNullableFields_AreCorrect()
    {
        // Arrange & Act
        var entity = new PropertyDetailsOldEntity
        {
            PropertyMastOldId = 100,
            OldFloorId = 1,  // Required field
            OldSubFloorId = null,  // Nullable
            OldConstructionYear = null,
            OldAssessmentYear = null,
            OldConstructionTypeId = 2,  // Required field
            OldTypeOfUseId = 3,  // Required field
            OldSubTypeOfUseId = null,  // Nullable
            OldCarpetAreaSqMeter = null,
            OldCarpetAreaSqFeet = null,
            OldBuiltupAreaSqMeter = null,
            OldBuiltupAreaSqFeet = null
        };

        // Assert - Required fields
        Assert.Equal(1, entity.OldFloorId);
        Assert.Equal(2, entity.OldConstructionTypeId);
        Assert.Equal(3, entity.OldTypeOfUseId);

        // Assert - Nullable fields
        Assert.Null(entity.OldSubFloorId);
        Assert.Null(entity.OldConstructionYear);
        Assert.Null(entity.OldAssessmentYear);
        Assert.Null(entity.OldSubTypeOfUseId);
        Assert.Null(entity.OldCarpetAreaSqMeter);
        Assert.Null(entity.OldCarpetAreaSqFeet);
        Assert.Null(entity.OldBuiltupAreaSqMeter);
        Assert.Null(entity.OldBuiltupAreaSqFeet);
    }

    [Fact]
    public void PropertyDetailsOldEntity_MarkedForDeletion_CanBeSet()
    {
        // Arrange
        var entity = new PropertyDetailsOldEntity { PropertyMastOldId = 100 };
        var deletionDate = DateTime.Now;

        // Act
        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = deletionDate;

        // Assert
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    #endregion

    #region Entity Tests - PropertyMastOldEntity

    [Fact]
    public void PropertyMastOldEntity_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var now = DateTime.Now;
        var assessmentDate = new DateTime(2025, 1, 21);

        var entity = new PropertyMastOldEntity
        {
            Id = 1,
            OldWardNo = "1",
            OldPropertyNo = "86",
            OldPartitionNo = "A",
            OldEgovNo = "MM1",
            OldPropertyTypeId = 1,
            OldALV = 50000.50,
            OldRV = 60000.75,
            OldGeneralTax = 5000.25,
            OldTotalTax = 6000.50,
            OldZoneNo = "1",
            OldPlotNo = "6",
            OldCSN = "98/440",
            OldPlotArea = 1000.50,
            OldAssessmentYear = 2025,
            OldFloor = "G+2",
            OldConstructionTypeOfUseId = "RCC",
            OldUseType = "Residential",
            OldConstructionArea = 500.25,
            OldOwnerName = "John Doe",
            OldOccupierName = "Jane Doe",
            OldAddress = "123 Main St",
            OldOwnerNameEnglish = "John Doe English",
            OldOccupierNameEnglish = "Jane Doe English",
            OldAddressEnglish = "123 Main St English",
            NoOfOldToilets = 2,
            OldTotalRooms = 5,
            OldSocietyName = "ABC Society",
            OldEmailId = "test@example.com",
            OldParkingAreaSqFt = 200.50,
            OldParkingAreaSqMtr = 18.58,
            OldAssessmentDate = assessmentDate,
            OldFlatOrShopNumber = "101",
            OldWing = "A",
            OldMobileNo = "1234567890",
            MarkedForDeletion = false,
            MarkedForDeletionDate = null,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now
        };

        // Assert - Test all 36 properties
        Assert.Equal(1, entity.Id);
        Assert.Equal("1", entity.OldWardNo);
        Assert.Equal("86", entity.OldPropertyNo);
        Assert.Equal("A", entity.OldPartitionNo);
        Assert.Equal("MM1", entity.OldEgovNo);
        Assert.Equal(1, entity.OldPropertyTypeId);
        Assert.Equal(50000.50, entity.OldALV);
        Assert.Equal(60000.75, entity.OldRV);
        Assert.Equal(5000.25, entity.OldGeneralTax);
        Assert.Equal(6000.50, entity.OldTotalTax);
        Assert.Equal("1", entity.OldZoneNo);
        Assert.Equal("6", entity.OldPlotNo);
        Assert.Equal("98/440", entity.OldCSN);
        Assert.Equal(1000.50, entity.OldPlotArea);
        Assert.Equal(2025, entity.OldAssessmentYear);
        Assert.Equal("G+2", entity.OldFloor);
        Assert.Equal("RCC", entity.OldConstructionTypeOfUseId);
        Assert.Equal("Residential", entity.OldUseType);
        Assert.Equal(500.25, entity.OldConstructionArea);
        Assert.Equal("John Doe", entity.OldOwnerName);
        Assert.Equal("Jane Doe", entity.OldOccupierName);
        Assert.Equal("123 Main St", entity.OldAddress);
        Assert.Equal("John Doe English", entity.OldOwnerNameEnglish);
        Assert.Equal("Jane Doe English", entity.OldOccupierNameEnglish);
        Assert.Equal("123 Main St English", entity.OldAddressEnglish);
        Assert.Equal(2, entity.NoOfOldToilets);
        Assert.Equal(5, entity.OldTotalRooms);
        Assert.Equal("ABC Society", entity.OldSocietyName);
        Assert.Equal("test@example.com", entity.OldEmailId);
        Assert.Equal(200.50, entity.OldParkingAreaSqFt);
        Assert.Equal(18.58, entity.OldParkingAreaSqMtr);
        Assert.Equal(assessmentDate, entity.OldAssessmentDate);
        Assert.Equal("101", entity.OldFlatOrShopNumber);
        Assert.Equal("A", entity.OldWing);
        Assert.Equal("1234567890", entity.OldMobileNo);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now, entity.UpdatedDate);
    }

    [Fact]
    public void PropertyMastOldEntity_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var entity = new PropertyMastOldEntity();

        // Assert
        Assert.Equal(0, entity.Id);
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
        Assert.Null(entity.OldFloor);
        Assert.Null(entity.OldConstructionTypeOfUseId);
        Assert.Null(entity.OldUseType);
        Assert.Null(entity.OldConstructionArea);
        Assert.Null(entity.OldOwnerName);
        Assert.Null(entity.OldOccupierName);
        Assert.Null(entity.OldAddress);
        Assert.Null(entity.OldOwnerNameEnglish);
        Assert.Null(entity.OldOccupierNameEnglish);
        Assert.Null(entity.OldAddressEnglish);
        Assert.Null(entity.NoOfOldToilets);
        Assert.Null(entity.OldTotalRooms);
        Assert.Null(entity.OldSocietyName);
        Assert.Null(entity.OldEmailId);
        Assert.Null(entity.OldParkingAreaSqFt);
        Assert.Null(entity.OldParkingAreaSqMtr);
        Assert.Null(entity.OldAssessmentDate);
        Assert.Null(entity.OldFlatOrShopNumber);
        Assert.Null(entity.OldWing);
        Assert.Null(entity.OldMobileNo);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    #endregion

    #region Entity Tests - TransMastOldEntity

    [Fact]
    public void TransMastOldEntity_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var now = DateTime.Now;
        var entity = new TransMastOldEntity
        {
            Id = 1,
            PropertyMastOldId = 100,
            FinanceYearId = 10,
            RVorCV = "RV",
            RVorCVValue = 50000.50m,
            TaxId = 5,
            TaxAmount = 5000.25m,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal(100, entity.PropertyMastOldId);
        Assert.Equal(10, entity.FinanceYearId);
        Assert.Equal("RV", entity.RVorCV);
        Assert.Equal(50000.50m, entity.RVorCVValue);
        Assert.Equal(5, entity.TaxId);
        Assert.Equal(5000.25m, entity.TaxAmount);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now, entity.UpdatedDate);
    }

    [Fact]
    public void TransMastOldEntity_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var entity = new TransMastOldEntity();

        // Assert
        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.PropertyMastOldId);
        Assert.Equal(0, entity.FinanceYearId);
        Assert.Null(entity.RVorCV);
        Assert.Equal(0m, entity.RVorCVValue);
        Assert.Equal(0, entity.TaxId);
        Assert.Equal(0m, entity.TaxAmount);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void TransMastOldEntity_MarkedForDeletion_CanBeSet()
    {
        // Arrange
        var entity = new TransMastOldEntity { PropertyMastOldId = 100, RVorCV = "RV" };
        var deletionDate = DateTime.Now;

        // Act
        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = deletionDate;

        // Assert
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    #endregion

    #region DTO Tests - PropertyDetailsOldDto

    [Fact]
    public void PropertyDetailsOldDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var now = DateTime.Now;
        var dto = new PropertyDetailsOldDto
        {
            Id = 1,
            PropertyId = 550722,
            OldFloorId = 5,
            FloorDescription = "Ground Floor",
            OldSubFloorId = 12,
            SubFloorDescription = "Basement",
            OldConstructionYear = "2015",
            ConstructionYearValue = 2015,
            OldAssessmentYear = "2020",
            AssessmentYearValue = 2020,
            OldConstructionTypeId = 2,
            ConstructionTypeDescription = "RCC",
            OldTypeOfUseId = 3,
            TypeOfUseDescription = "Residential",
            OldSubTypeOfUseId = 7,
            SubTypeOfUseDescription = "Apartment",
            OldCarpetAreaSqMeter = 111.48,
            OldCarpetAreaSqFeet = 1200.50,
            OldBuiltupAreaSqMeter = 130.50,
            OldBuiltupAreaSqFeet = 1400.75,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(550722, dto.PropertyId);
        Assert.Equal(5, dto.OldFloorId);
        Assert.Equal("Ground Floor", dto.FloorDescription);
        Assert.Equal(12, dto.OldSubFloorId);
        Assert.Equal("Basement", dto.SubFloorDescription);
        Assert.Equal("2015", dto.OldConstructionYear);
        Assert.Equal(2015, dto.ConstructionYearValue);
        Assert.Equal("2020", dto.OldAssessmentYear);
        Assert.Equal(2020, dto.AssessmentYearValue);
        Assert.Equal(2, dto.OldConstructionTypeId);
        Assert.Equal("RCC", dto.ConstructionTypeDescription);
        Assert.Equal(3, dto.OldTypeOfUseId);
        Assert.Equal("Residential", dto.TypeOfUseDescription);
        Assert.Equal(7, dto.OldSubTypeOfUseId);
        Assert.Equal("Apartment", dto.SubTypeOfUseDescription);
        Assert.Equal(111.48, dto.OldCarpetAreaSqMeter);
        Assert.Equal(1200.50, dto.OldCarpetAreaSqFeet);
        Assert.Equal(130.50, dto.OldBuiltupAreaSqMeter);
        Assert.Equal(1400.75, dto.OldBuiltupAreaSqFeet);
        Assert.False(dto.MarkedForDeletion);
        Assert.Null(dto.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyDetailsOldDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PropertyDetailsOldDto();

        // Assert
        Assert.Equal(0, dto.Id);
        Assert.Equal(0, dto.PropertyId);
        Assert.Null(dto.OldFloorId);
        Assert.Null(dto.FloorDescription);
        Assert.Null(dto.OldSubFloorId);
        Assert.Null(dto.SubFloorDescription);
        Assert.Null(dto.OldConstructionYear);
        Assert.Null(dto.ConstructionYearValue);
        Assert.Null(dto.OldAssessmentYear);
        Assert.Null(dto.AssessmentYearValue);
        Assert.Null(dto.OldConstructionTypeId);
        Assert.Null(dto.ConstructionTypeDescription);
        Assert.Null(dto.OldTypeOfUseId);
        Assert.Null(dto.TypeOfUseDescription);
        Assert.Null(dto.OldSubTypeOfUseId);
        Assert.Null(dto.SubTypeOfUseDescription);
        Assert.Null(dto.OldCarpetAreaSqMeter);
        Assert.Null(dto.OldCarpetAreaSqFeet);
        Assert.Null(dto.OldBuiltupAreaSqMeter);
        Assert.Null(dto.OldBuiltupAreaSqFeet);
        Assert.False(dto.MarkedForDeletion);
        Assert.Null(dto.MarkedForDeletionDate);
    }

    #endregion

    #region DTO Tests - PropertyDetailsOldListDto

    [Fact]
    public void PropertyDetailsOldListDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var floorDetails = new List<PropertyDetailsOldDto>
        {
            new() { Id = 1, PropertyId = 550722, OldFloorId = 5 },
            new() { Id = 2, PropertyId = 550722, OldFloorId = 6 }
        };

        var dto = new PropertyDetailsOldListDto
        {
            PropertyId = 550722,
            FloorDetails = floorDetails
        };

        // Assert
        Assert.Equal(550722, dto.PropertyId);
        Assert.NotNull(dto.FloorDetails);
        Assert.Equal(2, dto.FloorDetails.Count);
        Assert.Equal(1, dto.FloorDetails[0].Id);
        Assert.Equal(2, dto.FloorDetails[1].Id);
    }

    [Fact]
    public void PropertyDetailsOldListDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PropertyDetailsOldListDto();

        // Assert
        Assert.Equal(0, dto.PropertyId);
        Assert.NotNull(dto.FloorDetails);
        Assert.Empty(dto.FloorDetails);
    }

    #endregion

    #region DTO Tests - AddPropertyDetailsOldDto

    [Fact]
    public void AddPropertyDetailsOldDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new AddPropertyDetailsOldDto
        {
            OldFloorId = 5,
            OldSubFloorId = 12,
            OldConstructionYear = "2015",
            OldAssessmentYear = "2020",
            OldConstructionTypeId = 2,
            OldTypeOfUseId = 3,
            OldSubTypeOfUseId = 7,
            OldCarpetAreaSqMeter = 111.48,
            OldCarpetAreaSqFeet = 1200.50,
            OldBuiltupAreaSqMeter = 130.50,
            OldBuiltupAreaSqFeet = 1400.75
        };

        // Assert
        Assert.Equal(5, dto.OldFloorId);
        Assert.Equal(12, dto.OldSubFloorId);
        Assert.Equal("2015", dto.OldConstructionYear);
        Assert.Equal("2020", dto.OldAssessmentYear);
        Assert.Equal(2, dto.OldConstructionTypeId);
        Assert.Equal(3, dto.OldTypeOfUseId);
        Assert.Equal(7, dto.OldSubTypeOfUseId);
        Assert.Equal(111.48, dto.OldCarpetAreaSqMeter);
        Assert.Equal(1200.50, dto.OldCarpetAreaSqFeet);
        Assert.Equal(130.50, dto.OldBuiltupAreaSqMeter);
        Assert.Equal(1400.75, dto.OldBuiltupAreaSqFeet);
    }

    [Fact]
    public void AddPropertyDetailsOldDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new AddPropertyDetailsOldDto();

        // Assert
        Assert.Null(dto.OldFloorId);
        Assert.Null(dto.OldSubFloorId);
        Assert.Null(dto.OldConstructionYear);
        Assert.Null(dto.OldAssessmentYear);
        Assert.Null(dto.OldConstructionTypeId);
        Assert.Null(dto.OldTypeOfUseId);
        Assert.Null(dto.OldSubTypeOfUseId);
        Assert.Null(dto.OldCarpetAreaSqMeter);
        Assert.Null(dto.OldCarpetAreaSqFeet);
        Assert.Null(dto.OldBuiltupAreaSqMeter);
        Assert.Null(dto.OldBuiltupAreaSqFeet);
    }

    #endregion

    #region DTO Tests - UpdatePropertyDetailsOldDto

    [Fact]
    public void UpdatePropertyDetailsOldDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDetailsOldDto
        {
            OldFloorId = 5,
            OldSubFloorId = 12,
            OldConstructionYear = "2015",
            OldAssessmentYear = "2020",
            OldConstructionTypeId = 2,
            OldTypeOfUseId = 3,
            OldSubTypeOfUseId = 7,
            OldCarpetAreaSqMeter = 111.48,
            OldCarpetAreaSqFeet = 1200.50,
            OldBuiltupAreaSqMeter = 130.50,
            OldBuiltupAreaSqFeet = 1400.75
        };

        // Assert
        Assert.Equal(5, dto.OldFloorId);
        Assert.Equal(12, dto.OldSubFloorId);
        Assert.Equal("2015", dto.OldConstructionYear);
        Assert.Equal("2020", dto.OldAssessmentYear);
        Assert.Equal(2, dto.OldConstructionTypeId);
        Assert.Equal(3, dto.OldTypeOfUseId);
        Assert.Equal(7, dto.OldSubTypeOfUseId);
        Assert.Equal(111.48, dto.OldCarpetAreaSqMeter);
        Assert.Equal(1200.50, dto.OldCarpetAreaSqFeet);
        Assert.Equal(130.50, dto.OldBuiltupAreaSqMeter);
        Assert.Equal(1400.75, dto.OldBuiltupAreaSqFeet);
    }

    [Fact]
    public void UpdatePropertyDetailsOldDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDetailsOldDto();

        // Assert
        Assert.Null(dto.OldFloorId);
        Assert.Null(dto.OldSubFloorId);
        Assert.Null(dto.OldConstructionYear);
        Assert.Null(dto.OldAssessmentYear);
        Assert.Null(dto.OldConstructionTypeId);
        Assert.Null(dto.OldTypeOfUseId);
        Assert.Null(dto.OldSubTypeOfUseId);
        Assert.Null(dto.OldCarpetAreaSqMeter);
        Assert.Null(dto.OldCarpetAreaSqFeet);
        Assert.Null(dto.OldBuiltupAreaSqMeter);
        Assert.Null(dto.OldBuiltupAreaSqFeet);
    }

    #endregion

    #region DTO Tests - PropertyOldDetailsDto

    [Fact]
    public void PropertyOldDetailsDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new PropertyOldDetailsDto
        {
            PropertyId = 550722,
            OldWardNo = "1",
            OldPropertyNo = "86",
            OldPartitionNo = "A",
            OldEgovNo = "MM1",
            OldPlotArea = 1000.50,
            OldPlotNo = "6",
            OldRV = 60000.75,
            OldALV = 50000.50,
            OldTotalTax = 6000.50,
            OldZoneNo = "1",
            OldConstructionYear = "2015",
            OldCarpetAreaSqFeet = 1200.50,
            OldCarpetAreaSqMeter = 111.48,
            OldConstructionTypeId = 2,
            OldTypeOfUseId = 3,
            OldConstructionArea = 500.25,
            OldCSN = "98/440",
            OldGeneralTax = 5000.25
        };

        // Assert
        Assert.Equal(550722, dto.PropertyId);
        Assert.Equal("1", dto.OldWardNo);
        Assert.Equal("86", dto.OldPropertyNo);
        Assert.Equal("A", dto.OldPartitionNo);
        Assert.Equal("MM1", dto.OldEgovNo);
        Assert.Equal(1000.50, dto.OldPlotArea);
        Assert.Equal("6", dto.OldPlotNo);
        Assert.Equal(60000.75, dto.OldRV);
        Assert.Equal(50000.50, dto.OldALV);
        Assert.Equal(6000.50, dto.OldTotalTax);
        Assert.Equal("1", dto.OldZoneNo);
        Assert.Equal("2015", dto.OldConstructionYear);
        Assert.Equal(1200.50, dto.OldCarpetAreaSqFeet);
        Assert.Equal(111.48, dto.OldCarpetAreaSqMeter);
        Assert.Equal(2, dto.OldConstructionTypeId);
        Assert.Equal(3, dto.OldTypeOfUseId);
        Assert.Equal(500.25, dto.OldConstructionArea);
        Assert.Equal("98/440", dto.OldCSN);
        Assert.Equal(5000.25, dto.OldGeneralTax);
    }

    [Fact]
    public void PropertyOldDetailsDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PropertyOldDetailsDto();

        // Assert
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
        Assert.Null(dto.OldConstructionTypeId);
        Assert.Null(dto.OldTypeOfUseId);
        Assert.Null(dto.OldConstructionArea);
        Assert.Null(dto.OldCSN);
        Assert.Null(dto.OldGeneralTax);
    }

    #endregion

    #region DTO Tests - UpdatePropertyOldDetailsDto

    [Fact]
    public void UpdatePropertyOldDetailsDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new UpdatePropertyOldDetailsDto
        {
            OldWardNo = "1",
            OldFloorId = 5,
            OldPropertyNo = "86",
            OldPartitionNo = "A",
            OldEgovNo = "MM1",
            OldPlotArea = 1000.50,
            OldPlotNo = "6",
            OldRV = 60000.75,
            OldALV = 50000.50,
            OldTotalTax = 6000.50,
            OldZoneNo = "1",
            OldConstructionYear = "2015",
            OldCarpetAreaSqFeet = 1200.50,
            OldCarpetAreaSqMeter = 111.48,
            OldConstructionTypeId = 2,
            OldTypeOfUseId = 3,
            OldConstructionArea = 5600f,
            OldCSN = "98/440",
            OldGeneralTax = 500f
        };

        // Assert
        Assert.Equal("1", dto.OldWardNo);
        Assert.Equal(5, dto.OldFloorId);
        Assert.Equal("86", dto.OldPropertyNo);
        Assert.Equal("A", dto.OldPartitionNo);
        Assert.Equal("MM1", dto.OldEgovNo);
        Assert.Equal(1000.50, dto.OldPlotArea);
        Assert.Equal("6", dto.OldPlotNo);
        Assert.Equal(60000.75, dto.OldRV);
        Assert.Equal(50000.50, dto.OldALV);
        Assert.Equal(6000.50, dto.OldTotalTax);
        Assert.Equal("1", dto.OldZoneNo);
        Assert.Equal("2015", dto.OldConstructionYear);
        Assert.Equal(1200.50, dto.OldCarpetAreaSqFeet);
        Assert.Equal(111.48, dto.OldCarpetAreaSqMeter);
        Assert.Equal(2, dto.OldConstructionTypeId);
        Assert.Equal(3, dto.OldTypeOfUseId);
        Assert.Equal(5600f, dto.OldConstructionArea);
        Assert.Equal("98/440", dto.OldCSN);
        Assert.Equal(500f, dto.OldGeneralTax);
    }

    [Fact]
    public void UpdatePropertyOldDetailsDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new UpdatePropertyOldDetailsDto();

        // Assert
        Assert.Null(dto.OldWardNo);
        Assert.Null(dto.OldFloorId);
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
        Assert.Null(dto.OldConstructionTypeId);
        Assert.Null(dto.OldTypeOfUseId);
        Assert.Null(dto.OldConstructionArea);
        Assert.Null(dto.OldCSN);
        Assert.Null(dto.OldGeneralTax);
    }

    #endregion

    #region DTO Tests - PropertyOldTaxesDetailsDto

    [Fact]
    public void PropertyOldTaxesDetailsDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var taxYears = new List<OldTaxYearDto>
        {
            new() { FinanceYearId = 1, Year = 2020 },
            new() { FinanceYearId = 2, Year = 2021 }
        };

        var dto = new PropertyOldTaxesDetailsDto
        {
            PropertyId = 550722,
            TaxYears = taxYears
        };

        // Assert
        Assert.Equal(550722, dto.PropertyId);
        Assert.NotNull(dto.TaxYears);
        Assert.Equal(2, dto.TaxYears.Count);
        Assert.Equal(1, dto.TaxYears[0].FinanceYearId);
        Assert.Equal(2, dto.TaxYears[1].FinanceYearId);
    }

    [Fact]
    public void PropertyOldTaxesDetailsDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PropertyOldTaxesDetailsDto();

        // Assert
        Assert.Equal(0, dto.PropertyId);
        Assert.NotNull(dto.TaxYears);
        Assert.Empty(dto.TaxYears);
    }

    #endregion

    #region DTO Tests - OldTaxYearDto

    [Fact]
    public void OldTaxYearDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var taxes = new List<TaxDetailDto>
        {
            new() { TaxId = 1, TaxName = "Property Tax", TaxAmount = 1000.50m },
            new() { TaxId = 2, TaxName = "Water Tax", TaxAmount = 500.25m }
        };

        var dto = new OldTaxYearDto
        {
            FinanceYearId = 1,
            Year = 2020,
            YearCode = "2020-21",
            Taxes = taxes
        };

        // Assert
        Assert.Equal(1, dto.FinanceYearId);
        Assert.Equal(2020, dto.Year);
        Assert.Equal("2020-21", dto.YearCode);
        Assert.NotNull(dto.Taxes);
        Assert.Equal(2, dto.Taxes.Count);
    }

    [Fact]
    public void OldTaxYearDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new OldTaxYearDto();

        // Assert
        Assert.Equal(0, dto.FinanceYearId);
        Assert.Equal(0, dto.Year);
        Assert.Null(dto.YearCode);
        Assert.NotNull(dto.Taxes);
        Assert.Empty(dto.Taxes);
    }

    #endregion

    #region DTO Tests - UpdatePropertyOldTaxesDetailsDto

    [Fact]
    public void UpdatePropertyOldTaxesDetailsDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var taxYears = new List<UpdateOldTaxYearDto>
        {
            new() { FinanceYearId = 1 },
            new() { FinanceYearId = 2 }
        };

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = taxYears
        };

        // Assert
        Assert.NotNull(dto.TaxYears);
        Assert.Equal(2, dto.TaxYears.Count);
        Assert.Equal(1, dto.TaxYears[0].FinanceYearId);
        Assert.Equal(2, dto.TaxYears[1].FinanceYearId);
    }

    [Fact]
    public void UpdatePropertyOldTaxesDetailsDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new UpdatePropertyOldTaxesDetailsDto();

        // Assert
        Assert.NotNull(dto.TaxYears);
        Assert.Empty(dto.TaxYears);
    }

    #endregion

    #region DTO Tests - UpdateOldTaxYearDto

    [Fact]
    public void UpdateOldTaxYearDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var taxes = new List<UpdateTaxDetailDto>
        {
            new() { TaxId = 1, TaxAmount = 1000.50m },
            new() { TaxId = 2, TaxAmount = 500.25m }
        };

        var dto = new UpdateOldTaxYearDto
        {
            FinanceYearId = 1,
            Taxes = taxes
        };

        // Assert
        Assert.Equal(1, dto.FinanceYearId);
        Assert.NotNull(dto.Taxes);
        Assert.Equal(2, dto.Taxes.Count);
        Assert.Equal(1, dto.Taxes[0].TaxId);
        Assert.Equal(2, dto.Taxes[1].TaxId);
    }

    [Fact]
    public void UpdateOldTaxYearDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new UpdateOldTaxYearDto();

        // Assert
        Assert.Equal(0, dto.FinanceYearId);
        Assert.NotNull(dto.Taxes);
        Assert.Empty(dto.Taxes);
    }

    #endregion
}
