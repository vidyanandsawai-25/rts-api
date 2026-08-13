using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for the DTOs in BulkGenerateAcrossFloorsDto.cs - bulk generation of child assets
/// (rooms/shops) across multiple floors in a single call.
/// </summary>
public class BulkGenerateAcrossFloorsDtoTests
{
    #region BulkGenerateAcrossFloorsDto

    [Fact]
    public void WithValidData_IsValid()
    {
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int> { 1, 2 },
            UnitsPerFloor = 5
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void WithZeroParentAssetId_IsInvalid()
    {
        // ParentAssetId is a non-nullable int, so [Required] can never fire (a struct is never
        // "missing"); omitting it just leaves the CLR default 0, which [Range(1, ...)] rejects.
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 0,
            Type = "Shop",
            FloorIds = new List<int> { 1 },
            UnitsPerFloor = 5
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateAcrossFloorsDto.ParentAssetId))
            && r.ErrorMessage == "AMS_BulkGenerateAcrossFloors_ParentAssetId_InvalidRange");
    }

    [Fact]
    public void WithEmptyType_IsInvalid()
    {
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = string.Empty,
            FloorIds = new List<int> { 1 },
            UnitsPerFloor = 5
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateAcrossFloorsDto.Type))
            && r.ErrorMessage == "AMS_BulkGenerateAcrossFloors_Type_Required");
    }

    [Fact]
    public void WithTypeExceeding50Characters_IsInvalid()
    {
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = new string('T', 51),
            FloorIds = new List<int> { 1 },
            UnitsPerFloor = 5
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateAcrossFloorsDto.Type))
            && r.ErrorMessage == "AMS_BulkGenerateAcrossFloors_Type_MaxLengthExceeded_50");
    }

    [Fact]
    public void WithEmptyFloorIds_IsInvalid()
    {
        // FloorIds is List<int> initialized to `new()` by default, so it's never null - [Required]
        // alone only rejects null references, not empty collections. [MinLength(1)] (added
        // alongside [Required], reusing the same error key) closes that gap: an empty floor
        // selection is now rejected as intended.
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int>(),
            UnitsPerFloor = 5
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateAcrossFloorsDto.FloorIds))
            && r.ErrorMessage == "AMS_BulkGenerateAcrossFloors_FloorIds_Required");
    }

    [Fact]
    public void WithNullFloorIds_IsInvalid()
    {
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = null!,
            UnitsPerFloor = 5
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateAcrossFloorsDto.FloorIds))
            && r.ErrorMessage == "AMS_BulkGenerateAcrossFloors_FloorIds_Required");
    }

    [Fact]
    public void WithOneFloorId_IsValid()
    {
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int> { 1 },
            UnitsPerFloor = 5
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void WithMultipleFloorIds_IsValid()
    {
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int> { 1, 2, 3 },
            UnitsPerFloor = 5
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void WithFloorIdsContainingZeroOrNegativeValues_CurrentlyPassesValidation()
    {
        // DataAnnotations here only enforces "at least one element" ([MinLength(1)]), not that
        // each element is a positive id - there is no existing per-element numeric-range
        // validation attribute/convention in this codebase to reuse (checked
        // Application/Attributes/ and the rest of DTOs/), so this is left to whatever
        // service/repository eventually resolves these ids (they'd simply match no floor and be
        // reported as a per-item generation failure, not a 400). Documenting current behavior
        // rather than inventing a new validation pattern without a confirmed business rule.
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int> { 0, -1, 2 },
            UnitsPerFloor = 5
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void WithZeroUnitsPerFloor_IsInvalid()
    {
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int> { 1 },
            UnitsPerFloor = 0
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateAcrossFloorsDto.UnitsPerFloor))
            && r.ErrorMessage == "AMS_BulkGenerateAcrossFloors_UnitsPerFloor_InvalidRange");
    }

    [Fact]
    public void WithUnitsPerFloorExceeding100_IsInvalid()
    {
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int> { 1 },
            UnitsPerFloor = 101
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateAcrossFloorsDto.UnitsPerFloor))
            && r.ErrorMessage == "AMS_BulkGenerateAcrossFloors_UnitsPerFloor_InvalidRange");
    }

    [Fact]
    public void WithConstructionYearExceeding4Characters_IsInvalid()
    {
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int> { 1 },
            UnitsPerFloor = 5,
            ConstructionYear = "20223"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BulkGenerateAcrossFloorsDto.ConstructionYear))
            && r.ErrorMessage == "AMS_BulkGenerateAcrossFloors_ConstructionYear_MaxLengthExceeded_4");
    }

    [Fact]
    public void Defaults_OptionalFieldsAreNull_FloorIdsIsEmptyList()
    {
        var dto = new BulkGenerateAcrossFloorsDto();

        Assert.NotNull(dto.FloorIds);
        Assert.Empty(dto.FloorIds);
        Assert.Null(dto.ConstructionYear);
        Assert.Null(dto.SubTypeOfUseId);
        Assert.Null(dto.CreatedBy);
        Assert.Equal(0, dto.ConstructionTypeId);
        Assert.Equal(0, dto.TypeOfUseId);
        Assert.Equal(string.Empty, dto.Type);
    }

    #endregion

    #region BulkGenerateAcrossFloorsResponseDto

    [Fact]
    public void BulkGenerateAcrossFloorsResponseDto_PropertiesGetAndSetCorrectly()
    {
        var generatedAssets = new List<GeneratedAssetDto> { new() { AssetId = 1 } };
        var errors = new List<string> { "Floor 3 not found" };

        var dto = new BulkGenerateAcrossFloorsResponseDto
        {
            TotalGenerated = 1,
            GeneratedAssets = generatedAssets,
            Errors = errors
        };

        Assert.Equal(1, dto.TotalGenerated);
        Assert.Same(generatedAssets, dto.GeneratedAssets);
        Assert.Same(errors, dto.Errors);
    }

    [Fact]
    public void BulkGenerateAcrossFloorsResponseDto_Defaults_CollectionsAreEmpty()
    {
        var dto = new BulkGenerateAcrossFloorsResponseDto();

        Assert.Equal(0, dto.TotalGenerated);
        Assert.NotNull(dto.GeneratedAssets);
        Assert.Empty(dto.GeneratedAssets);
        Assert.NotNull(dto.Errors);
        Assert.Empty(dto.Errors);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
