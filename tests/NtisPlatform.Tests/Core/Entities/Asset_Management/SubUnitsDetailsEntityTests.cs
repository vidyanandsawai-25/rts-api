using System;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for SubUnitsDetailsEntity - floor-wise details of building assets including
/// construction, usage, and valuation information (AMS.SubUnitsDetails).
/// </summary>
public class SubUnitsDetailsEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var deletionDate = DateTime.Now;
        var entity = new SubUnitsDetailsEntity
        {
            Id = 1,
            AssetId = 10,
            FloorId = 2,
            SubFloorId = 3,
            ConstructionYear = "2010",
            AssessmentYear = "2026",
            ConstructionTypeId = 4,
            TypeOfUseId = 5,
            SubTypeOfUseId = 6,
            CarpetAreaSqMeter = 50m,
            CarpetAreaSqFeet = 538m,
            BuiltUpAreaSqMeter = 60m,
            BuiltUpAreaSqFeet = 646m,
            NoOfRooms = 3,
            CVAgeFactor = 0.9m,
            CVFloorFactor = 1.0m,
            CVNatureFactor = 1.1m,
            CVUseFactor = 1.2m,
            CVBaseRate = 1000m,
            BaseValue = 50000m,
            CapitalValue = 45000m,
            IsRented = true,
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.AssetId);
        Assert.Equal(2, entity.FloorId);
        Assert.Equal(3, entity.SubFloorId);
        Assert.Equal("2010", entity.ConstructionYear);
        Assert.Equal("2026", entity.AssessmentYear);
        Assert.Equal(4, entity.ConstructionTypeId);
        Assert.Equal(5, entity.TypeOfUseId);
        Assert.Equal(6, entity.SubTypeOfUseId);
        Assert.Equal(50m, entity.CarpetAreaSqMeter);
        Assert.Equal(538m, entity.CarpetAreaSqFeet);
        Assert.Equal(60m, entity.BuiltUpAreaSqMeter);
        Assert.Equal(646m, entity.BuiltUpAreaSqFeet);
        Assert.Equal(3, entity.NoOfRooms);
        Assert.Equal(0.9m, entity.CVAgeFactor);
        Assert.Equal(1.0m, entity.CVFloorFactor);
        Assert.Equal(1.1m, entity.CVNatureFactor);
        Assert.Equal(1.2m, entity.CVUseFactor);
        Assert.Equal(1000m, entity.CVBaseRate);
        Assert.Equal(50000m, entity.BaseValue);
        Assert.Equal(45000m, entity.CapitalValue);
        Assert.True(entity.IsRented);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_NullableFieldsAreNull_MarkedForDeletionIsFalse()
    {
        var entity = new SubUnitsDetailsEntity();

        Assert.Null(entity.SubFloorId);
        Assert.Null(entity.ConstructionYear);
        Assert.Null(entity.AssessmentYear);
        Assert.Null(entity.SubTypeOfUseId);
        Assert.Null(entity.CarpetAreaSqMeter);
        Assert.Null(entity.NoOfRooms);
        Assert.Null(entity.CapitalValue);
        Assert.Null(entity.IsRented);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_NonNullableForeignKeys_DefaultToZero()
    {
        var entity = new SubUnitsDetailsEntity();

        Assert.Equal(0, entity.AssetId);
        Assert.Equal(0, entity.FloorId);
        Assert.Equal(0, entity.ConstructionTypeId);
        Assert.Equal(0, entity.TypeOfUseId);
    }

    [Fact]
    public void Defaults_NavigationProperties_AreAllNull()
    {
        var entity = new SubUnitsDetailsEntity();

        Assert.Null(entity.Asset);
        Assert.Null(entity.Floor);
        Assert.Null(entity.SubFloor);
        Assert.Null(entity.ConstructionType);
        Assert.Null(entity.TypeOfUse);
        Assert.Null(entity.SubTypeOfUse);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new SubUnitsDetailsEntity();

        Assert.True(entity.IsActive);
    }

    [Fact]
    public void DoesNotImplementIHardDeletable_DespiteHavingTheMatchingFields()
    {
        Assert.False(typeof(IHardDeletable).IsAssignableFrom(typeof(SubUnitsDetailsEntity)));
    }
}
