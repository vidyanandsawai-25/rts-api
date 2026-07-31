using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for SubUnitsDetailsSummaryDto, SubUnitsDetailsNamesDto, SubUnitsDetailsDto (read),
/// CreateSubUnitsDetailsDto and UpdateSubUnitsDetailsDto.
///
/// AssetId/FloorId/ConstructionTypeId/TypeOfUseId on the Create/Update DTOs are non-nullable ints
/// carrying both [Required] and [Range(1, int.MaxValue)] - since a struct can never be "missing",
/// [Required] never fires for these; only [Range] catches a 0 (or otherwise out-of-range) value,
/// so the "invalid" tests below assert the Range error key, not a Required one.
/// </summary>
public class SubUnitsDetailsDtoTests
{
    #region SubUnitsDetailsSummaryDto

    [Fact]
    public void SubUnitsDetailsSummaryDto_PropertiesGetAndSetCorrectly()
    {
        var floorDetails = new List<SubUnitsDetailsDto> { new() { Id = 1 } };
        var dto = new SubUnitsDetailsSummaryDto
        {
            FloorDetails = floorDetails,
            TotalBaseValue = 1000m,
            TotalCapitalValue = 2000m,
            TotalMarketValue = 3000m,
            TotalFloors = 4
        };

        Assert.Same(floorDetails, dto.FloorDetails);
        Assert.Equal(1000m, dto.TotalBaseValue);
        Assert.Equal(2000m, dto.TotalCapitalValue);
        Assert.Equal(3000m, dto.TotalMarketValue);
        Assert.Equal(4, dto.TotalFloors);
    }

    [Fact]
    public void SubUnitsDetailsSummaryDto_Defaults_FloorDetailsIsEmptyList_NumericFieldsAreZero()
    {
        var dto = new SubUnitsDetailsSummaryDto();

        Assert.NotNull(dto.FloorDetails);
        Assert.Empty(dto.FloorDetails);
        Assert.Equal(0m, dto.TotalBaseValue);
        Assert.Equal(0m, dto.TotalCapitalValue);
        Assert.Equal(0m, dto.TotalMarketValue);
        Assert.Equal(0, dto.TotalFloors);
    }

    #endregion

    #region SubUnitsDetailsNamesDto

    [Fact]
    public void SubUnitsDetailsNamesDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new SubUnitsDetailsNamesDto
        {
            AssetName = "Building A",
            FloorName = "Ground Floor",
            SubFloorName = "Mezzanine",
            ConstructionTypeName = "RCC",
            TypeOfUseName = "Commercial",
            SubTypeOfUseName = "Retail"
        };

