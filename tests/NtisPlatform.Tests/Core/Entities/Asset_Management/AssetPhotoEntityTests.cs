using System;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

public class AssetPhotoEntityTests
{
    [Fact]
    public void Create_ValidParameters_CreatesEntity()
    {
        var entity = AssetPhotoEntity.Create(10, 2, displayOrder: 1, remarks: "Front view");

        Assert.Equal(10, entity.AssetId);
        Assert.Equal(2, entity.PhotoTypeId);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.Equal("Front view", entity.Remarks);
        Assert.True(entity.IsActive);
        Assert.True(entity.IsLatest);
        Assert.False(entity.MarkedForDeletion);
        Assert.False(entity.HasDocument());
    }

    [Fact]
    public void InternalConstructor_SetsProperties()
    {
        var now = DateTime.UtcNow;
        var entity = new AssetPhotoEntity(10, 2, 100, true, 1, "Remarks", true, now, 5);

        Assert.Equal(10, entity.AssetId);
        Assert.Equal(2, entity.PhotoTypeId);
        Assert.Equal(100, entity.DocumentBindingId);
        Assert.True(entity.IsLatest);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.Equal("Remarks", entity.Remarks);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
        Assert.Equal(5, entity.SubUnitsDetailsId);
    }

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = AssetPhotoEntity.Create(10, 2);
        var now = DateTime.UtcNow;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Create_InvalidAssetIdOrPhotoTypeId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AssetPhotoEntity.Create(0, 2));
        Assert.Throws<ArgumentException>(() => AssetPhotoEntity.Create(10, 0));
    }

    [Fact]
    public void Create_NegativeDisplayOrder_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AssetPhotoEntity.Create(10, 2, displayOrder: -1));
    }

    [Fact]
    public void Create_RemarksTooLong_ThrowsArgumentException()
    {
        var longRemarks = new string('a', 501);
        Assert.Throws<ArgumentException>(() => AssetPhotoEntity.Create(10, 2, remarks: longRemarks));
    }

    [Fact]
    public void CreateWithDocument_ValidParameters_CreatesEntityWithBinding()
    {
        var entity = AssetPhotoEntity.CreateWithDocument(10, 2, documentBindingId: 100, displayOrder: 1, remarks: "Front");

        Assert.Equal(100, entity.DocumentBindingId);
        Assert.True(entity.HasDocument());
    }

    [Fact]
    public void CreateWithDocument_InvalidDocumentBindingId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AssetPhotoEntity.CreateWithDocument(10, 2, documentBindingId: 0));
    }

    [Fact]
    public void CreateWithDocument_NegativeDisplayOrder_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AssetPhotoEntity.CreateWithDocument(10, 2, documentBindingId: 100, displayOrder: -1));
    }

    [Fact]
    public void LinkDocumentBinding_ValidId_SetsDocumentBindingId()
    {
        var entity = AssetPhotoEntity.Create(10, 2);
        entity.LinkDocumentBinding(200);

        Assert.Equal(200, entity.DocumentBindingId);
        Assert.True(entity.HasDocument());
    }

    [Fact]
    public void LinkDocumentBinding_InvalidId_ThrowsArgumentException()
    {
        var entity = AssetPhotoEntity.Create(10, 2);
        Assert.Throws<ArgumentException>(() => entity.LinkDocumentBinding(0));
    }

    [Fact]
    public void LinkDocumentBinding_WhenMarkedForDeletion_ThrowsInvalidOperationException()
    {
        var entity = AssetPhotoEntity.Create(10, 2);
        entity.MarkForDeletion();

        Assert.Throws<InvalidOperationException>(() => entity.LinkDocumentBinding(200));
    }

    [Fact]
    public void UnlinkDocumentBinding_ClearsDocumentBindingId()
    {
        var entity = AssetPhotoEntity.CreateWithDocument(10, 2, documentBindingId: 100);
        entity.UnlinkDocumentBinding();

        Assert.Null(entity.DocumentBindingId);
        Assert.False(entity.HasDocument());
    }

    [Fact]
    public void SetDisplayOrder_Valid_UpdatesDisplayOrder()
    {
        var entity = AssetPhotoEntity.Create(10, 2);
        entity.SetDisplayOrder(5);

        Assert.Equal(5, entity.DisplayOrder);
    }

    [Fact]
    public void SetDisplayOrder_Negative_ThrowsArgumentException()
    {
        var entity = AssetPhotoEntity.Create(10, 2);
        Assert.Throws<ArgumentException>(() => entity.SetDisplayOrder(-1));
    }

    [Fact]
    public void SetRemarks_Valid_UpdatesRemarks()
    {
        var entity = AssetPhotoEntity.Create(10, 2);
        entity.SetRemarks("New Remarks");

        Assert.Equal("New Remarks", entity.Remarks);
    }

    [Fact]
    public void MarkAsSuperseded_SetsIsLatestFalse()
    {
        var entity = AssetPhotoEntity.Create(10, 2);
        entity.MarkAsSuperseded();

        Assert.False(entity.IsLatest);
    }

    [Fact]
    public void MarkForDeletion_SetsMarkedForDeletionTrue_AndDeactivates()
    {
        var entity = AssetPhotoEntity.Create(10, 2);
        entity.MarkForDeletion();

        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);
        Assert.False(entity.IsActive);
        Assert.True(entity.IsLatest);

        Assert.Throws<InvalidOperationException>(() => entity.MarkForDeletion());
    }

    [Fact]
    public void RestoreFromDeletion_RestoresActiveState()
    {
        var entity = AssetPhotoEntity.Create(10, 2);
        Assert.Throws<InvalidOperationException>(() => entity.RestoreFromDeletion());

        entity.MarkForDeletion();
        entity.RestoreFromDeletion();

        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void AssetPhotoNotFoundException_SetsCorrectProperties()
    {
        var ex = new AssetPhotoNotFoundException(42);

        Assert.Equal("ASSET_PHOTO_NOT_FOUND", ex.ErrorCode);
        Assert.Contains("42", ex.Message);
    }
}
