using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.InventoryAssetDetail;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for InventoryAssetDetailDto / CreateInventoryAssetDetailDto / UpdateInventoryAssetDetailDto -
/// individual inventory unit details.
/// </summary>
public class InventoryAssetDetailDtoTests
{
    #region InventoryAssetDetailDto (read)

    [Fact]
    public void InventoryAssetDetailDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new InventoryAssetDetailDto
        {
            Id = 1,
            IsActive = true,
            AssetId = 10,
            BatchId = 20,
            UnitNumber = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemModelId = 4,
            InventoryItemConditionId = 5,
            OwningDepartmentId = 6,
            Specifications = "16GB RAM",
            PhotoFileId = "photo.jpg",
            UnitPurchaseValue = 500m,
            UnitCapitalValue = 400m,
            AssetName = "Chair Unit 1",
            AssetNo = "AST-001",
            CategoryName = "Furniture",
            ItemName = "Chair",
            ModelName = "Model X",
            ConditionName = "Good",
            DepartmentName = "IT"
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(10, dto.AssetId);
        Assert.Equal(20, dto.BatchId);
        Assert.Equal(1, dto.UnitNumber);
        Assert.Equal(2, dto.InventoryItemCategoryId);
        Assert.Equal(3, dto.InventoryItemNameId);
        Assert.Equal(4, dto.InventoryItemModelId);
        Assert.Equal(5, dto.InventoryItemConditionId);
        Assert.Equal(6, dto.OwningDepartmentId);
        Assert.Equal("16GB RAM", dto.Specifications);
        Assert.Equal("photo.jpg", dto.PhotoFileId);
        Assert.Equal(500m, dto.UnitPurchaseValue);
        Assert.Equal(400m, dto.UnitCapitalValue);
        Assert.Equal("Chair Unit 1", dto.AssetName);
        Assert.Equal("AST-001", dto.AssetNo);
        Assert.Equal("Furniture", dto.CategoryName);
        Assert.Equal("Chair", dto.ItemName);
        Assert.Equal("Model X", dto.ModelName);
        Assert.Equal("Good", dto.ConditionName);
        Assert.Equal("IT", dto.DepartmentName);
    }

    [Fact]
    public void InventoryAssetDetailDto_Defaults_OptionalFieldsAreNull()
    {
        var dto = new InventoryAssetDetailDto();

        Assert.Null(dto.InventoryItemCategoryId);
        Assert.Null(dto.InventoryItemNameId);
        Assert.Null(dto.InventoryItemModelId);
        Assert.Null(dto.InventoryItemConditionId);
        Assert.Null(dto.OwningDepartmentId);
        Assert.Null(dto.Specifications);
        Assert.Null(dto.PhotoFileId);
        Assert.Null(dto.UnitCapitalValue);
    }

    #endregion

    #region CreateInventoryAssetDetailDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateInventoryAssetDetailDto { AssetId = 1, BatchId = 2, UnitNumber = 1, UnitPurchaseValue = 100m };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithZeroAssetId_IsInvalid()
    {
        var dto = new CreateInventoryAssetDetailDto { AssetId = 0, BatchId = 2, UnitNumber = 1, UnitPurchaseValue = 100m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryAssetDetailDto.AssetId))
            && r.ErrorMessage == "AMS_InventoryAssetDetail_AssetId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroBatchId_IsInvalid()
    {
        var dto = new CreateInventoryAssetDetailDto { AssetId = 1, BatchId = 0, UnitNumber = 1, UnitPurchaseValue = 100m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryAssetDetailDto.BatchId))
            && r.ErrorMessage == "AMS_InventoryAssetDetail_BatchId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroUnitNumber_IsInvalid()
    {
        var dto = new CreateInventoryAssetDetailDto { AssetId = 1, BatchId = 2, UnitNumber = 0, UnitPurchaseValue = 100m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryAssetDetailDto.UnitNumber))
            && r.ErrorMessage == "AMS_InventoryAssetDetail_UnitNumber_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroUnitPurchaseValue_IsInvalid()
    {
        // UnitPurchaseValue is a non-nullable decimal, so [Required] can't fail here; the
        // 0-default is caught by [Range(0.01, ...)] instead.
        var dto = new CreateInventoryAssetDetailDto { AssetId = 1, BatchId = 2, UnitNumber = 1, UnitPurchaseValue = 0m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryAssetDetailDto.UnitPurchaseValue))
            && r.ErrorMessage == "AMS_InventoryAssetDetail_UnitPurchaseValue_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeInventoryItemConditionId_IsInvalid()
    {
        var dto = new CreateInventoryAssetDetailDto
        {
            AssetId = 1,
            BatchId = 2,
            UnitNumber = 1,
            UnitPurchaseValue = 100m,
            InventoryItemConditionId = -1
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryAssetDetailDto.InventoryItemConditionId))
            && r.ErrorMessage == "AMS_InventoryAssetDetail_InventoryItemConditionId_InvalidRange");
    }

    [Fact]
    public void Create_WithSpecificationsExceeding500Characters_IsInvalid()
    {
        var dto = new CreateInventoryAssetDetailDto
        {
            AssetId = 1,
            BatchId = 2,
            UnitNumber = 1,
            UnitPurchaseValue = 100m,
            Specifications = new string('S', 501)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryAssetDetailDto.Specifications))
            && r.ErrorMessage == "AMS_InventoryAssetDetail_Specifications_MaxLengthExceeded_500");
    }

    #endregion

    #region UpdateInventoryAssetDetailDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateInventoryAssetDetailDto { UnitNumber = 1, UnitPurchaseValue = 100m };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithZeroUnitNumber_IsInvalid()
    {
        var dto = new UpdateInventoryAssetDetailDto { UnitNumber = 0, UnitPurchaseValue = 100m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateInventoryAssetDetailDto.UnitNumber))
            && r.ErrorMessage == "AMS_InventoryAssetDetail_UnitNumber_InvalidRange");
    }

    [Fact]
    public void Update_WithNegativeUnitCapitalValue_IsInvalid()
    {
        var dto = new UpdateInventoryAssetDetailDto { UnitNumber = 1, UnitPurchaseValue = 100m, UnitCapitalValue = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateInventoryAssetDetailDto.UnitCapitalValue))
            && r.ErrorMessage == "AMS_InventoryAssetDetail_UnitCapitalValue_InvalidRange");
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
