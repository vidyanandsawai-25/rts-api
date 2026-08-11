using System;
using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.InventoryAsset;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Mapping tests for InventoryBatchMappingProfile. Focused on the CreateInventoryBatchDto /
/// UpdateInventoryBatchDto "InventoryItemConditionId" &lt;-&gt; InventoryBatchEntity.ConditionId
/// wiring: the entity property was renamed from InventoryItemConditionId to ConditionId (to match
/// the [AMS].[InventoryBatch].ConditionId column) while the DTOs kept the old name, so AutoMapper's
/// by-name convention no longer matches them by default and needs an explicit ForMember.
///
/// Note: this profile is also covered by the project-wide AutoMapperValidationTest, which allowlists
/// several intentionally-unmapped destination members on InventoryBatchDto/InventoryBatchDetailDto/
/// InventoryUnitResponseDto (Names, Units, Documents, Condition, ConditionFactor, CVFormula, etc. -
/// resolved by application code after mapping, not by AutoMapper). A bare AssertConfigurationIsValid()
/// here would fail on those pre-existing, already-documented gaps, which are unrelated to what this
/// file actually verifies, so we assert specific field-level behavior instead.
/// </summary>
public class InventoryBatchMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public InventoryBatchMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<InventoryBatchMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    #region CreateDto to Entity

    [Fact]
    public void Map_CreateDtoToEntity_MapsConditionId_FromInventoryItemConditionId()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemModelId = 4,
            InventoryItemConditionId = 99,
            OwningDepartmentId = 5,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m
        };

        var entity = _mapper.Map<InventoryBatchEntity>(dto);

        Assert.Equal(99, entity.ConditionId);
    }

    [Fact]
    public void Map_CreateDtoToEntity_MapsAllForeignKeyIds()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemModelId = 4,
            InventoryItemConditionId = 5,
            OwningDepartmentId = 6,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m
        };

        var entity = _mapper.Map<InventoryBatchEntity>(dto);

        Assert.Equal(1, entity.ParentAssetId);
        Assert.Equal(2, entity.InventoryItemCategoryId);
        Assert.Equal(3, entity.InventoryItemNameId);
        Assert.Equal(4, entity.InventoryItemModelId);
        Assert.Equal(5, entity.ConditionId);
        Assert.Equal(6, entity.OwningDepartmentId);
    }

    [Fact]
    public void Map_CreateDtoToEntity_ComputesTotalBatchValue_AsQuantityTimesUnitValue()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 5,
            PurchaseDate = DateTime.Now,
            Quantity = 4,
            UnitValue = 250.50m
        };

        var entity = _mapper.Map<InventoryBatchEntity>(dto);

        Assert.Equal(1002.00m, entity.TotalBatchValue);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresAuditAndComputedFields()
    {
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 5,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m
        };

        var entity = _mapper.Map<InventoryBatchEntity>(dto);

        Assert.Equal(0, entity.Id);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedDate);
        Assert.Null(entity.TotalBatchCV);
        Assert.Null(entity.ParentAsset);
        Assert.Empty(entity.Units);
    }

    [Fact]
    public void Map_CreateDtoToEntity_MapsOptionalFields()
    {
        var invoiceDate = DateTime.Now.AddDays(-2);
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 5,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m,
            Specifications = "16GB RAM",
            InvoiceNumber = "INV-001",
            InvoiceDate = invoiceDate,
            InvoiceFileName = "invoice.pdf",
            PhotoFileName = "photo.jpg"
        };

        var entity = _mapper.Map<InventoryBatchEntity>(dto);

        Assert.Equal("16GB RAM", entity.Specifications);
        Assert.Equal("INV-001", entity.InvoiceNumber);
        Assert.Equal(invoiceDate, entity.InvoiceDate);
        Assert.Equal("invoice.pdf", entity.InvoiceFileName);
        Assert.Equal("photo.jpg", entity.PhotoFileName);
    }

    [Fact]
    public void Map_CreateDtoToEntity_NullOwningDepartmentId_MapsToZero_NotToNullReferenceOrException()
    {
        // Documents current behavior: CreateInventoryBatchDto.OwningDepartmentId/InventoryItemModelId
        // are optional (int?) but the entity/DB column are non-nullable. AutoMapper's built-in
        // Nullable<T> converter silently substitutes default(int) rather than throwing, so a client
        // that omits OwningDepartmentId produces OwningDepartmentId = 0 here (which then fails at the
        // FK constraint, not at DTO validation). This is a pre-existing DTO/entity nullability gap,
        // not something this mapping profile can fix on its own - documenting the actual behavior.
        var dto = new CreateInventoryBatchDto
        {
            ParentAssetId = 1,
            InventoryItemCategoryId = 2,
            InventoryItemNameId = 3,
            InventoryItemConditionId = 5,
            OwningDepartmentId = null,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m
        };

        var entity = _mapper.Map<InventoryBatchEntity>(dto);

        Assert.Equal(0, entity.OwningDepartmentId);
    }

    #endregion

    #region UpdateDto to Entity - partial update semantics

    [Fact]
    public void Map_UpdateDtoToEntity_MapsConditionId_WhenProvided()
    {
        var existingEntity = new InventoryBatchEntity { Id = 1, ParentAssetId = 1, ConditionId = 10 };
        var updateDto = new UpdateInventoryBatchDto { BatchId = 1, InventoryItemConditionId = 20 };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(20, existingEntity.ConditionId);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_NullConditionId_LeavesExistingConditionIdUnchanged()
    {
        var existingEntity = new InventoryBatchEntity { Id = 1, ParentAssetId = 1, ConditionId = 10 };
        var updateDto = new UpdateInventoryBatchDto { BatchId = 1, InventoryItemConditionId = null };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(10, existingEntity.ConditionId);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresParentAssetIdAndQuantity()
    {
        var existingEntity = new InventoryBatchEntity { Id = 1, ParentAssetId = 1, Quantity = 5 };
        var updateDto = new UpdateInventoryBatchDto { BatchId = 1 };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(1, existingEntity.ParentAssetId);
        Assert.Equal(5, existingEntity.Quantity);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresAuditFields()
    {
        var createdDate = DateTime.Now.AddDays(-10);
        var existingEntity = new InventoryBatchEntity { Id = 1, ParentAssetId = 1, CreatedDate = createdDate };
        var updateDto = new UpdateInventoryBatchDto { BatchId = 1, Specifications = "Updated" };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(createdDate, existingEntity.CreatedDate);
        Assert.Null(existingEntity.UpdatedDate);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_MapsProvidedOptionalFields()
    {
        var existingEntity = new InventoryBatchEntity { Id = 1, ParentAssetId = 1, Specifications = "Old spec", UnitValue = 5m };
        var updateDto = new UpdateInventoryBatchDto
        {
            BatchId = 1,
            Specifications = "New spec",
            UnitValue = 15m,
            InventoryItemNameId = 42,
            OwningDepartmentId = 7
        };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal("New spec", existingEntity.Specifications);
        Assert.Equal(15m, existingEntity.UnitValue);
        Assert.Equal(42, existingEntity.InventoryItemNameId);
        Assert.Equal(7, existingEntity.OwningDepartmentId);
    }

    #endregion

    #region Entity to Dto

    [Fact]
    public void Map_EntityToInventoryBatchDto_MapsBatchId_FromId()
    {
        var entity = new InventoryBatchEntity { Id = 7, ParentAssetId = 1, Quantity = 2, UnitValue = 100m, TotalBatchValue = 200m };

        var dto = _mapper.Map<InventoryBatchDto>(entity);

        Assert.Equal(7, dto.BatchId);
    }

    [Fact]
    public void Map_EntityToInventoryBatchDto_TotalCapitalValue_DefaultsToZero_WhenTotalBatchCVIsNull()
    {
        var entity = new InventoryBatchEntity { Id = 1, ParentAssetId = 1, TotalBatchCV = null };

        var dto = _mapper.Map<InventoryBatchDto>(entity);

        Assert.Equal(0m, dto.TotalCapitalValue);
    }

    [Fact]
    public void Map_EntityToInventoryBatchDto_ComputesTotalDepreciation_AsValueMinusCapitalValue()
    {
        var entity = new InventoryBatchEntity
        {
            Id = 1,
            ParentAssetId = 1,
            TotalBatchValue = 1000m,
            TotalBatchCV = 650m
        };

        var dto = _mapper.Map<InventoryBatchDto>(entity);

        Assert.Equal(650m, dto.TotalCapitalValue);
        Assert.Equal(350m, dto.TotalDepreciation);
    }

    [Fact]
    public void Map_EntityToInventoryBatchDetailDto_MapsBatchId_FromId()
    {
        var entity = new InventoryBatchEntity
        {
            Id = 9,
            ParentAssetId = 1,
            PurchaseDate = DateTime.Now,
            Quantity = 1,
            UnitValue = 10m,
            TotalBatchValue = 10m
        };

        var dto = _mapper.Map<InventoryBatchDetailDto>(entity);

        Assert.Equal(9, dto.BatchId);
    }

    #endregion
}
