using System;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for AssetDetailsEntity - auxiliary location + KYC details for an asset (1:1 with
/// AssetMaster via AssetId; the DB PK is AssetId, not the inherited BaseEntity.Id).
/// Note: this entity has MarkedForDeletion/MarkedForDeletionDate fields but does NOT implement
/// IHardDeletable, unlike sibling entities in this same batch (AssetLeaseRentDetailsEntity,
/// AssetRoomWiseMinusDataEntity, etc.) that declare the interface explicitly - documented below,
/// not fixed, per the review-only scope for this file.
/// </summary>
public class AssetDetailsEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var deletionDate = DateTime.Now;
        var entity = new AssetDetailsEntity
        {
            Id = 1,
            AssetId = 10,
            OrganizationId = 20,
            ZoneId = 2,
            WardId = 3,
            MoujaId = 4,
            SubZoneId = 5,
            AssetWardNo = "W-1",
            PropertyNo = "P-1",
            PartitionNo = "PT-1",
            UpicId = "UPIC-1",
            PlotNo = "PL-1",
            CSN = "CSN-1",
            LandRate = 100.5m,
            LengthFt = 10m,
            LengthMtr = 3.05m,
            WidthFt = 8m,
            WidthMtr = 2.44m,
            LandAreaSqFeet = 80m,
            LandAreaSqMeter = 7.43m,
            Address = "123 Main St",
            NearestLandmark = "Near Park",
            PinCode = "123456",
            Latitude = 18.5204m,
            Longitude = 73.8567m,
            BoundaryGeoJson = "{}",
            InChargeName = "John Doe",
            InChargeRegionalName = "जॉन",
            InChargeDesignationId = 6,
            InChargeMobile = "9999999999",
            InChargeEmail = "john@example.com",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.AssetId);
        Assert.Equal(20, entity.OrganizationId);
        Assert.Equal(2, entity.ZoneId);
        Assert.Equal(3, entity.WardId);
        Assert.Equal(4, entity.MoujaId);
        Assert.Equal(5, entity.SubZoneId);
        Assert.Equal("W-1", entity.AssetWardNo);
        Assert.Equal("P-1", entity.PropertyNo);
        Assert.Equal("PT-1", entity.PartitionNo);
        Assert.Equal("UPIC-1", entity.UpicId);
        Assert.Equal("PL-1", entity.PlotNo);
        Assert.Equal("CSN-1", entity.CSN);
        Assert.Equal(100.5m, entity.LandRate);
        Assert.Equal(10m, entity.LengthFt);
        Assert.Equal(3.05m, entity.LengthMtr);
        Assert.Equal(8m, entity.WidthFt);
        Assert.Equal(2.44m, entity.WidthMtr);
        Assert.Equal(80m, entity.LandAreaSqFeet);
        Assert.Equal(7.43m, entity.LandAreaSqMeter);
        Assert.Equal("123 Main St", entity.Address);
        Assert.Equal("Near Park", entity.NearestLandmark);
        Assert.Equal("123456", entity.PinCode);
        Assert.Equal(18.5204m, entity.Latitude);
        Assert.Equal(73.8567m, entity.Longitude);
        Assert.Equal("{}", entity.BoundaryGeoJson);
        Assert.Equal("John Doe", entity.InChargeName);
        Assert.Equal("जॉन", entity.InChargeRegionalName);
        Assert.Equal(6, entity.InChargeDesignationId);
        Assert.Equal("9999999999", entity.InChargeMobile);
        Assert.Equal("john@example.com", entity.InChargeEmail);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_NullableFieldsAreNull_MarkedForDeletionIsFalse()
    {
        var entity = new AssetDetailsEntity();

        Assert.Null(entity.ZoneId);
        Assert.Null(entity.WardId);
        Assert.Null(entity.MoujaId);
        Assert.Null(entity.SubZoneId);
        Assert.Null(entity.LandRate);
        Assert.Null(entity.Latitude);
        Assert.Null(entity.Longitude);
        Assert.Null(entity.InChargeMobile);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_CompatibilityShimFields_AreDefaultUnset()
    {
        // These fields were dropped from AMS.AssetDetails in the re-architecture and are excluded
        // from the EF model (Fluent Ignore()) - kept only so legacy code referencing them compiles.
        var entity = new AssetDetailsEntity();

        Assert.Null(entity.CapitalValue);
        Assert.False(entity.HasLift);
        Assert.Null(entity.Length);
        Assert.Null(entity.Width);
        Assert.Null(entity.BuiltupAreaSqMeter);
        Assert.Null(entity.CarpetAreaSqMeter);
        Assert.Null(entity.GstNo);
        Assert.Null(entity.ShopActNo);
    }

    [Fact]
    public void Defaults_AssetNavigationProperty_IsNull()
    {
        // Declared `= null!` (non-nullable reference type) purely to satisfy the compiler for a
        // required EF navigation - the runtime default is still null until EF (or a test) sets it.
        var entity = new AssetDetailsEntity();

        Assert.Null(entity.Asset);
    }

    [Fact]
    public void InheritsBaseEntity_AuditColumnsAreAvailable()
    {
        var createdDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var entity = new AssetDetailsEntity
        {
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = createdDate,
            UpdatedBy = 200,
            UpdatedDate = updatedDate
        };

        Assert.True(entity.IsActive);
        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(createdDate, entity.CreatedDate);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.Equal(updatedDate, entity.UpdatedDate);
    }

    [Fact]
    public void DoesNotImplementIHardDeletable_DespiteHavingTheMatchingFields()
    {
        Assert.False(typeof(IHardDeletable).IsAssignableFrom(typeof(AssetDetailsEntity)));
    }
}
