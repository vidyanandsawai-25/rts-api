using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.InventoryAsset;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for the DTOs in InventoryAssetDtos.cs - the live inventory batch registration /
/// CV-display DTOs actually wired up by InventoryBatchMappingProfile (as opposed to the
/// unrelated, unused DTOs of the same names in DTOs/Asset_Management/InventoryBatch/InventoryBatchDto.cs,
/// which no CreateMap or service references - see InventoryBatchDtoTests.cs for that dead-code set).
/// </summary>
public class InventoryAssetDtosTests
{
    #region CreateInventoryBatchDto

    [Fact]
    public void CreateInventoryBatchDto_WithValidData_IsValid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            UnitValue = 10m
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void CreateInventoryBatchDto_WithZeroParentAssetId_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 0,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            UnitValue = 10m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.ParentAssetId))
            && r.ErrorMessage == "AMS_InventoryBatch_ParentAssetId_InvalidRange");
    }

    [Fact]
    public void CreateInventoryBatchDto_WithZeroInventoryItemConditionId_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 0,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            UnitValue = 10m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.InventoryItemConditionId))
            && r.ErrorMessage == "AMS_InventoryBatch_InventoryItemConditionId_InvalidRange");
    }

    [Fact]
    public void CreateInventoryBatchDto_WithZeroQuantity_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 0,
            UnitValue = 10m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.Quantity))
            && r.ErrorMessage == "AMS_InventoryBatch_Quantity_InvalidRange");
    }

    [Fact]
    public void CreateInventoryBatchDto_WithQuantityExceeding10000_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 10001,
            UnitValue = 10m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.Quantity))
            && r.ErrorMessage == "AMS_InventoryBatch_Quantity_InvalidRange");
    }

    [Fact]
    public void CreateInventoryBatchDto_WithZeroUnitValue_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            UnitValue = 0m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.UnitValue))
            && r.ErrorMessage == "AMS_InventoryBatch_UnitValue_InvalidRange");
    }

    [Fact]
    public void CreateInventoryBatchDto_WithDefaultPurchaseDate_PassesAttributeValidation_DespiteRequiredAttribute()
    {
        // PurchaseDate is a non-nullable DateTime, so [Required] can never fail for it (a struct
        // is never "missing") - DateTime.MinValue passes DataAnnotations validation here even
        // though the field is documented as required. A real omitted-date guard would need
        // [Required] on a nullable DateTime?, or a custom IValidatableObject check.
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            Quantity = 1,
            UnitValue = 10m
        };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(default, dto.PurchaseDate);
    }

    [Fact]
    public void CreateInventoryBatchDto_Defaults_UnitsIsEmptyList_ModelIdAndOwningDepartmentAreNull()
    {
        var dto = new CreateInventoryBatchDto();

        Assert.NotNull(dto.Units);
        Assert.Empty(dto.Units);
        Assert.Null(dto.InventoryItemModelId);
        Assert.Null(dto.OwningDepartmentId);
        Assert.Null(dto.DocumentFiles);
    }

    [Fact]
    public void CreateInventoryBatchDto_SpecificationsExceeding500Characters_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            UnitValue = 10m,
            Specifications = new string('S', 501)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.Specifications))
            && r.ErrorMessage == "AMS_InventoryBatch_Specifications_MaxLengthExceeded_500");
    }

    #endregion

    #region RegisterInventoryUnitDto

    [Fact]
    public void RegisterInventoryUnitDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new RegisterInventoryUnitDto { UnitNumber = 3, InventoryItemConditionId = 5, ConditionFactor = 0.8m };

        Assert.Equal(3, dto.UnitNumber);
        Assert.Equal(5, dto.InventoryItemConditionId);
        Assert.Equal(0.8m, dto.ConditionFactor);
    }

    [Fact]
    public void RegisterInventoryUnitDto_Defaults_ConditionOverridesAreNull()
    {
        var dto = new RegisterInventoryUnitDto();

        Assert.Null(dto.InventoryItemConditionId);
        Assert.Null(dto.ConditionFactor);
    }

    [Fact]
    public void RegisterInventoryUnitDto_WithConditionFactorAboveOne_IsInvalid()
    {
        var dto = new RegisterInventoryUnitDto { UnitNumber = 1, ConditionFactor = 1.5m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterInventoryUnitDto.ConditionFactor))
            && r.ErrorMessage == "AMS_RegisterInventoryUnit_ConditionFactor_InvalidRange");
    }

    [Fact]
    public void RegisterInventoryUnitDto_WithNegativeConditionFactor_IsInvalid()
    {
        var dto = new RegisterInventoryUnitDto { UnitNumber = 1, ConditionFactor = -0.1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterInventoryUnitDto.ConditionFactor))
            && r.ErrorMessage == "AMS_RegisterInventoryUnit_ConditionFactor_InvalidRange");
    }

    [Fact]
    public void RegisterInventoryUnitDto_WithUnitNumberExceeding10000_IsInvalid()
    {
        var dto = new RegisterInventoryUnitDto { UnitNumber = 10001 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterInventoryUnitDto.UnitNumber))
            && r.ErrorMessage == "AMS_RegisterInventoryUnit_UnitNumber_InvalidRange");
    }

    #endregion

    #region UpdateInventoryBatchDto

    [Fact]
    public void UpdateInventoryBatchDto_WithValidData_IsValid()
    {
        var dto = new UpdateInventoryBatchDto { BatchId = 1, InventoryItemConditionId = 2 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void UpdateInventoryBatchDto_WithoutBatchId_PassesAttributeValidation_DespiteRequiredAttribute()
    {
        // BatchId is a non-nullable int with only [Required] (no [Range]), so omitting it (0)
        // produces zero DataAnnotations errors - [Required] never triggers on a value type.
        // The controller/route-bound id is what actually guards this in practice.
        var dto = new UpdateInventoryBatchDto();

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(0, dto.BatchId);
    }

    [Fact]
    public void UpdateInventoryBatchDto_WithZeroUnitValue_IsInvalid()
    {
        var dto = new UpdateInventoryBatchDto { BatchId = 1, UnitValue = 0m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateInventoryBatchDto.UnitValue))
            && r.ErrorMessage == "AMS_InventoryBatch_UnitValue_InvalidRange");
    }

    [Fact]
    public void UpdateInventoryBatchDto_Defaults_AllOptionalFieldsAreNull()
    {
        var dto = new UpdateInventoryBatchDto { BatchId = 1 };

        Assert.Null(dto.InventoryItemNameId);
        Assert.Null(dto.InventoryItemModelId);
        Assert.Null(dto.Specifications);
        Assert.Null(dto.PurchaseDate);
        Assert.Null(dto.InventoryItemConditionId);
        Assert.Null(dto.UnitValue);
        Assert.Null(dto.InvoiceNumber);
        Assert.Null(dto.InvoiceDate);
        Assert.Null(dto.InvoiceFileName);
        Assert.Null(dto.OwningDepartmentId);
        Assert.Null(dto.PhotoFileName);
    }

    [Fact]
    public void UpdateInventoryBatchDto_InvoiceFileNameExceeding300Characters_IsInvalid()
    {
        var dto = new UpdateInventoryBatchDto { BatchId = 1, InvoiceFileName = new string('F', 301) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateInventoryBatchDto.InvoiceFileName))
            && r.ErrorMessage == "AMS_InventoryBatch_InvoiceFileName_MaxLengthExceeded_300");
    }

    #endregion

    #region Response / display DTOs (no DataAnnotations - property round-trip + defaults only)

    [Fact]
    public void InventoryLookupNamesDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new InventoryLookupNamesDto
        {
            InventoryType = "Furniture",
            Label = "Furniture Items",
            ItemName = "Chair",
            ModelBrand = "Godrej",
            Condition = "Good",
            OwningDepartment = "IT"
        };

        Assert.Equal("Furniture", dto.InventoryType);
        Assert.Equal("Furniture Items", dto.Label);
        Assert.Equal("Chair", dto.ItemName);
        Assert.Equal("Godrej", dto.ModelBrand);
        Assert.Equal("Good", dto.Condition);
        Assert.Equal("IT", dto.OwningDepartment);
    }

    [Fact]
    public void InventoryLookupNamesDto_Defaults_StringsAreEmpty_OptionalFieldsAreNull()
    {
        var dto = new InventoryLookupNamesDto();

        Assert.Equal(string.Empty, dto.InventoryType);
        Assert.Equal(string.Empty, dto.Label);
        Assert.Equal(string.Empty, dto.ItemName);
        Assert.Equal(string.Empty, dto.ModelBrand);
        Assert.Null(dto.Condition);
        Assert.Null(dto.OwningDepartment);
    }

    [Fact]
    public void InventoryBatchDto_PropertiesGetAndSetCorrectly()
    {
        var names = new InventoryLookupNamesDto { InventoryType = "IT" };
        var units = new List<InventoryUnitResponseDto> { new() { AssetId = 1 } };
        var dto = new InventoryBatchDto
        {
            BatchId = 1,
            ParentAssetId = 2,
            Quantity = 3,
            UnitValue = 100m,
            TotalBatchValue = 300m,
            TotalCapitalValue = 250m,
            TotalDepreciation = 50m,
            InvoiceNumber = "INV-1",
            Message = "Created",
            Names = names,
            Units = units
        };

        Assert.Equal(1, dto.BatchId);
        Assert.Equal(2, dto.ParentAssetId);
        Assert.Equal(3, dto.Quantity);
        Assert.Equal(100m, dto.UnitValue);
        Assert.Equal(300m, dto.TotalBatchValue);
        Assert.Equal(250m, dto.TotalCapitalValue);
        Assert.Equal(50m, dto.TotalDepreciation);
        Assert.Equal("INV-1", dto.InvoiceNumber);
        Assert.Equal("Created", dto.Message);
        Assert.Same(names, dto.Names);
        Assert.Same(units, dto.Units);
    }

    [Fact]
    public void InventoryBatchDto_Defaults_NamesAndUnitsAreInitialized_NotNull()
    {
        var dto = new InventoryBatchDto();

        Assert.NotNull(dto.Names);
        Assert.NotNull(dto.Units);
        Assert.Empty(dto.Units);
        Assert.Null(dto.InvoiceNumber);
        Assert.Null(dto.Message);
    }

    [Fact]
    public void InventoryUnitResponseDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new InventoryUnitResponseDto
        {
            AssetId = 1,
            AssetNo = "AST-001",
            AssetName = "Chair Unit 1",
            UnitNumber = 1,
            Condition = "Good",
            UnitPurchaseValue = 500m,
            UnitCapitalValue = 400m,
            DepreciationRate = 0.1m,
            ConditionFactor = 0.9m,
            AgeInYears = 2,
            CVFormula = "Value * Factor"
        };

        Assert.Equal(1, dto.AssetId);
        Assert.Equal("AST-001", dto.AssetNo);
        Assert.Equal("Chair Unit 1", dto.AssetName);
        Assert.Equal(1, dto.UnitNumber);
        Assert.Equal("Good", dto.Condition);
        Assert.Equal(500m, dto.UnitPurchaseValue);
        Assert.Equal(400m, dto.UnitCapitalValue);
        Assert.Equal(0.1m, dto.DepreciationRate);
        Assert.Equal(0.9m, dto.ConditionFactor);
        Assert.Equal(2, dto.AgeInYears);
        Assert.Equal("Value * Factor", dto.CVFormula);
    }

    [Fact]
    public void InventoryUnitResponseDto_Defaults_NamesAreEmptyString_OptionalFieldsAreNull()
    {
        var dto = new InventoryUnitResponseDto();

        Assert.Equal(string.Empty, dto.AssetNo);
        Assert.Equal(string.Empty, dto.AssetName);
        Assert.Null(dto.Condition);
        Assert.Null(dto.UnitCapitalValue);
        Assert.Null(dto.DepreciationRate);
        Assert.Null(dto.ConditionFactor);
        Assert.Null(dto.CVFormula);
    }

    [Fact]
    public void InventoryCategoryGroupDto_PropertiesGetAndSetCorrectly_AndBatchesDefaultsToEmpty()
    {
        var dto = new InventoryCategoryGroupDto
        {
            InventoryType = "Furniture",
            Label = "Furniture Items",
            TotalBatches = 2,
            TotalUnits = 10,
            TotalPurchaseValue = 1000m,
            TotalCapitalValue = 800m,
            TotalDepreciation = 200m,
            DepreciationPercent = 20m
        };

        Assert.Equal("Furniture", dto.InventoryType);
        Assert.Equal(2, dto.TotalBatches);
        Assert.Equal(10, dto.TotalUnits);
        Assert.Equal(1000m, dto.TotalPurchaseValue);
        Assert.Equal(800m, dto.TotalCapitalValue);
        Assert.Equal(200m, dto.TotalDepreciation);
        Assert.Equal(20m, dto.DepreciationPercent);
        Assert.NotNull(dto.Batches);
        Assert.Empty(dto.Batches);
    }

    [Fact]
    public void InventoryCVResponseDto_PropertiesGetAndSetCorrectly_AndCollectionsDefaultToEmpty()
    {
        var dto = new InventoryCVResponseDto
        {
            ParentAssetId = 1,
            ParentAssetName = "Building A",
            TotalBatches = 3,
            TotalUnitsRegistered = 15,
            TotalFailed = 1,
            GrandPurchaseValue = 5000m,
            GrandCapitalValue = 4000m,
            GrandDepreciation = 1000m
        };

        Assert.Equal(1, dto.ParentAssetId);
        Assert.Equal("Building A", dto.ParentAssetName);
        Assert.Equal(3, dto.TotalBatches);
        Assert.Equal(15, dto.TotalUnitsRegistered);
        Assert.Equal(1, dto.TotalFailed);
        Assert.Equal(5000m, dto.GrandPurchaseValue);
        Assert.Equal(4000m, dto.GrandCapitalValue);
        Assert.Equal(1000m, dto.GrandDepreciation);
        Assert.NotNull(dto.CategoryGroups);
        Assert.Empty(dto.CategoryGroups);
        Assert.NotNull(dto.FailedBatches);
        Assert.Empty(dto.FailedBatches);
    }

    [Fact]
    public void DepreciationRateDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new DepreciationRateDto
        {
            CategoryId = 1,
            TypeCode = "FURN",
            TypeName = "Furniture",
            DepreciationRate = 0.1m
        };

        Assert.Equal(1, dto.CategoryId);
        Assert.Equal("FURN", dto.TypeCode);
        Assert.Equal("Furniture", dto.TypeName);
        Assert.Equal(0.1m, dto.DepreciationRate);
    }

    [Fact]
    public void InventoryRatesResponseDto_Defaults_DepreciationRatesIsEmptyList()
    {
        var dto = new InventoryRatesResponseDto();

        Assert.NotNull(dto.DepreciationRates);
        Assert.Empty(dto.DepreciationRates);
    }

    [Fact]
    public void InventoryBatchListResponseDto_PropertiesGetAndSetCorrectly_AndBatchesDefaultsToEmpty()
    {
        var dto = new InventoryBatchListResponseDto
        {
            ParentAssetId = 1,
            ParentAssetName = "Building A",
            TotalBatches = 2,
            TotalUnits = 8,
            TotalPurchaseValue = 2000m,
            TotalCapitalValue = 1600m
        };

        Assert.Equal(1, dto.ParentAssetId);
        Assert.Equal("Building A", dto.ParentAssetName);
        Assert.Equal(2, dto.TotalBatches);
        Assert.Equal(8, dto.TotalUnits);
        Assert.Equal(2000m, dto.TotalPurchaseValue);
        Assert.Equal(1600m, dto.TotalCapitalValue);
        Assert.NotNull(dto.Batches);
        Assert.Empty(dto.Batches);
    }

    [Fact]
    public void InventoryBatchDetailDto_PropertiesGetAndSetCorrectly()
    {
        var purchaseDate = DateTime.UtcNow;
        var createdDate = DateTime.UtcNow.AddDays(-5);
        var dto = new InventoryBatchDetailDto
        {
            BatchId = 1,
            ParentAssetId = 2,
            Specifications = "16GB RAM",
            PurchaseDate = purchaseDate,
            Quantity = 5,
            UnitValue = 100m,
            TotalBatchValue = 500m,
            TotalBatchCV = 400m,
            InvoiceNumber = "INV-1",
            InvoiceDate = purchaseDate,
            InvoiceFileName = "invoice.pdf",
            PhotoFileName = "photo.jpg",
            CreatedDate = createdDate
        };

        Assert.Equal(1, dto.BatchId);
        Assert.Equal(2, dto.ParentAssetId);
        Assert.Equal("16GB RAM", dto.Specifications);
        Assert.Equal(purchaseDate, dto.PurchaseDate);
        Assert.Equal(5, dto.Quantity);
        Assert.Equal(100m, dto.UnitValue);
        Assert.Equal(500m, dto.TotalBatchValue);
        Assert.Equal(400m, dto.TotalBatchCV);
        Assert.Equal("INV-1", dto.InvoiceNumber);
        Assert.Equal(purchaseDate, dto.InvoiceDate);
        Assert.Equal("invoice.pdf", dto.InvoiceFileName);
        Assert.Equal("photo.jpg", dto.PhotoFileName);
        Assert.Equal(createdDate, dto.CreatedDate);
    }

    [Fact]
    public void InventoryBatchDetailDto_Defaults_NamesUnitsAndDocumentsAreInitialized_NotNull()
    {
        var dto = new InventoryBatchDetailDto();

        Assert.NotNull(dto.Names);
        Assert.NotNull(dto.Units);
        Assert.Empty(dto.Units);
        Assert.NotNull(dto.Documents);
        Assert.Empty(dto.Documents);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
