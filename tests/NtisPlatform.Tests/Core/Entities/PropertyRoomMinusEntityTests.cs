using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Unit tests for RoomWiseMinusDataEntity
/// </summary>
public class RoomWiseMinusDataEntityTests
{
    [Fact]
    public void RoomWiseMinusDataEntity_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new RoomWiseMinusDataEntity
        {
            Id = 1,
            RoomWiseSubmissionId = 100,
            LengthMtr = 2.5,
            WidthMtr = 1.5,
            AreaSqMtr = 3.75,
            HeightMtr = 0.5,
            Shape = "Rectangle",
            Base1Mtr = 1.0,
            Base2Mtr = 2.0,
            IsOffset = true,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(100, entity.RoomWiseSubmissionId);
        Assert.Equal(2.5, entity.LengthMtr);
        Assert.Equal(1.5, entity.WidthMtr);
        Assert.Equal(3.75, entity.AreaSqMtr);
        Assert.Equal(0.5, entity.HeightMtr);
        Assert.Equal("Rectangle", entity.Shape);
        Assert.Equal(1.0, entity.Base1Mtr);
        Assert.Equal(2.0, entity.Base2Mtr);
        Assert.True(entity.IsOffset);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_OptionalProperties_CanBeNull()
    {
        var entity = new RoomWiseMinusDataEntity
        {
            RoomWiseSubmissionId = 100,
            IsActive = true
        };

        Assert.Null(entity.LengthMtr);
        Assert.Null(entity.WidthMtr);
        Assert.Null(entity.AreaSqMtr);
        Assert.Null(entity.HeightMtr);
        Assert.Null(entity.Shape);
        Assert.Null(entity.Base1Mtr);
        Assert.Null(entity.Base2Mtr);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_MarkedForDeletion_DefaultsToFalse()
    {
        var entity = new RoomWiseMinusDataEntity();
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_IsOffset_DefaultsToFalse()
    {
        var entity = new RoomWiseMinusDataEntity();
        Assert.False(entity.IsOffset);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_IsOffset_CanBeSetToTrue()
    {
        var entity = new RoomWiseMinusDataEntity
        {
            IsOffset = true
        };
        Assert.True(entity.IsOffset);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_IsOffset_CanBeSetToFalse()
    {
        var entity = new RoomWiseMinusDataEntity
        {
            IsOffset = true // Set to true first
        };
        entity.IsOffset = false; // Then set to false
        Assert.False(entity.IsOffset);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_NavigationProperty_RoomWiseSubmissionDetails_CanBeSet()
    {
        var minusEntity = new RoomWiseMinusDataEntity
        {
            Id = 1,
            RoomWiseSubmissionId = 100
        };

        var roomEntity = new RoomWiseSubmissionDetailsEntity
        {
            Id = 100,
            PropertyDetailsId = 200,
            RoomNo = "R-101",
            RoomTypeId = 1
        };

        minusEntity.RoomWiseSubmissionDetails = roomEntity;

        Assert.NotNull(minusEntity.RoomWiseSubmissionDetails);
        Assert.Equal(100, minusEntity.RoomWiseSubmissionDetails.Id);
        Assert.Equal("R-101", minusEntity.RoomWiseSubmissionDetails.RoomNo);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_Dimensions_WorksCorrectly()
    {
        var entity = new RoomWiseMinusDataEntity
        {
            LengthMtr = 3.0,
            WidthMtr = 2.0,
            AreaSqMtr = 6.0,
            HeightMtr = 1.0
        };

        Assert.Equal(3.0, entity.LengthMtr);
        Assert.Equal(2.0, entity.WidthMtr);
        Assert.Equal(6.0, entity.AreaSqMtr);
        Assert.Equal(1.0, entity.HeightMtr);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_ShapeAndBases_WorksCorrectly()
    {
        var entity = new RoomWiseMinusDataEntity
        {
            Shape = "Trapezoid",
            Base1Mtr = 2.5,
            Base2Mtr = 3.5
        };

        Assert.Equal("Trapezoid", entity.Shape);
        Assert.Equal(2.5, entity.Base1Mtr);
        Assert.Equal(3.5, entity.Base2Mtr);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_AreaCalculation_Example()
    {
        var entity = new RoomWiseMinusDataEntity
        {
            RoomWiseSubmissionId = 100,
            LengthMtr = 5.0,
            WidthMtr = 4.0,
            Shape = "Rectangle"
        };

        var calculatedArea = entity.LengthMtr * entity.WidthMtr;
        entity.AreaSqMtr = calculatedArea;

        Assert.Equal(20.0, entity.AreaSqMtr);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_MultipleMinusAreas_CanBeCreated()
    {
        var minus1 = new RoomWiseMinusDataEntity
        {
            Id = 1,
            RoomWiseSubmissionId = 100,
            LengthMtr = 1.0,
            WidthMtr = 1.0,
            AreaSqMtr = 1.0,
            Shape = "Square"
        };

        var minus2 = new RoomWiseMinusDataEntity
        {
            Id = 2,
            RoomWiseSubmissionId = 100,
            LengthMtr = 2.0,
            WidthMtr = 1.5,
            AreaSqMtr = 3.0,
            Shape = "Rectangle"
        };

        Assert.Equal(100, minus1.RoomWiseSubmissionId);
        Assert.Equal(100, minus2.RoomWiseSubmissionId);
        Assert.Equal(1.0, minus1.AreaSqMtr);
        Assert.Equal(3.0, minus2.AreaSqMtr);
    }
}
