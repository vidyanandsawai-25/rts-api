using System;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;
using NtisPlatform.Core.Entities.Asset_Management;


namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

public class InventoryDocumentEntityTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Create
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidParameters_CreatesEntity()
    {
        var entity = InventoryDocumentEntity.Create(5, 3, displayOrder: 1, remarks: "Test remark");

        Assert.Equal(5, entity.InventoryBatchId);
        Assert.Equal(3, entity.DocumentTypeId);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.Equal("Test remark", entity.Remarks);
        Assert.True(entity.IsActive);
        Assert.True(entity.IsLatest);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.DocumentBindingId);
        Assert.False(entity.HasDocument());
    }

    [Fact]
    public void Create_WithNullDisplayOrderAndRemarks_Succeeds()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);

        Assert.Null(entity.DisplayOrder);
        Assert.Null(entity.Remarks);
        Assert.True(entity.IsActive);
        Assert.True(entity.IsLatest);
    }

    [Fact]
    public void Create_InvalidInventoryBatchId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => InventoryDocumentEntity.Create(0, 3));
        Assert.Throws<ArgumentException>(() => InventoryDocumentEntity.Create(-1, 3));
    }

    [Fact]
    public void Create_InvalidDocumentTypeId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => InventoryDocumentEntity.Create(5, 0));
        Assert.Throws<ArgumentException>(() => InventoryDocumentEntity.Create(5, -1));
    }

    [Fact]
    public void Create_NegativeDisplayOrder_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => InventoryDocumentEntity.Create(5, 3, displayOrder: -1));
    }

    [Fact]
    public void Create_RemarksTooLong_ThrowsArgumentException()
    {
        var longRemarks = new string('x', 501);
        Assert.Throws<ArgumentException>(() => InventoryDocumentEntity.Create(5, 3, remarks: longRemarks));
    }

    [Fact]
    public void Create_RemarksExactly500_Succeeds()
    {
        var remarks = new string('x', 500);
        var entity = InventoryDocumentEntity.Create(5, 3, remarks: remarks);

        Assert.Equal(500, entity.Remarks!.Length);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateWithDocument
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreateWithDocument_ValidParameters_CreatesEntityWithBinding()
    {
        var entity = InventoryDocumentEntity.CreateWithDocument(5, 3, documentBindingId: 100, displayOrder: 1, remarks: "Invoice");

        Assert.Equal(5, entity.InventoryBatchId);
        Assert.Equal(3, entity.DocumentTypeId);
        Assert.Equal(100, entity.DocumentBindingId);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.Equal("Invoice", entity.Remarks);
        Assert.True(entity.HasDocument());
        Assert.True(entity.IsLatest);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void CreateWithDocument_InvalidDocumentBindingId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => InventoryDocumentEntity.CreateWithDocument(5, 3, documentBindingId: 0));
        Assert.Throws<ArgumentException>(() => InventoryDocumentEntity.CreateWithDocument(5, 3, documentBindingId: -5));
    }

    [Fact]
    public void CreateWithDocument_NegativeDisplayOrder_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            InventoryDocumentEntity.CreateWithDocument(5, 3, documentBindingId: 100, displayOrder: -1));
    }

    [Fact]
    public void CreateWithDocument_RemarksTooLong_ThrowsArgumentException()
    {
        var longRemarks = new string('x', 501);
        Assert.Throws<ArgumentException>(() =>
            InventoryDocumentEntity.CreateWithDocument(5, 3, documentBindingId: 100, remarks: longRemarks));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Internal constructor (for EF reconstruction)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InternalConstructor_SetsAllProperties()
    {
        var deletionDate = DateTime.Now;
        var entity = new InventoryDocumentEntity(
            inventoryBatchId: 7,
            documentTypeId: 2,
            documentBindingId: 50,
            isLatest: false,
            displayOrder: 3,
            remarks: "Old version",
            markedForDeletion: true,
            markedForDeletionDate: deletionDate);

        Assert.Equal(7, entity.InventoryBatchId);
        Assert.Equal(2, entity.DocumentTypeId);
        Assert.Equal(50, entity.DocumentBindingId);
        Assert.False(entity.IsLatest);
        Assert.Equal(3, entity.DisplayOrder);
        Assert.Equal("Old version", entity.Remarks);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IHardDeletable explicit interface
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = InventoryDocumentEntity.Create(5, 3);
        var now = DateTime.Now;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LinkDocumentBinding
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LinkDocumentBinding_ValidId_SetsDocumentBindingId()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        entity.LinkDocumentBinding(200);

        Assert.Equal(200, entity.DocumentBindingId);
        Assert.True(entity.HasDocument());
    }

    [Fact]
    public void LinkDocumentBinding_InvalidId_ThrowsArgumentException()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        Assert.Throws<ArgumentException>(() => entity.LinkDocumentBinding(0));
        Assert.Throws<ArgumentException>(() => entity.LinkDocumentBinding(-1));
    }

    [Fact]
    public void LinkDocumentBinding_WhenMarkedForDeletion_ThrowsInvalidOperationException()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        entity.MarkForDeletion();

        Assert.Throws<InvalidOperationException>(() => entity.LinkDocumentBinding(200));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UnlinkDocumentBinding
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UnlinkDocumentBinding_ClearsDocumentBindingId()
    {
        var entity = InventoryDocumentEntity.CreateWithDocument(5, 3, documentBindingId: 100);
        entity.UnlinkDocumentBinding();

        Assert.Null(entity.DocumentBindingId);
        Assert.False(entity.HasDocument());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SetDisplayOrder
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetDisplayOrder_Valid_UpdatesDisplayOrder()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        entity.SetDisplayOrder(5);

        Assert.Equal(5, entity.DisplayOrder);
    }

    [Fact]
    public void SetDisplayOrder_Null_ClearsDisplayOrder()
    {
        var entity = InventoryDocumentEntity.Create(5, 3, displayOrder: 2);
        entity.SetDisplayOrder(null);

        Assert.Null(entity.DisplayOrder);
    }

    [Fact]
    public void SetDisplayOrder_Negative_ThrowsArgumentException()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        Assert.Throws<ArgumentException>(() => entity.SetDisplayOrder(-1));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SetRemarks
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetRemarks_Valid_UpdatesRemarks()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        entity.SetRemarks("Updated remarks");

        Assert.Equal("Updated remarks", entity.Remarks);
    }

    [Fact]
    public void SetRemarks_TooLong_ThrowsArgumentException()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        var longRemarks = new string('z', 501);

        Assert.Throws<ArgumentException>(() => entity.SetRemarks(longRemarks));
    }

    [Fact]
    public void SetRemarks_Null_Succeeds()
    {
        var entity = InventoryDocumentEntity.Create(5, 3, remarks: "Some remark");
        entity.SetRemarks(null);

        Assert.Null(entity.Remarks);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MarkAsSuperseded
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsSuperseded_SetsIsLatestFalse()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        Assert.True(entity.IsLatest);

        entity.MarkAsSuperseded();

        Assert.False(entity.IsLatest);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MarkForDeletion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MarkForDeletion_SetsFlags_AndDeactivates()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        entity.MarkForDeletion();

        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);
        Assert.False(entity.IsActive);
        Assert.False(entity.IsLatest);
    }

    [Fact]
    public void MarkForDeletion_CalledTwice_ThrowsInvalidOperationException()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        entity.MarkForDeletion();

        Assert.Throws<InvalidOperationException>(() => entity.MarkForDeletion());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RestoreFromDeletion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RestoreFromDeletion_RestoresActiveState()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);
        entity.MarkForDeletion();
        entity.RestoreFromDeletion();

        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void RestoreFromDeletion_WhenNotDeleted_ThrowsInvalidOperationException()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);

        Assert.Throws<InvalidOperationException>(() => entity.RestoreFromDeletion());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HasDocument
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void HasDocument_ReturnsFalse_WhenNoBindingId()
    {
        var entity = InventoryDocumentEntity.Create(5, 3);

        Assert.False(entity.HasDocument());
    }

    [Fact]
    public void HasDocument_ReturnsTrue_WhenBindingIdIsPositive()
    {
        var entity = InventoryDocumentEntity.CreateWithDocument(5, 3, documentBindingId: 1);

        Assert.True(entity.HasDocument());
    }
}
