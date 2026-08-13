using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for the DTOs in BulkGenerateChildAssetsDto.cs - the simplified bulk-generate flow that
/// creates a sequential run of child assets (rooms/flats/shops) under one parent asset, optionally
/// pre-linked to a single floor.
/// </summary>
public class BulkGenerateChildAssetsDtoTests
{
    #region BulkGenerateChildAssetsDto

    [Fact]
    public void WithValidData_IsValid()
    {
        var dto = new BulkGenerateChildAssetsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            Count = 4
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void WithZeroParentAssetId_IsInvalid()
    {
        // ParentAssetId is a non-nullable int, so [Required] can never fire (a struct is never
        // "missing"); omitting it just leaves the CLR default 0, which [Range(1, ...)] rejects.
        var dto = new BulkGenerateChildAssetsDto
        {
            ParentAssetId = 0,
            Type = "Shop",
            Count = 4
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateChildAssetsDto.ParentAssetId))
            && r.ErrorMessage == "AMS_BulkGenerateChildAssets_ParentAssetId_InvalidRange");
    }

    [Fact]
    public void WithEmptyType_IsInvalid()
    {
        var dto = new BulkGenerateChildAssetsDto
        {
            ParentAssetId = 1,
            Type = string.Empty,
            Count = 4
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateChildAssetsDto.Type))
            && r.ErrorMessage == "AMS_BulkGenerateChildAssets_Type_Required");
    }

    [Fact]
    public void WithTypeExceeding50Characters_IsInvalid()
    {
        var dto = new BulkGenerateChildAssetsDto
        {
            ParentAssetId = 1,
            Type = new string('T', 51),
            Count = 4
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateChildAssetsDto.Type))
            && r.ErrorMessage == "AMS_BulkGenerateChildAssets_Type_MaxLengthExceeded_50");
    }

    [Fact]
    public void WithPrefixExceeding20Characters_IsInvalid()
    {
        var dto = new BulkGenerateChildAssetsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            Count = 4,
            Prefix = new string('P', 21)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateChildAssetsDto.Prefix))
            && r.ErrorMessage == "AMS_BulkGenerateChildAssets_Prefix_MaxLengthExceeded_20");
    }

    [Fact]
    public void WithZeroCount_IsInvalid()
    {
        var dto = new BulkGenerateChildAssetsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            Count = 0
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateChildAssetsDto.Count))
            && r.ErrorMessage == "AMS_BulkGenerateChildAssets_Count_InvalidRange");
    }

    [Fact]
    public void WithCountExceeding500_IsInvalid()
    {
        var dto = new BulkGenerateChildAssetsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            Count = 501
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateChildAssetsDto.Count))
            && r.ErrorMessage == "AMS_BulkGenerateChildAssets_Count_InvalidRange");
    }

    [Fact]
    public void Defaults_StartNumberIsOne_PrefixIsEmpty_AreaSqFtIsZero_FloorDetailsIdIsNull()
    {
        var dto = new BulkGenerateChildAssetsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            Count = 4
        };

        Assert.Equal(1, dto.StartNumber);
        Assert.Equal(string.Empty, dto.Prefix);
        Assert.Equal(0m, dto.AreaSqFt);
        Assert.Null(dto.FloorDetailsId);
        Assert.Null(dto.CreatedBy);
    }

    #endregion

    #region BulkGenerateChildAssetsResponseDto

    [Fact]
    public void BulkGenerateChildAssetsResponseDto_PropertiesGetAndSetCorrectly()
    {
        var generatedAssets = new List<GeneratedAssetDto> { new() { AssetId = 1 } };
        var errors = new List<string> { "Row 3 failed" };

        var dto = new BulkGenerateChildAssetsResponseDto
        {
            TotalGenerated = 3,
            GeneratedAssets = generatedAssets,
            Errors = errors
        };

        Assert.Equal(3, dto.TotalGenerated);
        Assert.Same(generatedAssets, dto.GeneratedAssets);
        Assert.Same(errors, dto.Errors);
    }

    [Fact]
    public void BulkGenerateChildAssetsResponseDto_Defaults_CollectionsAreEmpty()
    {
        var dto = new BulkGenerateChildAssetsResponseDto();

        Assert.Equal(0, dto.TotalGenerated);
        Assert.NotNull(dto.GeneratedAssets);
        Assert.Empty(dto.GeneratedAssets);
        Assert.NotNull(dto.Errors);
        Assert.Empty(dto.Errors);
    }

    #endregion

    #region GeneratedAssetDto

    [Fact]
    public void GeneratedAssetDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new GeneratedAssetDto
        {
            AssetId = 1,
            AssetNo = "AST-001",
            AssetName = "Shop 1",
            RoomWiseSubmissionDetailsId = 2
        };

        Assert.Equal(1, dto.AssetId);
        Assert.Equal("AST-001", dto.AssetNo);
        Assert.Equal("Shop 1", dto.AssetName);
        Assert.Equal(2, dto.RoomWiseSubmissionDetailsId);
    }

    [Fact]
    public void GeneratedAssetDto_Defaults_StringsAreEmpty_RoomWiseSubmissionDetailsIdIsNull()
    {
        var dto = new GeneratedAssetDto();

        Assert.Equal(string.Empty, dto.AssetNo);
        Assert.Equal(string.Empty, dto.AssetName);
        Assert.Null(dto.RoomWiseSubmissionDetailsId);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
