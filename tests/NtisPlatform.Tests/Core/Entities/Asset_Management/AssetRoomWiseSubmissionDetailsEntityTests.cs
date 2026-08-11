using System;
using System.Collections.Generic;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for AssetRoomWiseSubmissionDetailsEntity - room-wise details for child assets
/// (rooms/shops) under a parent asset.
/// </summary>
public class AssetRoomWiseSubmissionDetailsEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var deletionDate = DateTime.UtcNow;
        var entity = new AssetRoomWiseSubmissionDetailsEntity
        {
            Id = 1,
            AssetId = 10,
            SubUnitsDetailsId = 20,
            LengthMtr = 4.0,
            WidthMtr = 3.0,
            LengthFt = 13.12,
            WidthFt = 9.84,
            AreaSqMtr = 12.0,
            AreaSqFeet = 129.2,
            HeightMtr = 3.2,
            HeightFt = 10.5,
            TotalAreaSqMtr = 12.0,
            TotalAreaSqFeet = 129.2,
            Shape = "Rectangle",
            RoomNo = "R-101",
            OuterYesNo = true,
            RoomType = "Bedroom",
            MinusYesNo = true,
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.AssetId);
        Assert.Equal(20, entity.SubUnitsDetailsId);
        Assert.Equal(4.0, entity.LengthMtr);
        Assert.Equal(3.0, entity.WidthMtr);
        Assert.Equal(13.12, entity.LengthFt);
        Assert.Equal(9.84, entity.WidthFt);
        Assert.Equal(12.0, entity.AreaSqMtr);
        Assert.Equal(129.2, entity.AreaSqFeet);
        Assert.Equal(3.2, entity.HeightMtr);
        Assert.Equal(10.5, entity.HeightFt);
        Assert.Equal(12.0, entity.TotalAreaSqMtr);
        Assert.Equal(129.2, entity.TotalAreaSqFeet);
        Assert.Equal("Rectangle", entity.Shape);
        Assert.Equal("R-101", entity.RoomNo);
        Assert.True(entity.OuterYesNo);
        Assert.Equal("Bedroom", entity.RoomType);
        Assert.True(entity.MinusYesNo);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_BoolFlagsAreFalse_NullableFieldsAreNull()
    {
        var entity = new AssetRoomWiseSubmissionDetailsEntity();

        Assert.False(entity.OuterYesNo);
        Assert.False(entity.MinusYesNo);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.AssetId);
        Assert.Null(entity.SubUnitsDetailsId);
        Assert.Null(entity.Shape);
        Assert.Null(entity.RoomNo);
        Assert.Null(entity.RoomType);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_NavigationProperties_AssetAndSubUnitsDetailsAreNull()
    {
        var entity = new AssetRoomWiseSubmissionDetailsEntity();

        Assert.Null(entity.Asset);
        Assert.Null(entity.SubUnitsDetails);
    }

    [Fact]
    public void Defaults_RoomMinusDataCollection_IsNull_UnlikeSiblingCollectionNavigations()
    {
        // Unlike AssetLeaseRentDetailsEntity.History or LeaseRentBillTransactionEntity.Details
        // (both initialized to `= new List<>()`), RoomMinusData has no default initializer and is
        // a plain nullable collection - a caller must assign a list before adding to it. Documenting
        // this inconsistency rather than treating it as a defect to fix.
        var entity = new AssetRoomWiseSubmissionDetailsEntity();

        Assert.Null(entity.RoomMinusData);
    }

    [Fact]
    public void RoomMinusData_CanBeAssignedAndModified()
    {
        var entity = new AssetRoomWiseSubmissionDetailsEntity
        {
            RoomMinusData = new List<AssetRoomWiseMinusDataEntity> { new() { Id = 1 } }
        };

        Assert.Single(entity.RoomMinusData!);
    }

    [Fact]
    public void ImplementsIHardDeletable()
    {
        Assert.True(typeof(IHardDeletable).IsAssignableFrom(typeof(AssetRoomWiseSubmissionDetailsEntity)));
    }

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = new AssetRoomWiseSubmissionDetailsEntity();
        var now = DateTime.UtcNow;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new AssetRoomWiseSubmissionDetailsEntity();

        Assert.True(entity.IsActive);
    }
}
