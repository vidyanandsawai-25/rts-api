using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.InventoryBatch;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for the InventoryBatchDto / CreateInventoryBatchDto / UpdateInventoryBatchDto declared in
/// DTOs/Asset_Management/InventoryBatch/InventoryBatchDto.cs.
///
/// IMPORTANT: this is a separate, unrelated type set from the identically-named classes in
/// DTOs/Asset_Management/InventoryAsset/InventoryAssetDtos.cs (see InventoryAssetDtosTests.cs).
/// A repo-wide search found no CreateMap, service, or controller referencing this
/// "NtisPlatform.Application.DTOs.Asset_Management.InventoryBatch" namespace anywhere in src/ -
/// InventoryBatchMappingProfile and the live inventory-batch registration flow both use the
/// InventoryAsset-namespace versions instead. These tests cover the type as it exists today;
/// they do not imply the type is wired into any live code path. Note also that
/// UpdateInventoryBatchDto here has no BatchId property at all (unlike its InventoryAsset-namespace
/// counterpart) - the route id would be the only identifier if this DTO were ever wired up.
/// </summary>
public class InventoryBatchDtoTests
{
    #region InventoryBatchDto (read)

    [Fact]
    public void InventoryBatchDto_PropertiesGetAndSetCorrectly()
    {
        var purchaseDate = DateTime.Now;
        var invoiceDate = DateTime.Now.AddDays(-3);
        var dto = new InventoryBatchDto
        {
            Id = 1,
            IsActive = true,
            ParentAssetId = 10,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemModelId = 4,
            InventoryItemConditionId = 5,
            OwningDepartmentId = 6,
            Specifications = "16GB RAM",
            PurchaseDate = purchaseDate,
            Quantity = 5,
            UnitValue = 100m,
            TotalBatchValue = 500m,
            TotalBatchCV = 400m,
            InvoiceNumber = "INV-1",
            InvoiceDate = invoiceDate,
            InvoiceFileName = "invoice.pdf",
            PhotoFileName = "photo.jpg",
            ParentAssetName = "Building A",
            CategoryName = "IT Equipment",
            ItemName = "Laptop",
            ModelName = "Model X",
            ConditionName = "Good",
            DepartmentName = "IT"
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(10, dto.ParentAssetId);
        Assert.Equal(2, dto.InventoryItemCategoryId);
        Assert.Equal(3, dto.InventoryItemNameId);
        Assert.Equal(4, dto.InventoryItemModelId);
        Assert.Equal(5, dto.InventoryItemConditionId);
        Assert.Equal(6, dto.OwningDepartmentId);
        Assert.Equal("16GB RAM", dto.Specifications);
        Assert.Equal(purchaseDate, dto.PurchaseDate);
        Assert.Equal(5, dto.Quantity);
        Assert.Equal(100m, dto.UnitValue);
        Assert.Equal(500m, dto.TotalBatchValue);
        Assert.Equal(400m, dto.TotalBatchCV);
        Assert.Equal("INV-1", dto.InvoiceNumber);
        Assert.Equal(invoiceDate, dto.InvoiceDate);
        Assert.Equal("invoice.pdf", dto.InvoiceFileName);
        Assert.Equal("photo.jpg", dto.PhotoFileName);
        Assert.Equal("Building A", dto.ParentAssetName);
        Assert.Equal("IT Equipment", dto.CategoryName);
        Assert.Equal("Laptop", dto.ItemName);
        Assert.Equal("Model X", dto.ModelName);
        Assert.Equal("Good", dto.ConditionName);
        Assert.Equal("IT", dto.DepartmentName);
    }

    [Fact]
    public void InventoryBatchDto_Defaults_OptionalFieldsAreNull()
    {
        var dto = new InventoryBatchDto();

        Assert.Null(dto.InventoryItemCategoryId);
        Assert.Null(dto.InventoryItemNameId);
        Assert.Null(dto.InventoryItemModelId);
        Assert.Null(dto.InventoryItemConditionId);
        Assert.Null(dto.OwningDepartmentId);
        Assert.Null(dto.Specifications);
        Assert.Null(dto.TotalBatchCV);
        Assert.Null(dto.InvoiceNumber);
        Assert.Null(dto.InvoiceDate);
    }

    #endregion

    #region CreateInventoryBatchDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithZeroInventoryItemCategoryId_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 0,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.InventoryItemCategoryId))
            && r.ErrorMessage == "AMS_InventoryBatch_InventoryItemCategoryId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroInventoryItemConditionId_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 0,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.InventoryItemConditionId))
            && r.ErrorMessage == "AMS_InventoryBatch_InventoryItemConditionId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroQuantity_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.Now,
            Quantity = 0,
            UnitValue = 10m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.Quantity))
            && r.ErrorMessage == "AMS_InventoryBatch_Quantity_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroUnitValue_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 0m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.UnitValue))
            && r.ErrorMessage == "AMS_InventoryBatch_UnitValue_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeTotalBatchCV_IsInvalid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m,
            TotalBatchCV = -1m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.TotalBatchCV))
            && r.ErrorMessage == "AMS_InventoryBatch_TotalBatchCV_InvalidRange");
    }

    [Fact]
    public void Create_WithMissingPurchaseDate_IsInvalid()
    {
        // PurchaseDate was changed from a non-nullable DateTime to DateTime? specifically so
        // [Required] can actually detect "missing": a bare non-nullable DateTime always binds an
        // omitted request field to DateTime.MinValue, which is never null, so [Required] could
        // never fire for it. With the nullable type, an omitted field binds to null and IS caught.
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            Quantity = 1,
            UnitValue = 10m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateInventoryBatchDto.PurchaseDate))
            && r.ErrorMessage == "AMS_InventoryBatch_PurchaseDate_Required");
        Assert.Null(dto.PurchaseDate);
    }

    [Fact]
    public void Create_WithExplicitDateTimeMinValue_CurrentlyPassesValidation()
    {
        // Known residual gap: [Required] on a nullable type only rejects an actual null - it does
        // not reject a client that explicitly submits the literal "0001-01-01" as the value (as
        // opposed to omitting the field, which is what [Required] now correctly catches above).
        // Guarding against that specific literal would need an extra custom check; not added here
        // since this DTO is unreferenced by any CreateMap/service/controller in src/ (see the type
        // summary above) and the task's preferred fix (nullable + [Required]) doesn't call for it -
        // flagged in the review summary as an open question rather than assumed.
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            Quantity = 1,
            UnitValue = 10m,
            PurchaseDate = DateTime.MinValue
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithValidPurchaseDate_IsValid()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 4,
            Quantity = 1,
            UnitValue = 10m,
            PurchaseDate = DateTime.Now
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_Defaults_DocumentFilesAndMetadataAreNull()
    {
        var dto = new CreateInventoryBatchDto();

        Assert.Null(dto.DocumentFiles);
        Assert.Null(dto.DocumentMetadataJson);
        Assert.Null(dto.OwningDepartmentId);
        Assert.Null(dto.InventoryItemModelId);
    }

    #endregion

    #region UpdateInventoryBatchDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateInventoryBatchDto
        {
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithMissingPurchaseDate_IsInvalid()
    {
        // Same nullable-DateTime fix as CreateInventoryBatchDto.PurchaseDate above.
        var dto = new UpdateInventoryBatchDto
        {
            Quantity = 1,
            UnitValue = 10m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateInventoryBatchDto.PurchaseDate))
            && r.ErrorMessage == "AMS_InventoryBatch_PurchaseDate_Required");
        Assert.Null(dto.PurchaseDate);
    }

    [Fact]
    public void Update_WithZeroUnitValue_IsInvalid()
    {
        var dto = new UpdateInventoryBatchDto
        {
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 0m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateInventoryBatchDto.UnitValue))
            && r.ErrorMessage == "AMS_InventoryBatch_UnitValue_InvalidRange");
    }

    [Fact]
    public void Update_WithZeroQuantity_IsInvalid()
    {
        var dto = new UpdateInventoryBatchDto
        {
            PurchaseDate = DateTime.Now,
            Quantity = 0,
            UnitValue = 10m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateInventoryBatchDto.Quantity))
            && r.ErrorMessage == "AMS_InventoryBatch_Quantity_InvalidRange");
    }

    [Fact]
    public void Update_HasNoBatchIdProperty_UnlikeInventoryAssetNamespaceCounterpart()
    {
        // Documents a real shape difference from InventoryAsset.UpdateInventoryBatchDto, which
        // does have a BatchId - if this type were ever wired up, only the route id would identify
        // the batch being updated.
        Assert.Null(typeof(UpdateInventoryBatchDto).GetProperty("BatchId"));
    }

    [Fact]
    public void Update_Defaults_OptionalForeignKeysAndSpecificationsAreNull()
    {
        var dto = new UpdateInventoryBatchDto { PurchaseDate = DateTime.Now, Quantity = 1, UnitValue = 10m };

        Assert.Null(dto.InventoryItemCategoryId);
        Assert.Null(dto.InventoryItemNameId);
        Assert.Null(dto.InventoryItemModelId);
        Assert.Null(dto.InventoryItemConditionId);
        Assert.Null(dto.OwningDepartmentId);
        Assert.Null(dto.Specifications);
        Assert.Null(dto.TotalBatchCV);
        Assert.Null(dto.InvoiceNumber);
        Assert.Null(dto.InvoiceDate);
        Assert.Null(dto.InvoiceFileName);
        Assert.Null(dto.PhotoFileName);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
