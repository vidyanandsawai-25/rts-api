using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Asset_Management.AssetRoomWiseMinusData;
using NtisPlatform.Application.DTOs.Queries;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for AssetRoomWiseMinusDataDto / CreateAssetRoomWiseMinusDataDto /
/// UpdateAssetRoomWiseMinusDataDto / AssetRoomWiseMinusDataQueryParameters.
/// </summary>
public class AssetRoomWiseMinusDataDtoTests
{
    #region AssetRoomWiseMinusDataDto (read)

    [Fact]
    public void AssetRoomWiseMinusDataDto_PropertiesGetAndSetCorrectly()
    {
        var deletionDate = DateTime.Now;
        var dto = new AssetRoomWiseMinusDataDto
        {
            Id = 1,
            IsActive = true,
            RoomWiseSubmissionId = 5,
            LengthMtr = 3.5,
            WidthMtr = 2.5,
            AreaSqMtr = 8.75,
            HeightMtr = 3.0,
            Shape = "Rectangle",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(5, dto.RoomWiseSubmissionId);
        Assert.Equal(3.5, dto.LengthMtr);
        Assert.Equal(2.5, dto.WidthMtr);
        Assert.Equal(8.75, dto.AreaSqMtr);
        Assert.Equal(3.0, dto.HeightMtr);
        Assert.Equal("Rectangle", dto.Shape);
        Assert.True(dto.MarkedForDeletion);
        Assert.Equal(deletionDate, dto.MarkedForDeletionDate);
    }

    [Fact]
    public void AssetRoomWiseMinusDataDto_Defaults_AreNull()
    {
        var dto = new AssetRoomWiseMinusDataDto();

        Assert.Null(dto.RoomWiseSubmissionId);
        Assert.Null(dto.LengthMtr);
        Assert.Null(dto.WidthMtr);
        Assert.Null(dto.AreaSqMtr);
        Assert.Null(dto.HeightMtr);
        Assert.Null(dto.Shape);
        Assert.False(dto.MarkedForDeletion);
        Assert.Null(dto.MarkedForDeletionDate);
    }

    #endregion

    #region CreateAssetRoomWiseMinusDataDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetRoomWiseMinusDataDto { RoomWiseSubmissionId = 1, LengthMtr = 3, WidthMtr = 2 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithAllFieldsNull_IsValid()
    {
        // Every field is genuinely optional at this level (entity-level checks apply elsewhere).
        var dto = new CreateAssetRoomWiseMinusDataDto();

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithNegativeLengthMtr_IsInvalid()
    {
        var dto = new CreateAssetRoomWiseMinusDataDto { LengthMtr = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomWiseMinusDataDto.LengthMtr))
            && r.ErrorMessage == "AMS_AssetRoomWiseMinusData_LengthMtr_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeWidthMtr_IsInvalid()
    {
        var dto = new CreateAssetRoomWiseMinusDataDto { WidthMtr = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomWiseMinusDataDto.WidthMtr))
            && r.ErrorMessage == "AMS_AssetRoomWiseMinusData_WidthMtr_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeAreaSqMtr_IsInvalid()
    {
        var dto = new CreateAssetRoomWiseMinusDataDto { AreaSqMtr = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomWiseMinusDataDto.AreaSqMtr))
            && r.ErrorMessage == "AMS_AssetRoomWiseMinusData_AreaSqMtr_InvalidRange");
    }

    [Fact]
    public void Create_WithShapeExceeding25Characters_IsInvalid()
    {
        var dto = new CreateAssetRoomWiseMinusDataDto { Shape = new string('S', 26) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomWiseMinusDataDto.Shape))
            && r.ErrorMessage == "AMS_AssetRoomWiseMinusData_Shape_MaxLengthExceeded_25");
    }

    #endregion

    #region UpdateAssetRoomWiseMinusDataDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetRoomWiseMinusDataDto { Id = 1, RoomWiseSubmissionId = 1 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithNegativeHeightMtr_IsInvalid()
    {
        var dto = new UpdateAssetRoomWiseMinusDataDto { Id = 1, HeightMtr = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetRoomWiseMinusDataDto.HeightMtr))
            && r.ErrorMessage == "AMS_AssetRoomWiseMinusData_HeightMtr_InvalidRange");
    }

    #endregion

    #region AssetRoomWiseMinusDataQueryParameters

    [Fact]
    public void QueryParameters_InheritsFromBaseQueryParameters()
    {
        var queryParameters = new AssetRoomWiseMinusDataQueryParameters();

        Assert.IsAssignableFrom<BaseQueryParameters>(queryParameters);
    }

    [Fact]
    public void QueryParameters_RoomWiseSubmissionId_HasFilterableAttribute()
    {
        var property = typeof(AssetRoomWiseMinusDataQueryParameters)
            .GetProperty(nameof(AssetRoomWiseMinusDataQueryParameters.RoomWiseSubmissionId));

        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();

        Assert.True(hasFilterable);
    }

    [Fact]
    public void QueryParameters_RoomWiseSubmissionId_CanBeSet()
    {
        var queryParameters = new AssetRoomWiseMinusDataQueryParameters { RoomWiseSubmissionId = 7 };

        Assert.Equal(7, queryParameters.RoomWiseSubmissionId);
    }

    [Fact]
    public void QueryParameters_Defaults_RoomWiseSubmissionIdIsNull()
    {
        var queryParameters = new AssetRoomWiseMinusDataQueryParameters();

        Assert.Null(queryParameters.RoomWiseSubmissionId);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
