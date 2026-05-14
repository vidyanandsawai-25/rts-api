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
            LengthMtr = 2.5m,
            WidthMtr = 1.5m,
            AreaSqMtr = 3.75m,
            HeightMtr = 0.5m,
            Shape = "Rectangle",
            Base1Mtr = 1.0m,
            Base2Mtr = 2.0m,
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
        Assert.Equal(2.5m, entity.LengthMtr);
        Assert.Equal(1.5m, entity.WidthMtr);
        Assert.Equal(3.75m, entity.AreaSqMtr);
        Assert.Equal(0.5m, entity.HeightMtr);
        Assert.Equal("Rectangle", entity.Shape);
        Assert.Equal(1.0m, entity.Base1Mtr);
        Assert.Equal(2.0m, entity.Base2Mtr);
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
            RoomType = "Bedroom"
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
            LengthMtr = 3.0m,
            WidthMtr = 2.0m,
            AreaSqMtr = 6.0m,
            HeightMtr = 1.0m
        };

        Assert.Equal(3.0m, entity.LengthMtr);
        Assert.Equal(2.0m, entity.WidthMtr);
        Assert.Equal(6.0m, entity.AreaSqMtr);
        Assert.Equal(1.0m, entity.HeightMtr);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_ShapeAndBases_WorksCorrectly()
    {
        var entity = new RoomWiseMinusDataEntity
        {
            Shape = "Trapezoid",
            Base1Mtr = 2.5m,
            Base2Mtr = 3.5m
        };

        Assert.Equal("Trapezoid", entity.Shape);
        Assert.Equal(2.5m, entity.Base1Mtr);
        Assert.Equal(3.5m, entity.Base2Mtr);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_AreaCalculation_Example()
    {
        var entity = new RoomWiseMinusDataEntity
        {
            RoomWiseSubmissionId = 100,
            LengthMtr = 5.0m,
            WidthMtr = 4.0m,
            Shape = "Rectangle"
        };

        var calculatedArea = entity.LengthMtr * entity.WidthMtr;
        entity.AreaSqMtr = calculatedArea;

        Assert.Equal(20.0m, entity.AreaSqMtr);
    }

    [Fact]
    public void RoomWiseMinusDataEntity_MultipleMinusAreas_CanBeCreated()
    {
        var minus1 = new RoomWiseMinusDataEntity
        {
            Id = 1,
            RoomWiseSubmissionId = 100,
            LengthMtr = 1.0m,
            WidthMtr = 1.0m,
            AreaSqMtr = 1.0m,
            Shape = "Square"
        };

        var minus2 = new RoomWiseMinusDataEntity
        {
            Id = 2,
            RoomWiseSubmissionId = 100,
            LengthMtr = 2.0m,
            WidthMtr = 1.5m,
            AreaSqMtr = 3.0m,
            Shape = "Rectangle"
        };

        Assert.Equal(100, minus1.RoomWiseSubmissionId);
        Assert.Equal(100, minus2.RoomWiseSubmissionId);
        Assert.Equal(1.0m, minus1.AreaSqMtr);
        Assert.Equal(3.0m, minus2.AreaSqMtr);
    }
}
