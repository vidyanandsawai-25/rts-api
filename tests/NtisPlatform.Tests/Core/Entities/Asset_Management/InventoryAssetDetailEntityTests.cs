using System;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for InventoryAssetDetailEntity - an individual registered inventory unit (one physical
/// item) belonging to an InventoryBatchEntity.
/// </summary>
public class InventoryAssetDetailEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var deletionDate = DateTime.UtcNow;
        var entity = new InventoryAssetDetailEntity
        {
            Id = 1,
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
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.AssetId);
        Assert.Equal(20, entity.BatchId);
        Assert.Equal(1, entity.UnitNumber);
        Assert.Equal(2, entity.InventoryItemCategoryId);
        Assert.Equal(3, entity.InventoryItemNameId);
        Assert.Equal(4, entity.InventoryItemModelId);
        Assert.Equal(5, entity.InventoryItemConditionId);
        Assert.Equal(6, entity.OwningDepartmentId);
        Assert.Equal("16GB RAM", entity.Specifications);
        Assert.Equal("photo.jpg", entity.PhotoFileId);
        Assert.Equal(500m, entity.UnitPurchaseValue);
        Assert.Equal(400m, entity.UnitCapitalValue);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_NullableForeignKeysAndOptionalFields_AreNull()
    {
        var entity = new InventoryAssetDetailEntity();

        Assert.Null(entity.InventoryItemCategoryId);
        Assert.Null(entity.InventoryItemNameId);
        Assert.Null(entity.InventoryItemModelId);
        Assert.Null(entity.InventoryItemConditionId);
        Assert.Null(entity.OwningDepartmentId);
        Assert.Null(entity.Specifications);
        Assert.Null(entity.PhotoFileId);
        Assert.Null(entity.UnitCapitalValue);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_NavigationProperties_AreNull()
    {
        var entity = new InventoryAssetDetailEntity();

        Assert.Null(entity.AssetMaster);
        Assert.Null(entity.Batch);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new InventoryAssetDetailEntity();

        Assert.True(entity.IsActive);
    }

    [Fact]
    public void DoesNotImplementIHardDeletable_DespiteHavingTheMatchingFields()
    {
        // Same gap as AssetDetailsEntity/AssetFieldValueEntity/SubUnitsDetailsEntity in this batch -
        // MarkedForDeletion/MarkedForDeletionDate exist but the interface isn't declared, so a
        // consumer that filters by `is IHardDeletable` (e.g. a cleanup job) would skip this entity.
        Assert.False(typeof(IHardDeletable).IsAssignableFrom(typeof(InventoryAssetDetailEntity)));
    }
}
