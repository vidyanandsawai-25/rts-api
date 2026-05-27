using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Unit tests for RoomWiseSubmissionDetailsEntity
/// </summary>
public class RoomWiseSubmissionDetailsEntityTests
{
    [Fact]
    public void RoomWiseSubmissionDetailsEntity_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new RoomWiseSubmissionDetailsEntity
        {
            Id = 1,
            PropertyDetailsId = 100,
            PropertyId = 549357,
            LengthMtr = 10.5,
            WidthMtr = 8.0,
            AreaSqMtr = 84.0,
            HeightMtr = 3.5,
            Base1Mtr = 5.0,
            Base2Mtr = 7.0,
            NoOfRooms = 3,
            TotalAreaSqMtr = 252.0,
            Shape = "Rectangle",
            RoomNo = "R-101",
            OuterYesNo = true,
            RoomTypeId = 1,
            SubmissionType = "Initial",
            MinusYesNo = true,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(100, entity.PropertyDetailsId);
        Assert.Equal(549357, entity.PropertyId);
        Assert.Equal(10.5, entity.LengthMtr);
        Assert.Equal(8.0, entity.WidthMtr);
        Assert.Equal(84.0, entity.AreaSqMtr);
        Assert.Equal(3.5, entity.HeightMtr);
        Assert.Equal(5.0, entity.Base1Mtr);
        Assert.Equal(7.0, entity.Base2Mtr);
        Assert.Equal(3, entity.NoOfRooms);
        Assert.Equal(252.0, entity.TotalAreaSqMtr);
        Assert.Equal("Rectangle", entity.Shape);
        Assert.Equal("R-101", entity.RoomNo);
        Assert.True(entity.OuterYesNo);
        Assert.Equal(1, entity.RoomTypeId);
        Assert.Equal("Initial", entity.SubmissionType);
        Assert.True(entity.MinusYesNo);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void RoomWiseSubmissionDetailsEntity_OptionalProperties_CanBeNull()
    {
        var entity = new RoomWiseSubmissionDetailsEntity
        {
            PropertyDetailsId = 100,
            IsActive = true
        };

        Assert.Null(entity.PropertyId);
        Assert.Null(entity.LengthMtr);
        Assert.Null(entity.WidthMtr);
        Assert.Null(entity.AreaSqMtr);
        Assert.Null(entity.HeightMtr);
        Assert.Null(entity.Base1Mtr);
        Assert.Null(entity.Base2Mtr);
        Assert.Null(entity.NoOfRooms);
        Assert.Null(entity.TotalAreaSqMtr);
        Assert.Null(entity.Shape);
        Assert.Null(entity.RoomNo);
        Assert.Null(entity.RoomTypeId);
        Assert.Null(entity.SubmissionType);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void RoomWiseSubmissionDetailsEntity_BooleanProperties_DefaultToFalse()
    {
        var entity = new RoomWiseSubmissionDetailsEntity();

        Assert.False(entity.OuterYesNo);
        Assert.False(entity.MinusYesNo);
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void RoomWiseSubmissionDetailsEntity_NavigationProperty_PropertyDetails_CanBeSet()
    {
        var roomEntity = new RoomWiseSubmissionDetailsEntity
        {
            Id = 1,
            PropertyDetailsId = 100
        };

        var propertyDetailsEntity = new PropertyDetailsEntity
        {
            Id = 100,
            PropertyId = 549357,
            FloorId = 2,
            ConstructionTypeId = 3,
            TypeOfUseId = 4
        };

        roomEntity.PropertyDetails = propertyDetailsEntity;

        Assert.NotNull(roomEntity.PropertyDetails);
        Assert.Equal(100, roomEntity.PropertyDetails.Id);
        Assert.Equal(549357, roomEntity.PropertyDetails.PropertyId);
    }

    [Fact]
    public void RoomWiseSubmissionDetailsEntity_NavigationProperty_PropertyMast_CanBeSet()
    {
        var roomEntity = new RoomWiseSubmissionDetailsEntity
        {
            Id = 1,
            PropertyId = 549357
        };

        var propertyEntity = new PropertyEntity
        {
            Id = 549357,
            TaxZoneId = 1,
            WardId = 5
        };

        roomEntity.PropertyMast = propertyEntity;

        Assert.NotNull(roomEntity.PropertyMast);
        Assert.Equal(549357, roomEntity.PropertyMast.Id);
    }

    [Fact]
    public void RoomWiseSubmissionDetailsEntity_CollectionNavigation_PropertyRoomMinus_CanBeSet()
    {
        var roomEntity = new RoomWiseSubmissionDetailsEntity
        {
            Id = 1,
            PropertyDetailsId = 100
        };

        var minusEntity1 = new RoomWiseMinusDataEntity
        {
            Id = 1,
            RoomWiseSubmissionId = 1,
            LengthMtr = 2.0,
            WidthMtr = 1.5
        };

        var minusEntity2 = new RoomWiseMinusDataEntity
        {
            Id = 2,
            RoomWiseSubmissionId = 1,
            LengthMtr = 1.0,
            WidthMtr = 1.0
        };

        roomEntity.PropertyRoomMinus = new List<RoomWiseMinusDataEntity> { minusEntity1, minusEntity2 };

        Assert.NotNull(roomEntity.PropertyRoomMinus);
        Assert.Equal(2, roomEntity.PropertyRoomMinus.Count);
    }

    [Fact]
    public void RoomWiseSubmissionDetailsEntity_Dimensions_WorksCorrectly()
    {
        var entity = new RoomWiseSubmissionDetailsEntity
        {
            LengthMtr = 12.5,
            WidthMtr = 10.0,
            HeightMtr = 3.5,
            AreaSqMtr = 125.0
        };

        Assert.Equal(12.5, entity.LengthMtr);
        Assert.Equal(10.0, entity.WidthMtr);
        Assert.Equal(3.5, entity.HeightMtr);
        Assert.Equal(125.0, entity.AreaSqMtr);
    }

    [Fact]
    public void RoomWiseSubmissionDetailsEntity_BaseDimensions_WorksCorrectly()
    {
        var entity = new RoomWiseSubmissionDetailsEntity
        {
            Base1Mtr = 5.5,
            Base2Mtr = 7.5,
            Shape = "Trapezoid"
        };

        Assert.Equal(5.5, entity.Base1Mtr);
        Assert.Equal(7.5, entity.Base2Mtr);
        Assert.Equal("Trapezoid", entity.Shape);
    }

    [Fact]
    public void RoomWiseSubmissionDetailsEntity_RoomDetails_WorksCorrectly()
    {
        var entity = new RoomWiseSubmissionDetailsEntity
        {
            RoomNo = "R-205",
            RoomTypeId = 2,
            NoOfRooms = 1,
            TotalAreaSqMtr = 150.0
        };

        Assert.Equal("R-205", entity.RoomNo);
        Assert.Equal(2, entity.RoomTypeId);
        Assert.Equal(1, entity.NoOfRooms);
        Assert.Equal(150.0, entity.TotalAreaSqMtr);
    }

    [Fact]
    public void RoomWiseSubmissionDetailsEntity_SubmissionFlags_WorksCorrectly()
    {
        var entity = new RoomWiseSubmissionDetailsEntity
        {
            SubmissionType = "Final",
            OuterYesNo = true,
            MinusYesNo = false
        };

        Assert.Equal("Final", entity.SubmissionType);
        Assert.True(entity.OuterYesNo);
        Assert.False(entity.MinusYesNo);
    }
}
