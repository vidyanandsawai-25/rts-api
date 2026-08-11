using System;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for InventoryItemCategoryEntity - top-level inventory item type (e.g. Furniture, IT
/// Equipment). Physically under Core/Entities/Asset_Management/ but namespaced Master, matching
/// its sibling InventoryItemName/Model master entities.
/// </summary>
public class InventoryItemCategoryEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var deletionDate = DateTime.Now;
        var entity = new InventoryItemCategoryEntity
        {
            Id = 1,
            AssetCategoryId = 3,
            TypeCode = "FURN",
            TypeName = "Furniture",
            DisplayOrder = 2,
            DepreciationRate = 0.15m,
            Description = "Furniture items",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(3, entity.AssetCategoryId);
        Assert.Equal("FURN", entity.TypeCode);
        Assert.Equal("Furniture", entity.TypeName);
        Assert.Equal(2, entity.DisplayOrder);
        Assert.Equal(0.15m, entity.DepreciationRate);
        Assert.Equal("Furniture items", entity.Description);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_TypeNameIsEmptyString_DepreciationRateDefaultsTo10Percent()
    {
        var entity = new InventoryItemCategoryEntity();

        Assert.Equal(0, entity.AssetCategoryId);
        Assert.Equal(string.Empty, entity.TypeCode);
        Assert.Equal(string.Empty, entity.TypeName);
        Assert.Null(entity.DisplayOrder);
        Assert.Equal(0.10m, entity.DepreciationRate);
        Assert.Null(entity.Description);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void ImplementsIHardDeletable()
    {
        Assert.True(typeof(IHardDeletable).IsAssignableFrom(typeof(InventoryItemCategoryEntity)));
    }

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = new InventoryItemCategoryEntity();
        var now = DateTime.Now;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new InventoryItemCategoryEntity();

        Assert.True(entity.IsActive);
    }
}
