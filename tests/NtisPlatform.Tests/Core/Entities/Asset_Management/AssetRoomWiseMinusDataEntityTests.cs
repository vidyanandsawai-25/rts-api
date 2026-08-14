using System;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for AssetRoomWiseMinusDataEntity - a "minus" (exclusion) area carved out of a room-wise
/// submission (e.g. a stairwell or shaft within a room that shouldn't count toward area).
/// </summary>
public class AssetRoomWiseMinusDataEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var deletionDate = DateTime.Now;
        var entity = new AssetRoomWiseMinusDataEntity
        {
            Id = 1,
            RoomWiseSubmissionId = 5,
            LengthMtr = 3.5,
            LengthFt = 11.48,
            WidthMtr = 2.5,
            WidthFt = 8.2,
            AreaSqMtr = 8.75,
            AreaSqFeet = 94.2,
            HeightMtr = 3.0,
            HeightFt = 9.84,
            Shape = "Rectangle",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(5, entity.RoomWiseSubmissionId);
        Assert.Equal(3.5, entity.LengthMtr);
        Assert.Equal(11.48, entity.LengthFt);
        Assert.Equal(2.5, entity.WidthMtr);
        Assert.Equal(8.2, entity.WidthFt);
        Assert.Equal(8.75, entity.AreaSqMtr);
        Assert.Equal(94.2, entity.AreaSqFeet);
        Assert.Equal(3.0, entity.HeightMtr);
        Assert.Equal(9.84, entity.HeightFt);
        Assert.Equal("Rectangle", entity.Shape);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_AllNullableFieldsAreNull_MarkedForDeletionIsFalse()
    {
        var entity = new AssetRoomWiseMinusDataEntity();

        Assert.Null(entity.RoomWiseSubmissionId);
        Assert.Null(entity.LengthMtr);
        Assert.Null(entity.LengthFt);
        Assert.Null(entity.WidthMtr);
        Assert.Null(entity.WidthFt);
        Assert.Null(entity.AreaSqMtr);
        Assert.Null(entity.AreaSqFeet);
        Assert.Null(entity.HeightMtr);
        Assert.Null(entity.HeightFt);
        Assert.Null(entity.Shape);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_RoomWiseSubmissionDetailsNavigation_IsNull()
    {
        var entity = new AssetRoomWiseMinusDataEntity();

        Assert.Null(entity.RoomWiseSubmissionDetails);
    }

    [Fact]
    public void ImplementsIHardDeletable()
    {
        Assert.True(typeof(IHardDeletable).IsAssignableFrom(typeof(AssetRoomWiseMinusDataEntity)));
    }

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = new AssetRoomWiseMinusDataEntity();
        var now = DateTime.Now;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new AssetRoomWiseMinusDataEntity();

        Assert.True(entity.IsActive);
    }
}
