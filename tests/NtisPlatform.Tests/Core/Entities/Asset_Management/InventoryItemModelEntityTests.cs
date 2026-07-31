using System;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for InventoryItemModelEntity - a specific model/brand under an InventoryItemName
/// (e.g. "ThinkPad T14" under "Laptop").
/// </summary>
public class InventoryItemModelEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var deletionDate = DateTime.UtcNow;
        var entity = new InventoryItemModelEntity
        {
            Id = 1,
            InventoryItemNameId = 5,
            ModelName = "ThinkPad T14",
            DisplayOrder = 3,
            Description = "Business laptop",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(5, entity.InventoryItemNameId);
        Assert.Equal("ThinkPad T14", entity.ModelName);
        Assert.Equal(3, entity.DisplayOrder);
        Assert.Equal("Business laptop", entity.Description);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_ModelNameIsEmptyString_DisplayOrderIsZero()
    {
        var entity = new InventoryItemModelEntity();

        Assert.Equal(string.Empty, entity.ModelName);
        Assert.Equal(0, entity.DisplayOrder);
        Assert.Null(entity.Description);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void ImplementsIHardDeletable()
    {
        Assert.True(typeof(IHardDeletable).IsAssignableFrom(typeof(InventoryItemModelEntity)));
    }

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = new InventoryItemModelEntity();
        var now = DateTime.UtcNow;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new InventoryItemModelEntity();

        Assert.True(entity.IsActive);
    }
}
