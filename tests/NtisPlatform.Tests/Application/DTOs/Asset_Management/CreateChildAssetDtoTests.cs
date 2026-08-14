using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for the DTOs in CreateChildAssetDto.cs - the full "create a single child asset (room/
/// shop) with complete details" flow, including its nested Rent Information, Floor Configuration,
/// and Room-wise Configuration/Valuation sections, plus the read-side response shapes used by the
/// paired GET endpoint.
/// </summary>
public class CreateChildAssetDtoTests
{
    #region CreateChildAssetDto

    [Fact]
    public void WithValidData_IsValid()
    {
        var dto = new CreateChildAssetDto { ParentAssetId = 1 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void WithZeroParentAssetId_IsInvalid()
    {
        // ParentAssetId is a non-nullable int, so [Required] can never fire (a struct is never
        // "missing"); omitting it just leaves the CLR default 0, which [Range(1, ...)] rejects.
        var dto = new CreateChildAssetDto { ParentAssetId = 0 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateChildAssetDto.ParentAssetId))
            && r.ErrorMessage == "AMS_ChildAsset_ParentAssetId_InvalidRange");
    }

    [Fact]
    public void WithComplexNameExceeding200Characters_IsInvalid()
    {
        var dto = new CreateChildAssetDto { ParentAssetId = 1, ComplexName = new string('C', 201) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateChildAssetDto.ComplexName))
            && r.ErrorMessage == "AMS_ChildAsset_ComplexName_MaxLengthExceeded_200");
    }

    [Fact]
    public void WithMobileNoExceeding15Characters_IsInvalid()
    {
        var dto = new CreateChildAssetDto { ParentAssetId = 1, MobileNo = new string('9', 16) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateChildAssetDto.MobileNo))
            && r.ErrorMessage == "AMS_ChildAsset_MobileNo_MaxLengthExceeded_15");
    }

    [Fact]
    public void WithInvalidEmailId_IsInvalid()
    {
        var dto = new CreateChildAssetDto { ParentAssetId = 1, EmailId = "not-an-email" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateChildAssetDto.EmailId))
            && r.ErrorMessage == "AMS_ChildAsset_EmailId_Invalid");
    }

    [Fact]
    public void WithNegativeTotalAreaSqFt_IsInvalid()
    {
        var dto = new CreateChildAssetDto { ParentAssetId = 1, TotalAreaSqFt = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateChildAssetDto.TotalAreaSqFt))
            && r.ErrorMessage == "AMS_ChildAsset_TotalAreaSqFt_InvalidRange");
    }

    [Fact]
    public void Defaults_NestedSectionsAreNull_IsRoomWiseValuationActiveIsFalse()
    {
        var dto = new CreateChildAssetDto { ParentAssetId = 1 };

        Assert.Null(dto.RentInformation);
        Assert.Null(dto.FloorConfiguration);
        Assert.False(dto.IsRoomWiseValuationActive);
        Assert.Null(dto.RoomDetails);
        Assert.Null(dto.PhotoFiles);
        Assert.Null(dto.PhotoMetadataJson);
        Assert.Null(dto.FloorId);
        Assert.Null(dto.FloorDetailsId);
    }

    [Fact]
    public void NestedRentInformationAndFloorConfigurationAndRoomDetails_CanBeAssignedAndRetrieved()
    {
        var rentInformation = new RentInformationDto { RentAmount = 5000m };
        var floorConfiguration = new FloorConfigurationDto { UnitAreaSqFt = 500m };
        var offsets = new List<RoomOffsetDto> { new() { Id = 1, Shape = "Rectangle" } };
        var roomDetails = new List<RoomDetailDto> { new() { RoomNo = "R-1", Offsets = offsets } };

        var dto = new CreateChildAssetDto
        {
            ParentAssetId = 1,
            RentInformation = rentInformation,
            FloorConfiguration = floorConfiguration,
            IsRoomWiseValuationActive = true,
            RoomDetails = roomDetails
        };

        Assert.Same(rentInformation, dto.RentInformation);
        Assert.Same(floorConfiguration, dto.FloorConfiguration);
        Assert.True(dto.IsRoomWiseValuationActive);
        Assert.Same(roomDetails, dto.RoomDetails);
        Assert.Same(offsets, dto.RoomDetails![0].Offsets);
    }

    #endregion

    #region RentInformationDto

    [Fact]
    public void RentInformation_WithValidData_IsValid()
    {
        var dto = new RentInformationDto
        {
            LeaseRentType = "Monthly",
            LeaseStart = DateTime.Now,
            LeaseEnd = DateTime.Now.AddYears(1),
            Duration = 12,
            RentFrequency = "Monthly",
            RentAmount = 5000m,
            SecurityDeposit = 10000m,
            DepositType = "Refundable"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void RentInformation_WithNegativeDuration_IsInvalid()
    {
        var dto = new RentInformationDto { Duration = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RentInformationDto.Duration))
            && r.ErrorMessage == "AMS_RentInformation_Duration_InvalidRange");
    }

    [Fact]
    public void RentInformation_WithNegativeRentAmount_IsInvalid()
    {
        var dto = new RentInformationDto { RentAmount = -100m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RentInformationDto.RentAmount))
            && r.ErrorMessage == "AMS_RentInformation_RentAmount_InvalidRange");
    }

    [Fact]
    public void RentInformation_WithNegativeSecurityDeposit_IsInvalid()
    {
        var dto = new RentInformationDto { SecurityDeposit = -100m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RentInformationDto.SecurityDeposit))
            && r.ErrorMessage == "AMS_RentInformation_SecurityDeposit_InvalidRange");
    }

    [Fact]
    public void RentInformation_WithLeaseRentTypeExceeding100Characters_IsInvalid()
    {
        var dto = new RentInformationDto { LeaseRentType = new string('L', 101) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RentInformationDto.LeaseRentType))
            && r.ErrorMessage == "AMS_RentInformation_LeaseRentType_MaxLengthExceeded_100");
    }

    [Fact]
    public void RentInformation_Defaults_AllFieldsAreNull()
    {
        var dto = new RentInformationDto();

        Assert.Null(dto.LeaseRentType);
        Assert.Null(dto.LeaseStart);
        Assert.Null(dto.LeaseEnd);
        Assert.Null(dto.Duration);
        Assert.Null(dto.RentFrequency);
        Assert.Null(dto.RentAmount);
        Assert.Null(dto.SecurityDeposit);
        Assert.Null(dto.DepositType);
    }

    #endregion

    #region FloorConfigurationDto

    [Fact]
    public void FloorConfiguration_WithValidData_IsValid()
    {
        var dto = new FloorConfigurationDto { UnitAreaSqFt = 500m, CalculatedCapitalValue = 100000m };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void FloorConfiguration_WithNegativeUnitAreaSqFt_IsInvalid()
    {
        var dto = new FloorConfigurationDto { UnitAreaSqFt = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(FloorConfigurationDto.UnitAreaSqFt))
            && r.ErrorMessage == "AMS_FloorConfiguration_UnitAreaSqFt_InvalidRange");
    }

    [Fact]
    public void FloorConfiguration_WithNegativeCalculatedCapitalValue_IsInvalid()
    {
        var dto = new FloorConfigurationDto { CalculatedCapitalValue = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(FloorConfigurationDto.CalculatedCapitalValue))
            && r.ErrorMessage == "AMS_FloorConfiguration_CalculatedCapitalValue_InvalidRange");
    }

    [Fact]
    public void FloorConfiguration_Defaults_AllFieldsAreNull()
    {
        var dto = new FloorConfigurationDto();

        Assert.Null(dto.UnitAreaSqFt);
        Assert.Null(dto.CalculatedCapitalValue);
    }

    #endregion

    #region RoomOffsetDto (no DataAnnotations)

    [Fact]
    public void RoomOffsetDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new RoomOffsetDto
        {
            Id = 1,
            Shape = "Triangle",
            Length = 3.5,
            Width = 2.0,
            Height = 2.8,
            Base1 = 1.5,
            Base2 = 2.5,
            Radius = 0.5,
            AreaSqM = 4.2,
            Op = "add"
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("Triangle", dto.Shape);
        Assert.Equal(3.5, dto.Length);
        Assert.Equal(2.0, dto.Width);
        Assert.Equal(2.8, dto.Height);
        Assert.Equal(1.5, dto.Base1);
        Assert.Equal(2.5, dto.Base2);
        Assert.Equal(0.5, dto.Radius);
        Assert.Equal(4.2, dto.AreaSqM);
        Assert.Equal("add", dto.Op);
    }

    [Fact]
    public void RoomOffsetDto_Defaults_AllNullableFieldsAreNull()
    {
        var dto = new RoomOffsetDto();

        Assert.Equal(0, dto.Id);
        Assert.Null(dto.Shape);
        Assert.Null(dto.Length);
        Assert.Null(dto.Width);
        Assert.Null(dto.Height);
        Assert.Null(dto.AreaSqM);
        Assert.Null(dto.Op);
    }

    #endregion

    #region RoomDetailDto

    [Fact]
    public void RoomDetail_WithValidData_IsValid()
    {
        var dto = new RoomDetailDto { RoomNo = "R-1", RoomType = "Bedroom", Shape = "Rectangle", SubmissionType = "Manual" };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void RoomDetail_WithRoomNoExceeding50Characters_IsInvalid()
    {
        var dto = new RoomDetailDto { RoomNo = new string('R', 51) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RoomDetailDto.RoomNo))
            && r.ErrorMessage == "AMS_RoomDetail_RoomNo_MaxLengthExceeded_50");
    }

    [Fact]
    public void RoomDetail_WithRoomTypeExceeding100Characters_IsInvalid()
    {
        var dto = new RoomDetailDto { RoomType = new string('T', 101) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RoomDetailDto.RoomType))
            && r.ErrorMessage == "AMS_RoomDetail_RoomType_MaxLengthExceeded_100");
    }

    [Fact]
    public void RoomDetail_WithShapeExceeding50Characters_IsInvalid()
    {
        var dto = new RoomDetailDto { Shape = new string('S', 51) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RoomDetailDto.Shape))
            && r.ErrorMessage == "AMS_RoomDetail_Shape_MaxLengthExceeded_50");
    }

    [Fact]
    public void RoomDetail_Defaults_OffsetsIsNull_BoolFlagsAreFalse()
    {
        var dto = new RoomDetailDto();

        Assert.Null(dto.Offsets);
        Assert.False(dto.OuterYesNo);
        Assert.False(dto.MinusYesNo);
        Assert.Null(dto.NoOfRooms);
        Assert.Null(dto.LengthMtr);
    }

    [Fact]
    public void RoomDetail_OffsetsList_CanBeAssignedAndRetrieved()
    {
        var offsets = new List<RoomOffsetDto> { new() { Id = 1 }, new() { Id = 2 } };

        var dto = new RoomDetailDto { Offsets = offsets };

        Assert.Same(offsets, dto.Offsets);
        Assert.Equal(2, dto.Offsets!.Count);
    }

    #endregion

    #region CreateChildAssetResponseDto (no DataAnnotations)

    [Fact]
    public void CreateChildAssetResponseDto_PropertiesGetAndSetCorrectly()
    {
        var errors = new List<string> { "Some error" };
        var dto = new CreateChildAssetResponseDto
        {
            Success = true,
            Message = "Created",
            AssetId = 1,
            AssetNo = "AST-001",
            RoomWiseSubmissionDetailsId = 2,
            RenterDetailsId = 3,
            SubUnitsDetailsId = 4,
            Errors = errors
        };

        Assert.True(dto.Success);
        Assert.Equal("Created", dto.Message);
        Assert.Equal(1, dto.AssetId);
        Assert.Equal("AST-001", dto.AssetNo);
        Assert.Equal(2, dto.RoomWiseSubmissionDetailsId);
        Assert.Equal(3, dto.RenterDetailsId);
        Assert.Equal(4, dto.SubUnitsDetailsId);
        Assert.Same(errors, dto.Errors);
    }

    [Fact]
    public void CreateChildAssetResponseDto_Defaults_MessageIsEmpty_ErrorsIsEmptyList()
    {
        var dto = new CreateChildAssetResponseDto();

        Assert.False(dto.Success);
        Assert.Equal(string.Empty, dto.Message);
        Assert.Null(dto.AssetId);
        Assert.NotNull(dto.Errors);
        Assert.Empty(dto.Errors);
    }

    #endregion

    #region GetChildAssetResponseDto (no DataAnnotations)

    [Fact]
    public void GetChildAssetResponseDto_PropertiesGetAndSetCorrectly()
    {
        var renterDetails = new RenterDetailsDto { Id = 1 };
        var roomWiseDetails = new List<RoomWiseDetailsDto> { new() { Id = 2 } };

        var dto = new GetChildAssetResponseDto
        {
            Success = true,
            Message = "Found",
            AssetId = 10,
            RenterDetails = renterDetails,
            RoomWiseDetails = roomWiseDetails
        };

        Assert.True(dto.Success);
        Assert.Equal("Found", dto.Message);
        Assert.Equal(10, dto.AssetId);
        Assert.Same(renterDetails, dto.RenterDetails);
        Assert.Same(roomWiseDetails, dto.RoomWiseDetails);
    }

    [Fact]
    public void GetChildAssetResponseDto_Defaults_RenterDetailsAndRoomWiseDetailsAreNull()
    {
        var dto = new GetChildAssetResponseDto();

        Assert.False(dto.Success);
        Assert.Equal(string.Empty, dto.Message);
        Assert.Equal(0, dto.AssetId);
        Assert.Null(dto.RenterDetails);
        Assert.Null(dto.RoomWiseDetails);
    }

    #endregion

    #region RenterDetailsDto (no DataAnnotations)

    [Fact]
    public void RenterDetailsDto_PropertiesGetAndSetCorrectly()
    {
        var fromDate = DateTime.Now;
        var toDate = DateTime.Now.AddYears(1);
        var dto = new RenterDetailsDto
        {
            Id = 1,
            FloorDetailsId = 2,
            RoomWiseSubmissionDetailsId = 3,
            AssetId = 4,
            RenterName = "John Doe",
            GSTNo = "GST123",
            TotalAreaSqFt = 500m,
            AadhaarCardNo = "1234-5678-9012",
            PANCardNo = "ABCDE1234F",
            MobileNo = "9999999999",
            EmailId = "john@example.com",
            LeaseRentType = "Monthly",
            FromDate = fromDate,
            ToDate = toDate,
            Duration = 12,
            RentFrequency = "Monthly",
            RentAmount = 5000m,
            SecurityDeposit = 10000m,
            DepositType = "Refundable",
            AgreementId = "AGR-1",
            IncrementFrequency = "Yearly",
            IncrementType = "Percentage",
            IncrementValue = 5.0,
            IncrementMethod = "Compound"
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.FloorDetailsId);
        Assert.Equal(3, dto.RoomWiseSubmissionDetailsId);
        Assert.Equal(4, dto.AssetId);
        Assert.Equal("John Doe", dto.RenterName);
        Assert.Equal("GST123", dto.GSTNo);
        Assert.Equal(500m, dto.TotalAreaSqFt);
        Assert.Equal("1234-5678-9012", dto.AadhaarCardNo);
        Assert.Equal("ABCDE1234F", dto.PANCardNo);
        Assert.Equal("9999999999", dto.MobileNo);
        Assert.Equal("john@example.com", dto.EmailId);
        Assert.Equal("Monthly", dto.LeaseRentType);
        Assert.Equal(fromDate, dto.FromDate);
        Assert.Equal(toDate, dto.ToDate);
        Assert.Equal(12, dto.Duration);
        Assert.Equal("Monthly", dto.RentFrequency);
        Assert.Equal(5000m, dto.RentAmount);
        Assert.Equal(10000m, dto.SecurityDeposit);
        Assert.Equal("Refundable", dto.DepositType);
        Assert.Equal("AGR-1", dto.AgreementId);
        Assert.Equal("Yearly", dto.IncrementFrequency);
        Assert.Equal("Percentage", dto.IncrementType);
        Assert.Equal(5.0, dto.IncrementValue);
        Assert.Equal("Compound", dto.IncrementMethod);
    }

    [Fact]
    public void RenterDetailsDto_Defaults_AllOptionalFieldsAreNull()
    {
        var dto = new RenterDetailsDto();

        Assert.Null(dto.RoomWiseSubmissionDetailsId);
        Assert.Null(dto.RenterName);
        Assert.Null(dto.GSTNo);
        Assert.Null(dto.TotalAreaSqFt);
        Assert.Null(dto.FromDate);
        Assert.Null(dto.ToDate);
        Assert.Null(dto.RentAmount);
    }

    #endregion

    #region RoomWiseDetailsDto (no DataAnnotations)

    [Fact]
    public void RoomWiseDetailsDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new RoomWiseDetailsDto
        {
            Id = 1,
            AssetId = 2,
            FloorDetailsId = 3,
            RoomNo = "R-1",
            RoomType = "Bedroom",
            Shape = "Rectangle",
            LengthMtr = 4.5,
            WidthMtr = 3.5,
            HeightMtr = 2.8,
            AreaSqMtr = 15.75,
            TotalAreaSqMtr = 15.75,
            OuterYesNo = true,
            MinusYesNo = false
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.AssetId);
        Assert.Equal(3, dto.FloorDetailsId);
        Assert.Equal("R-1", dto.RoomNo);
        Assert.Equal("Bedroom", dto.RoomType);
        Assert.Equal("Rectangle", dto.Shape);
        Assert.Equal(4.5, dto.LengthMtr);
        Assert.Equal(3.5, dto.WidthMtr);
        Assert.Equal(2.8, dto.HeightMtr);
        Assert.Equal(15.75, dto.AreaSqMtr);
        Assert.Equal(15.75, dto.TotalAreaSqMtr);
        Assert.True(dto.OuterYesNo);
        Assert.False(dto.MinusYesNo);
    }

    [Fact]
    public void RoomWiseDetailsDto_Defaults_NullableFieldsAreNull_BoolFlagsAreFalse()
    {
        var dto = new RoomWiseDetailsDto();

        Assert.Null(dto.AssetId);
        Assert.Null(dto.FloorDetailsId);
        Assert.Null(dto.RoomNo);
        Assert.False(dto.OuterYesNo);
        Assert.False(dto.MinusYesNo);
    }

    #endregion

    #region SubUnitResponseDto (no DataAnnotations)

    [Fact]
    public void SubUnitResponseDto_PropertiesGetAndSetCorrectly()
    {
        var createdDate = DateTime.Now;
        var dto = new SubUnitResponseDto
        {
            Id = 1,
            ParentAssetId = 2,
            AssetId = 3,
            ComplexName = "Complex A",
            ShopUnitName = "Shop 1",
            UnitNo = "U-1",
            TotalAreaSqFt = 500m,
            CalculatedCapitalValue = 100000m,
            CreatedDate = createdDate,
            FloorDetailsId = 4,
            UnitType = "Shop"
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.ParentAssetId);
        Assert.Equal(3, dto.AssetId);
        Assert.Equal("Complex A", dto.ComplexName);
        Assert.Equal("Shop 1", dto.ShopUnitName);
        Assert.Equal("U-1", dto.UnitNo);
        Assert.Equal(500m, dto.TotalAreaSqFt);
        Assert.Equal(100000m, dto.CalculatedCapitalValue);
        Assert.Equal(createdDate, dto.CreatedDate);
        Assert.Equal(4, dto.FloorDetailsId);
        Assert.Equal("Shop", dto.UnitType);
    }

    [Fact]
    public void SubUnitResponseDto_Defaults_OptionalFieldsAreNull()
    {
        var dto = new SubUnitResponseDto();

        Assert.Null(dto.ComplexName);
        Assert.Null(dto.ShopUnitName);
        Assert.Null(dto.UnitNo);
        Assert.Null(dto.TotalAreaSqFt);
        Assert.Null(dto.CalculatedCapitalValue);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.FloorDetailsId);
        Assert.Null(dto.UnitType);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
