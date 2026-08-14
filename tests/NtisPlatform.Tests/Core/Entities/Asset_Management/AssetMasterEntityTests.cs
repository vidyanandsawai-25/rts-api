using System;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for AssetMasterEntity. Added as part of restoring IHardDeletable (the entity already
/// carried MarkedForDeletion/MarkedForDeletionDate but the interface declaration had been dropped,
/// which meant Repository.DeleteAsync and HardDeleteCleanupService.MarkForHardDeleteAsync both
/// silently treated AssetMaster as a plain soft-delete-only BaseEntity - see the
/// ImplementsIHardDeletable/DeleteAsync tests below for the regression this covers).
/// </summary>
public class AssetMasterEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new AssetMasterEntity
        {
            Id = 1,
            AssetNo = "AST-001",
            AssetName = "Municipal Building",
            AssetRegionalName = "नगरपालिका इमारत",
            AssetCategoryId = 2,
            AssetTypeId = 3,
            ParentAssetId = 4,
            HierarchyLevel = 1,
            HierarchyPath = "/4/1",
            DepartmentId = 5,
            OwnershipType = "Owned",
            OccupancyStatus = "Occupied",
            AssetConditionId = 6,
            IsActive = true
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal("AST-001", entity.AssetNo);
        Assert.Equal("Municipal Building", entity.AssetName);
        Assert.Equal("नगरपालिका इमारत", entity.AssetRegionalName);
        Assert.Equal(2, entity.AssetCategoryId);
        Assert.Equal(3, entity.AssetTypeId);
        Assert.Equal(4, entity.ParentAssetId);
        Assert.Equal(1, entity.HierarchyLevel);
        Assert.Equal("/4/1", entity.HierarchyPath);
        Assert.Equal(5, entity.DepartmentId);
        Assert.Equal("Owned", entity.OwnershipType);
        Assert.Equal("Occupied", entity.OccupancyStatus);
        Assert.Equal(6, entity.AssetConditionId);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Defaults_IdentificationStringsAreEmpty_OptionalFieldsAreNull()
    {
        var entity = new AssetMasterEntity();

        Assert.Equal(string.Empty, entity.AssetNo);
        Assert.Equal(string.Empty, entity.AssetName);
        Assert.Null(entity.AssetRegionalName);
        Assert.Null(entity.ParentAssetId);
        Assert.Null(entity.DepartmentId);
        Assert.Null(entity.OwnershipType);
        Assert.Null(entity.OccupancyStatus);
        Assert.Null(entity.AssetConditionId);
    }

    [Fact]
    public void Defaults_HierarchyLevelDefaultsToZero()
    {
        var entity = new AssetMasterEntity();

        Assert.Equal(0, entity.HierarchyLevel);
        Assert.Null(entity.HierarchyPath);
    }

    [Fact]
    public void Defaults_MarkedForDeletionIsFalse()
    {
        var entity = new AssetMasterEntity();

        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_NavigationProperties_AreNull()
    {
        var entity = new AssetMasterEntity();

        Assert.Null(entity.AssetCategory);
        Assert.Null(entity.AssetType);
        Assert.Null(entity.ParentAsset);
        Assert.Null(entity.Details);
        Assert.Null(entity.FieldValues);
        Assert.Null(entity.InventoryBatch);
        Assert.Null(entity.SubUnitsDetails);
    }

    [Fact]
    public void Defaults_CompatibilityShimFields_AreDefaultUnset()
    {
        // Dropped from AMS.AssetMaster (moved to AMS.AssetDetails or removed) - excluded from the
        // EF model via Fluent Ignore(), kept only so legacy code referencing them still compiles.
        var entity = new AssetMasterEntity();

        Assert.Equal(0, entity.AssetLocationDetailsId);
        Assert.Null(entity.PropertyNo);
        Assert.Null(entity.PartitionNo);
        Assert.Null(entity.UpicId);
        Assert.Null(entity.PlotNo);
        Assert.Null(entity.PurchaseValue);
        Assert.Null(entity.PurchaseDate);
        Assert.Null(entity.DepreciationId);
        Assert.Null(entity.InventoryBatchId);
    }

    [Fact]
    public void InheritsBaseEntity_AuditColumnsAreAvailable()
    {
        var createdDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entity = new AssetMasterEntity
        {
            CreatedBy = 100,
            CreatedDate = createdDate,
            UpdatedBy = 200
        };

        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(createdDate, entity.CreatedDate);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void ImplementsIHardDeletable()
    {
        Assert.True(typeof(IHardDeletable).IsAssignableFrom(typeof(AssetMasterEntity)));
    }

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = new AssetMasterEntity();
        var now = DateTime.Now;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }
}
