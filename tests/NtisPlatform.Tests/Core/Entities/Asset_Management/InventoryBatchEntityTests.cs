using System;
using System.Collections.Generic;
using System.Linq;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for InventoryBatchEntity, which mirrors [AMS].[InventoryBatch]. ParentAssetId,
/// OwningDepartmentId, InventoryItemCategoryId, InventoryItemNameId, InventoryItemModelId and
/// ConditionId are plain non-nullable ints (not int?) because those columns are NOT NULL on the
/// table — see the FK_InventoryBatch_* constraints in ApplicationDbContext. The entity also
/// implements IHardDeletable (MarkedForDeletion / MarkedForDeletionDate), matching the sibling
/// InventoryItemCategoryEntity / InventoryItemModelEntity / InventoryItemNameEntity masters.
/// </summary>
public class InventoryBatchEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var purchaseDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var invoiceDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        var entity = new InventoryBatchEntity
        {
            Id = 1,
            ParentAssetId = 10,
            OwningDepartmentId = 20,
            InventoryItemCategoryId = 30,
            InventoryItemNameId = 40,
            InventoryItemModelId = 50,
            ConditionId = 60,
            Specifications = "16GB RAM, 512GB SSD",
            PurchaseDate = purchaseDate,
            Quantity = 5,
            UnitValue = 1200.50m,
            TotalBatchValue = 6002.50m,
            TotalBatchCV = 5500.00m,
            InvoiceNumber = "INV-2026-001",
            InvoiceDate = invoiceDate,
            InvoiceFileName = "invoice.pdf",
            PhotoFileName = "photo.jpg",
            IsActive = true
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.ParentAssetId);
        Assert.Equal(20, entity.OwningDepartmentId);
        Assert.Equal(30, entity.InventoryItemCategoryId);
        Assert.Equal(40, entity.InventoryItemNameId);
        Assert.Equal(50, entity.InventoryItemModelId);
        Assert.Equal(60, entity.ConditionId);
        Assert.Equal("16GB RAM, 512GB SSD", entity.Specifications);
        Assert.Equal(purchaseDate, entity.PurchaseDate);
        Assert.Equal(5, entity.Quantity);
        Assert.Equal(1200.50m, entity.UnitValue);
        Assert.Equal(6002.50m, entity.TotalBatchValue);
        Assert.Equal(5500.00m, entity.TotalBatchCV);
        Assert.Equal("INV-2026-001", entity.InvoiceNumber);
        Assert.Equal(invoiceDate, entity.InvoiceDate);
        Assert.Equal("invoice.pdf", entity.InvoiceFileName);
        Assert.Equal("photo.jpg", entity.PhotoFileName);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var entity = new InventoryBatchEntity();

        Assert.True(entity.IsActive);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.Null(entity.Specifications);
        Assert.Null(entity.TotalBatchCV);
        Assert.Null(entity.InvoiceNumber);
        Assert.Null(entity.InvoiceDate);
        Assert.Null(entity.InvoiceFileName);
        Assert.Null(entity.PhotoFileName);
        Assert.Null(entity.ParentAsset);
        Assert.NotNull(entity.Units);
        Assert.Empty(entity.Units);
    }

    [Fact]
    public void Units_CanBeAssignedAndModified()
    {
        var entity = new InventoryBatchEntity();
        var unit = new InventoryAssetDetailEntity { Id = 1 };

        entity.Units.Add(unit);

        Assert.Single(entity.Units);
        Assert.Same(unit, entity.Units.First());
    }

    [Fact]
    public void Units_CanBeReplacedWithAnotherCollection()
    {
        var entity = new InventoryBatchEntity
        {
            Units = new List<InventoryAssetDetailEntity>
            {
                new() { Id = 1 },
                new() { Id = 2 }
            }
        };

        Assert.Equal(2, entity.Units.Count);
    }

    [Fact]
    public void ImplementsIHardDeletable()
    {
        Assert.True(typeof(IHardDeletable).IsAssignableFrom(typeof(InventoryBatchEntity)));
    }

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = new InventoryBatchEntity();
        var now = DateTime.Now;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void InheritsBaseEntity_AuditColumnsAreAvailable()
    {
        var entity = new InventoryBatchEntity
        {
            CreatedBy = 100,
            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedBy = 200,
            UpdatedDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), entity.CreatedDate);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.Equal(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), entity.UpdatedDate);
    }
}
