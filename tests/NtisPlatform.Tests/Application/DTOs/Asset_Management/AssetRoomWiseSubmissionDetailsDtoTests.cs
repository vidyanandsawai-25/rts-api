using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetRoomWiseSubmissionDetails;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for AssetRoomWiseSubmissionDetailsDto / CreateAssetRoomWiseSubmissionDetailsDto /
/// UpdateAssetRoomWiseSubmissionDetailsDto - room-wise details for child assets.
/// </summary>
public class AssetRoomWiseSubmissionDetailsDtoTests
{
    #region AssetRoomWiseSubmissionDetailsDto (read)

    [Fact]
    public void AssetRoomWiseSubmissionDetailsDto_PropertiesGetAndSetCorrectly()
    {
        var deletionDate = DateTime.UtcNow;
        var dto = new AssetRoomWiseSubmissionDetailsDto
        {
            Id = 1,
            IsActive = true,
            ParentAssetId = 10,
            AssetId = 20,
            FloorDetailsId = 30,
            LengthMtr = 4.0,
            WidthMtr = 3.0,
            AreaSqMtr = 12.0,
            HeightMtr = 3.2,
            TotalAreaSqMtr = 12.0,
            Shape = "Rectangle",
            RoomNo = "R-101",
            OuterYesNo = true,
            RoomType = "Bedroom",
            MinusYesNo = false,
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate,
            ParentAssetName = "Parent Building",
            AssetName = "Unit 101",
            FloorName = "First Floor"
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(10, dto.ParentAssetId);
        Assert.Equal(20, dto.AssetId);
        Assert.Equal(30, dto.FloorDetailsId);
        Assert.Equal(4.0, dto.LengthMtr);
        Assert.Equal(3.0, dto.WidthMtr);
        Assert.Equal(12.0, dto.AreaSqMtr);
        Assert.Equal(3.2, dto.HeightMtr);
        Assert.Equal(12.0, dto.TotalAreaSqMtr);
        Assert.Equal("Rectangle", dto.Shape);
        Assert.Equal("R-101", dto.RoomNo);
        Assert.True(dto.OuterYesNo);
        Assert.Equal("Bedroom", dto.RoomType);
        Assert.False(dto.MinusYesNo);
        Assert.True(dto.MarkedForDeletion);
        Assert.Equal(deletionDate, dto.MarkedForDeletionDate);
        Assert.Equal("Parent Building", dto.ParentAssetName);
        Assert.Equal("Unit 101", dto.AssetName);
        Assert.Equal("First Floor", dto.FloorName);
    }

    [Fact]
    public void AssetRoomWiseSubmissionDetailsDto_Defaults_BoolsAreFalse_NullablesAreNull()
    {
        var dto = new AssetRoomWiseSubmissionDetailsDto();

        Assert.False(dto.OuterYesNo);
        Assert.False(dto.MinusYesNo);
        Assert.False(dto.MarkedForDeletion);
        Assert.Null(dto.ParentAssetId);
        Assert.Null(dto.AssetId);
        Assert.Null(dto.FloorDetailsId);
        Assert.Null(dto.Shape);
        Assert.Null(dto.RoomNo);
        Assert.Null(dto.RoomType);
    }

    #endregion

    #region CreateAssetRoomWiseSubmissionDetailsDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetRoomWiseSubmissionDetailsDto { ParentAssetId = 1, AssetId = 2, FloorDetailsId = 3 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithZeroParentAssetId_IsInvalid()
    {
        var dto = new CreateAssetRoomWiseSubmissionDetailsDto { ParentAssetId = 0 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomWiseSubmissionDetailsDto.ParentAssetId))
            && r.ErrorMessage == "AMS_AssetRoomWiseSubmissionDetails_ParentAssetId_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeAreaSqMtr_IsInvalid()
    {
        var dto = new CreateAssetRoomWiseSubmissionDetailsDto { AreaSqMtr = -5 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomWiseSubmissionDetailsDto.AreaSqMtr))
            && r.ErrorMessage == "AMS_AssetRoomWiseSubmissionDetails_AreaSqMtr_InvalidRange");
    }

    [Fact]
    public void Create_WithRoomNoExceeding50Characters_IsInvalid()
    {
        var dto = new CreateAssetRoomWiseSubmissionDetailsDto { RoomNo = new string('R', 51) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomWiseSubmissionDetailsDto.RoomNo))
            && r.ErrorMessage == "AMS_AssetRoomWiseSubmissionDetails_RoomNo_MaxLengthExceeded_50");
    }

    [Fact]
    public void Create_WithRoomTypeExceeding50Characters_IsInvalid()
    {
        var dto = new CreateAssetRoomWiseSubmissionDetailsDto { RoomType = new string('T', 51) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomWiseSubmissionDetailsDto.RoomType))
            && r.ErrorMessage == "AMS_AssetRoomWiseSubmissionDetails_RoomType_MaxLengthExceeded_50");
    }

    [Fact]
    public void Create_BoolFlags_DefaultToFalse()
    {
        var dto = new CreateAssetRoomWiseSubmissionDetailsDto();

        Assert.False(dto.OuterYesNo);
        Assert.False(dto.MinusYesNo);
    }

    #endregion

    #region UpdateAssetRoomWiseSubmissionDetailsDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetRoomWiseSubmissionDetailsDto { ParentAssetId = 1 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithNegativeHeightMtr_IsInvalid()
    {
        var dto = new UpdateAssetRoomWiseSubmissionDetailsDto { HeightMtr = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetRoomWiseSubmissionDetailsDto.HeightMtr))
            && r.ErrorMessage == "AMS_AssetRoomWiseSubmissionDetails_HeightMtr_InvalidRange");
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
