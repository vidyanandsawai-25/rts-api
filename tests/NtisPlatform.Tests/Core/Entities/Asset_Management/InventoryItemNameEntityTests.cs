using System;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for InventoryItemNameEntity - a specific item name (sub-type) under an
/// InventoryItemCategory (e.g. "Laptop" under "IT Equipment").
/// </summary>
public class InventoryItemNameEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var deletionDate = DateTime.Now;
        var entity = new InventoryItemNameEntity
        {
            Id = 1,
            InventoryItemCategoryId = 5,
            SubTypeCode = "LAPTOP",
            SubTypeName = "Laptop",
            DisplayOrder = 1,
            Description = "Portable computers",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(5, entity.InventoryItemCategoryId);
        Assert.Equal("LAPTOP", entity.SubTypeCode);
        Assert.Equal("Laptop", entity.SubTypeName);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.Equal("Portable computers", entity.Description);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_SubTypeCodeAndNameAreEmptyString_DisplayOrderIsZero()
    {
        var entity = new InventoryItemNameEntity();

        Assert.Equal(string.Empty, entity.SubTypeCode);
        Assert.Equal(string.Empty, entity.SubTypeName);
        Assert.Equal(0, entity.DisplayOrder);
        Assert.Null(entity.Description);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void ImplementsIHardDeletable()
    {
        Assert.True(typeof(IHardDeletable).IsAssignableFrom(typeof(InventoryItemNameEntity)));
    }

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = new InventoryItemNameEntity();
        var now = DateTime.Now;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new InventoryItemNameEntity();

        Assert.True(entity.IsActive);
    }
}
