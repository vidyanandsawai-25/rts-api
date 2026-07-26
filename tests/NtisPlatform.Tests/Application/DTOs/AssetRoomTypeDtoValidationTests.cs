using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Master.AssetRoomType;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// DataAnnotations validation tests for AssetRoomType Create/Update DTOs.
/// AssetTypeId was converted from a plain int (a dead [Required] — a non-nullable int defaults
/// to 0, which [Required] never treats as missing) to int? so [Required] can actually detect an
/// omitted value. RoomTypeCode intentionally has no [Required]: the AMS.AssetRoomTypeMaster
/// column is nullable and AssetRoomTypeService only runs the duplicate-code check when a code is
/// supplied, so requiring it here would contradict the DB schema and service behavior.
/// </summary>
public class AssetRoomTypeDtoValidationTests
{
    #region CreateAssetRoomTypeDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR" };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMissingAssetTypeId_IsInvalid()
    {
        var dto = new CreateAssetRoomTypeDto { RoomTypeName = "Bedroom" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomTypeDto.AssetTypeId))
            && r.ErrorMessage == "AssetRoomType_AssetTypeId_Required");
    }

    [Fact]
    public void Create_WithZeroAssetTypeId_IsInvalid()
    {
        // This is the exact scenario the old `[Required] public int AssetTypeId` silently missed:
        // an omitted field binds to 0, and [Required] alone never flags a non-nullable value type.
        var dto = new CreateAssetRoomTypeDto { AssetTypeId = 0, RoomTypeName = "Bedroom" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomTypeDto.AssetTypeId))
            && r.ErrorMessage == "AssetRoomType_AssetTypeId_Invalid");
    }

    [Fact]
    public void Create_WithNegativeAssetTypeId_IsInvalid()
    {
        var dto = new CreateAssetRoomTypeDto { AssetTypeId = -1, RoomTypeName = "Bedroom" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomTypeDto.AssetTypeId))
            && r.ErrorMessage == "AssetRoomType_AssetTypeId_Invalid");
    }

    [Fact]
    public void Create_WithEmptyRoomTypeName_IsInvalid()
    {
        var dto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = string.Empty };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomTypeDto.RoomTypeName))
            && r.ErrorMessage == "AssetRoomType_RoomTypeName_Required");
    }

    [Fact]
    public void Create_WithWhitespaceOnlyRoomTypeName_IsInvalid()
    {
        var dto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "   " };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomTypeDto.RoomTypeName))
            && r.ErrorMessage == "AssetRoomType_RoomTypeName_Required");
    }

    [Fact]
    public void Create_WithRoomTypeNameExceeding100Characters_IsInvalid()
    {
        var dto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = new string('A', 101) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomTypeDto.RoomTypeName))
            && r.ErrorMessage == "AssetRoomType_RoomTypeName_MaxLengthExceeded_100");
    }

    [Fact]
    public void Create_WithoutRoomTypeCode_IsValid()
    {
        // RoomTypeCode is genuinely optional per DB schema and service behavior.
        var dto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = null };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithRoomTypeCodeExceeding20Characters_IsInvalid()
    {
        var dto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = new string('B', 21) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomTypeDto.RoomTypeCode))
            && r.ErrorMessage == "AssetRoomType_RoomTypeCode_MaxLengthExceeded_20");
    }

    [Fact]
    public void Create_WithoutAssetCategoryId_IsValid()
    {
        // AssetCategoryId is genuinely optional — nullable DB column, service skips the FK check
        // entirely when it's not supplied.
        var dto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", AssetCategoryId = null };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMultipleInvalidFields_ReturnsMultipleErrors()
    {
        var dto = new CreateAssetRoomTypeDto { AssetTypeId = 0, RoomTypeName = string.Empty };

        var results = ValidateModel(dto);

        Assert.True(results.Count >= 2);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomTypeDto.AssetTypeId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetRoomTypeDto.RoomTypeName)));
    }

    #endregion

    #region UpdateAssetRoomTypeDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Master Bedroom", IsActive = true };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithMissingAssetTypeId_IsInvalid()
    {
        var dto = new UpdateAssetRoomTypeDto { RoomTypeName = "Master Bedroom" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetRoomTypeDto.AssetTypeId))
            && r.ErrorMessage == "AssetRoomType_AssetTypeId_Required");
    }

    [Fact]
    public void Update_WithIsActiveFalse_IsValid()
    {
        var dto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Master Bedroom", IsActive = false };

        Assert.Empty(ValidateModel(dto));
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