        Assert.Equal("Building A", dto.AssetName);
        Assert.Equal("Ground Floor", dto.FloorName);
        Assert.Equal("Mezzanine", dto.SubFloorName);
        Assert.Equal("RCC", dto.ConstructionTypeName);
        Assert.Equal("Commercial", dto.TypeOfUseName);
        Assert.Equal("Retail", dto.SubTypeOfUseName);
    }

    [Fact]
    public void SubUnitsDetailsNamesDto_Defaults_AllPropertiesAreNull()
    {
        var dto = new SubUnitsDetailsNamesDto();

        Assert.Null(dto.AssetName);
        Assert.Null(dto.FloorName);
        Assert.Null(dto.SubFloorName);
        Assert.Null(dto.ConstructionTypeName);
        Assert.Null(dto.TypeOfUseName);
        Assert.Null(dto.SubTypeOfUseName);
    }

    #endregion

    #region SubUnitsDetailsDto (read)

    [Fact]
    public void SubUnitsDetailsDto_InheritsFromBaseDtos()
    {
        var dto = new SubUnitsDetailsDto();
        Assert.IsAssignableFrom<BaseDtos>(dto);
    }

    [Fact]
    public void SubUnitsDetailsDto_PropertiesGetAndSetCorrectly()
    {
        var createdDate = DateTime.UtcNow.AddDays(-10);
        var updatedDate = DateTime.UtcNow;
        var deletionDate = DateTime.UtcNow.AddDays(1);
        var names = new SubUnitsDetailsNamesDto { AssetName = "Building A" };
        var roomDetails = new List<RoomDetailDto> { new() { RoomNo = "R-1" } };

        var dto = new SubUnitsDetailsDto
        {
            Id = 1,
            IsActive = true,
            CreatedDate = createdDate,
            UpdatedDate = updatedDate,
            AssetId = 2,
            FloorId = 3,
            SubFloorId = 4,
            ConstructionYear = "2020",
            AssessmentYear = "2021",
            ConstructionTypeId = 5,
            TypeOfUseId = 6,
            SubTypeOfUseId = 7,
            CarpetAreaSqMeter = 10.5m,
            CarpetAreaSqFeet = 113m,
            BuiltUpAreaSqMeter = 12m,
            BuiltUpAreaSqFeet = 129m,
            NoOfRooms = 3,
            SubAssetCount = 2,
            CapitalValue = 500000m,
            BaseValue = 400000m,
            CVBaseRate = 1000m,
            CVAgeFactor = 0.9m,
            CVFloorFactor = 1.0m,
            CVNatureFactor = 1.1m,
            CVUseFactor = 1.2m,
            IsRented = true,
            CVCalculationFormula = "BaseRate * AgeFactor",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate,
            Names = names,
            RoomDetails = roomDetails
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(createdDate, dto.CreatedDate);
        Assert.Equal(updatedDate, dto.UpdatedDate);
        Assert.Equal(2, dto.AssetId);
        Assert.Equal(3, dto.FloorId);
        Assert.Equal(4, dto.SubFloorId);
        Assert.Equal("2020", dto.ConstructionYear);
        Assert.Equal("2021", dto.AssessmentYear);
        Assert.Equal(5, dto.ConstructionTypeId);
        Assert.Equal(6, dto.TypeOfUseId);
        Assert.Equal(7, dto.SubTypeOfUseId);
        Assert.Equal(10.5m, dto.CarpetAreaSqMeter);
        Assert.Equal(113m, dto.CarpetAreaSqFeet);
        Assert.Equal(12m, dto.BuiltUpAreaSqMeter);
        Assert.Equal(129m, dto.BuiltUpAreaSqFeet);
        Assert.Equal(3, dto.NoOfRooms);
        Assert.Equal(2, dto.SubAssetCount);
        Assert.Equal(500000m, dto.CapitalValue);
        Assert.Equal(400000m, dto.BaseValue);
        Assert.Equal(1000m, dto.CVBaseRate);
        Assert.Equal(0.9m, dto.CVAgeFactor);
        Assert.Equal(1.0m, dto.CVFloorFactor);
        Assert.Equal(1.1m, dto.CVNatureFactor);
        Assert.Equal(1.2m, dto.CVUseFactor);
        Assert.True(dto.IsRented);
        Assert.Equal("BaseRate * AgeFactor", dto.CVCalculationFormula);
        Assert.True(dto.MarkedForDeletion);
        Assert.Equal(deletionDate, dto.MarkedForDeletionDate);
        Assert.Same(names, dto.Names);
        Assert.Same(roomDetails, dto.RoomDetails);
    }

    [Fact]
    public void SubUnitsDetailsDto_Defaults_NamesIsInitialized_OptionalFieldsAreNull_MarkedForDeletionIsFalse()
    {
        var dto = new SubUnitsDetailsDto();

        Assert.NotNull(dto.Names);
        Assert.Null(dto.RoomDetails);
        Assert.Null(dto.SubFloorId);
        Assert.Null(dto.ConstructionYear);
        Assert.Null(dto.AssessmentYear);
        Assert.Null(dto.SubTypeOfUseId);
        Assert.Null(dto.CarpetAreaSqMeter);
        Assert.Null(dto.CarpetAreaSqFeet);
        Assert.Null(dto.BuiltUpAreaSqMeter);
        Assert.Null(dto.BuiltUpAreaSqFeet);
        Assert.Null(dto.NoOfRooms);
        Assert.Null(dto.CapitalValue);
        Assert.Null(dto.BaseValue);
        Assert.Null(dto.CVBaseRate);
        Assert.Null(dto.CVAgeFactor);
        Assert.Null(dto.CVFloorFactor);
        Assert.Null(dto.CVNatureFactor);
        Assert.Null(dto.CVUseFactor);
        Assert.Null(dto.IsRented);
        Assert.Null(dto.CVCalculationFormula);
        Assert.False(dto.MarkedForDeletion);
        Assert.Null(dto.MarkedForDeletionDate);
    }

    #endregion

    #region CreateSubUnitsDetailsDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateSubUnitsDetailsDto
        {
            AssetId = 1,
            FloorId = 2,
            SubFloorId = 3,
            ConstructionYear = "2020",
            AssessmentYear = "2021",
            ConstructionTypeId = 4,
            TypeOfUseId = 5,
            SubTypeOfUseId = 6,
            CarpetAreaSqMeter = 10m,
            CarpetAreaSqFeet = 100m,
            BuiltUpAreaSqMeter = 12m,
            BuiltUpAreaSqFeet = 120m,
            NoOfRooms = 3,
            CapitalValue = 500000m,
            BaseValue = 400000m,
            CVBaseRate = 1000m,
            CVAgeFactor = 0.9m,
            CVFloorFactor = 1.0m,
            CVNatureFactor = 1.1m,
            CVUseFactor = 1.2m,
            CVCalculationFormula = "BaseRate * AgeFactor",
            RoomDetails = new List<RoomDetailDto> { new() { RoomNo = "R-1" } }
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithAllOptionalFieldsNull_IsValid()
    {
        var dto = new CreateSubUnitsDetailsDto
        {
            AssetId = 1,
            FloorId = 2,
            ConstructionTypeId = 3,
            TypeOfUseId = 4
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithZeroAssetId_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 0, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.AssetId))
            && r.ErrorMessage == "AMS_SubUnitsDetails_AssetId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroFloorId_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 0, ConstructionTypeId = 3, TypeOfUseId = 4 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.FloorId))
            && r.ErrorMessage == "AMS_SubUnitsDetails_FloorId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroConstructionTypeId_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 0, TypeOfUseId = 4 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.ConstructionTypeId))
            && r.ErrorMessage == "AMS_SubUnitsDetails_ConstructionTypeId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroTypeOfUseId_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 0 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.TypeOfUseId))
            && r.ErrorMessage == "AMS_SubUnitsDetails_TypeOfUseId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroSubFloorId_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, SubFloorId = 0 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.SubFloorId))
            && r.ErrorMessage == "AMS_SubUnitsDetails_SubFloorId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroSubTypeOfUseId_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, SubTypeOfUseId = 0 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.SubTypeOfUseId))
            && r.ErrorMessage == "AMS_SubUnitsDetails_SubTypeOfUseId_InvalidRange");
    }

    [Fact]
    public void Create_WithConstructionYearExceeding4Characters_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, ConstructionYear = "20255" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.ConstructionYear))
            && r.ErrorMessage == "AMS_SubUnitsDetails_ConstructionYear_MaxLengthExceeded_4");
    }

    [Fact]
    public void Create_WithNonNumericConstructionYear_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, ConstructionYear = "abcd" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.ConstructionYear))
            && r.ErrorMessage == "AMS_SubUnitsDetails_ConstructionYear_Invalid");
    }

    [Fact]
    public void Create_WithAssessmentYearExceeding4Characters_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, AssessmentYear = "20255" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.AssessmentYear))
            && r.ErrorMessage == "AMS_SubUnitsDetails_AssessmentYear_MaxLengthExceeded_4");
    }

    [Fact]
    public void Create_WithNonNumericAssessmentYear_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, AssessmentYear = "abcd" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.AssessmentYear))
            && r.ErrorMessage == "AMS_SubUnitsDetails_AssessmentYear_Invalid");
    }

    [Fact]
    public void Create_WithNegativeCarpetAreaSqMeter_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CarpetAreaSqMeter = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.CarpetAreaSqMeter))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CarpetAreaSqMeter_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeCarpetAreaSqFeet_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CarpetAreaSqFeet = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.CarpetAreaSqFeet))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CarpetAreaSqFeet_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeBuiltUpAreaSqMeter_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, BuiltUpAreaSqMeter = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.BuiltUpAreaSqMeter))
            && r.ErrorMessage == "AMS_SubUnitsDetails_BuiltUpAreaSqMeter_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeBuiltUpAreaSqFeet_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, BuiltUpAreaSqFeet = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.BuiltUpAreaSqFeet))
            && r.ErrorMessage == "AMS_SubUnitsDetails_BuiltUpAreaSqFeet_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeNoOfRooms_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, NoOfRooms = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.NoOfRooms))
            && r.ErrorMessage == "AMS_SubUnitsDetails_NoOfRooms_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeCapitalValue_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CapitalValue = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.CapitalValue))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CapitalValue_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeBaseValue_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, BaseValue = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.BaseValue))
            && r.ErrorMessage == "AMS_SubUnitsDetails_BaseValue_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeCVBaseRate_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CVBaseRate = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.CVBaseRate))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CVBaseRate_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeCVAgeFactor_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CVAgeFactor = -0.1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.CVAgeFactor))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CVAgeFactor_InvalidRange");
    }

    [Fact]
    public void Create_WithCVAgeFactorExceeding9Point9999_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CVAgeFactor = 10m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.CVAgeFactor))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CVAgeFactor_InvalidRange");
    }

    [Fact]
    public void Create_WithCVFloorFactorExceeding9Point9999_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CVFloorFactor = 10m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.CVFloorFactor))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CVFloorFactor_InvalidRange");
    }

    [Fact]
    public void Create_WithCVNatureFactorExceeding9Point9999_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CVNatureFactor = 10m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.CVNatureFactor))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CVNatureFactor_InvalidRange");
    }

    [Fact]
    public void Create_WithCVUseFactorExceeding9Point9999_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CVUseFactor = 10m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.CVUseFactor))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CVUseFactor_InvalidRange");
    }

    [Fact]
    public void Create_WithCVCalculationFormulaExceeding500Characters_IsInvalid()
    {
        var dto = new CreateSubUnitsDetailsDto
        {
            AssetId = 1,
            FloorId = 2,
            ConstructionTypeId = 3,
            TypeOfUseId = 4,
            CVCalculationFormula = new string('F', 501)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubUnitsDetailsDto.CVCalculationFormula))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CVCalculationFormula_MaxLengthExceeded_500");
    }

    [Fact]
    public void Create_InheritsFromCreateBaseDtos()
    {
        var dto = new CreateSubUnitsDetailsDto();
        Assert.IsAssignableFrom<CreateBaseDtos>(dto);
    }

    [Fact]
    public void Create_Defaults_RoomDetailsIsNull_OptionalFieldsAreNull()
    {
        var dto = new CreateSubUnitsDetailsDto();

        Assert.Null(dto.RoomDetails);
        Assert.Null(dto.SubFloorId);
        Assert.Null(dto.ConstructionYear);
        Assert.Null(dto.AssessmentYear);
        Assert.Null(dto.SubTypeOfUseId);
        Assert.Null(dto.CarpetAreaSqMeter);
        Assert.Null(dto.CVCalculationFormula);
        Assert.Null(dto.CreatedBy);
    }

    #endregion

    #region UpdateSubUnitsDetailsDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateSubUnitsDetailsDto
        {
            AssetId = 1,
            FloorId = 2,
            ConstructionTypeId = 3,
            TypeOfUseId = 4,
            IsActive = true,
            MarkedForDeletion = false
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithZeroAssetId_IsInvalid()
    {
        var dto = new UpdateSubUnitsDetailsDto { AssetId = 0, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateSubUnitsDetailsDto.AssetId))
            && r.ErrorMessage == "AMS_SubUnitsDetails_AssetId_InvalidRange");
    }

    [Fact]
    public void Update_WithZeroFloorId_IsInvalid()
    {
        var dto = new UpdateSubUnitsDetailsDto { AssetId = 1, FloorId = 0, ConstructionTypeId = 3, TypeOfUseId = 4 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateSubUnitsDetailsDto.FloorId))
            && r.ErrorMessage == "AMS_SubUnitsDetails_FloorId_InvalidRange");
    }

    [Fact]
    public void Update_WithZeroConstructionTypeId_IsInvalid()
    {
        var dto = new UpdateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 0, TypeOfUseId = 4 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateSubUnitsDetailsDto.ConstructionTypeId))
            && r.ErrorMessage == "AMS_SubUnitsDetails_ConstructionTypeId_InvalidRange");
    }

    [Fact]
    public void Update_WithZeroTypeOfUseId_IsInvalid()
    {
        var dto = new UpdateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 0 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateSubUnitsDetailsDto.TypeOfUseId))
            && r.ErrorMessage == "AMS_SubUnitsDetails_TypeOfUseId_InvalidRange");
    }

    [Fact]
    public void Update_WithNonNumericConstructionYear_IsInvalid()
    {
        var dto = new UpdateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, ConstructionYear = "20XX" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateSubUnitsDetailsDto.ConstructionYear))
            && r.ErrorMessage == "AMS_SubUnitsDetails_ConstructionYear_Invalid");
    }

    [Fact]
    public void Update_WithAssessmentYearExceeding4Characters_IsInvalid()
    {
        var dto = new UpdateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, AssessmentYear = "20255" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateSubUnitsDetailsDto.AssessmentYear))
            && r.ErrorMessage == "AMS_SubUnitsDetails_AssessmentYear_MaxLengthExceeded_4");
    }

    [Fact]
    public void Update_WithNegativeCapitalValue_IsInvalid()
    {
        var dto = new UpdateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CapitalValue = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateSubUnitsDetailsDto.CapitalValue))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CapitalValue_InvalidRange");
    }

    [Fact]
    public void Update_WithCVUseFactorExceeding9Point9999_IsInvalid()
    {
        var dto = new UpdateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, CVUseFactor = 10m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateSubUnitsDetailsDto.CVUseFactor))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CVUseFactor_InvalidRange");
    }

    [Fact]
    public void Update_WithCVCalculationFormulaExceeding500Characters_IsInvalid()
    {
        var dto = new UpdateSubUnitsDetailsDto
        {
            AssetId = 1,
            FloorId = 2,
            ConstructionTypeId = 3,
            TypeOfUseId = 4,
            CVCalculationFormula = new string('F', 501)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateSubUnitsDetailsDto.CVCalculationFormula))
            && r.ErrorMessage == "AMS_SubUnitsDetails_CVCalculationFormula_MaxLengthExceeded_500");
    }

    [Fact]
    public void Update_WithIsActiveFalse_IsValid()
    {
        var dto = new UpdateSubUnitsDetailsDto { AssetId = 1, FloorId = 2, ConstructionTypeId = 3, TypeOfUseId = 4, IsActive = false };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_MarkedForDeletionAndDate_GetAndSetCorrectly()
    {
        var deletionDate = DateTime.UtcNow;
        var dto = new UpdateSubUnitsDetailsDto
        {
            AssetId = 1,
            FloorId = 2,
            ConstructionTypeId = 3,
            TypeOfUseId = 4,
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.True(dto.MarkedForDeletion);
        Assert.Equal(deletionDate, dto.MarkedForDeletionDate);
    }

    [Fact]
    public void Update_InheritsFromUpdateBaseDtos()
    {
        var dto = new UpdateSubUnitsDetailsDto();
        Assert.IsAssignableFrom<UpdateBaseDtos>(dto);
    }

    [Fact]
    public void Update_Defaults_OptionalFieldsAreNull_MarkedForDeletionIsFalse()
    {
        var dto = new UpdateSubUnitsDetailsDto();

        Assert.Null(dto.SubFloorId);
        Assert.Null(dto.ConstructionYear);
        Assert.Null(dto.AssessmentYear);
        Assert.Null(dto.SubTypeOfUseId);
        Assert.Null(dto.CVCalculationFormula);
        Assert.Null(dto.UpdatedBy);
        Assert.False(dto.MarkedForDeletion);
        Assert.Null(dto.MarkedForDeletionDate);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
