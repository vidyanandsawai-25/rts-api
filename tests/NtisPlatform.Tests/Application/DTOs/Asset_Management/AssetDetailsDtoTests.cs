using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDetails;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for AssetDetailsDto / CreateAssetDetailsDto / UpdateAssetDetailsDto - auxiliary
/// location + KYC details for an asset (1:1 with AssetMaster).
/// </summary>
public class AssetDetailsDtoTests
{
    #region AssetDetailsDto (read)

    [Fact]
    public void AssetDetailsDto_PropertiesGetAndSetCorrectly()
    {
        var deletionDate = DateTime.Now;
        var dto = new AssetDetailsDto
        {
            Id = 1,
            IsActive = true,
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
            InChargeDesignationId = 6,
            InChargeDesignationName = "Manager",
            InChargeMobile = "9999999999",
            InChargeEmail = "john@example.com",
            InChargeRegionalName = "जॉन",
            MarkedForDeletion = true,
            MarkedForDeletionDate = deletionDate,
            ZoneName = "Zone A",
            WardName = "Ward 1",
            MoujaName = "Mouja X",
            SubZoneName = "SubZone Y"
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(10, dto.AssetId);
        Assert.Equal(20, dto.OrganizationId);
        Assert.Equal(2, dto.ZoneId);
        Assert.Equal(3, dto.WardId);
        Assert.Equal(4, dto.MoujaId);
        Assert.Equal(5, dto.SubZoneId);
        Assert.Equal("W-1", dto.AssetWardNo);
        Assert.Equal("P-1", dto.PropertyNo);
        Assert.Equal("PT-1", dto.PartitionNo);
        Assert.Equal("UPIC-1", dto.UpicId);
        Assert.Equal("PL-1", dto.PlotNo);
        Assert.Equal("CSN-1", dto.CSN);
        Assert.Equal(100.5m, dto.LandRate);
        Assert.Equal(10m, dto.LengthFt);
        Assert.Equal(3.05m, dto.LengthMtr);
        Assert.Equal(8m, dto.WidthFt);
        Assert.Equal(2.44m, dto.WidthMtr);
        Assert.Equal(80m, dto.LandAreaSqFeet);
        Assert.Equal(7.43m, dto.LandAreaSqMeter);
        Assert.Equal("123 Main St", dto.Address);
        Assert.Equal("Near Park", dto.NearestLandmark);
        Assert.Equal("123456", dto.PinCode);
        Assert.Equal(18.5204m, dto.Latitude);
        Assert.Equal(73.8567m, dto.Longitude);
        Assert.Equal("{}", dto.BoundaryGeoJson);
        Assert.Equal("John Doe", dto.InChargeName);
        Assert.Equal(6, dto.InChargeDesignationId);
        Assert.Equal("Manager", dto.InChargeDesignationName);
        Assert.Equal("9999999999", dto.InChargeMobile);
        Assert.Equal("john@example.com", dto.InChargeEmail);
        Assert.Equal("जॉन", dto.InChargeRegionalName);
        Assert.True(dto.MarkedForDeletion);
        Assert.Equal(deletionDate, dto.MarkedForDeletionDate);
        Assert.Equal("Zone A", dto.ZoneName);
        Assert.Equal("Ward 1", dto.WardName);
        Assert.Equal("Mouja X", dto.MoujaName);
        Assert.Equal("SubZone Y", dto.SubZoneName);
    }

    [Fact]
    public void AssetDetailsDto_Defaults_NullableFieldsAreNull()
    {
        var dto = new AssetDetailsDto();

        Assert.Null(dto.ZoneId);
        Assert.Null(dto.WardId);
        Assert.Null(dto.MoujaId);
        Assert.Null(dto.SubZoneId);
        Assert.Null(dto.LandRate);
        Assert.Null(dto.Latitude);
        Assert.Null(dto.Longitude);
        Assert.False(dto.MarkedForDeletion);
        Assert.Null(dto.MarkedForDeletionDate);
    }

    #endregion

    #region CreateAssetDetailsDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetDetailsDto { AssetId = 1, OrganizationId = 1 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithZeroAssetId_IsInvalid()
    {
        var dto = new CreateAssetDetailsDto { AssetId = 0, OrganizationId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDetailsDto.AssetId))
            && r.ErrorMessage == "AMS_AssetDetails_AssetId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroOrganizationId_IsInvalid()
    {
        // OrganizationId is a non-nullable int, so [Required] never actually fires (a value type
        // can't be "missing"); omitting it just leaves it at the CLR default 0, which [Range(1, ...)]
        // then rejects. Documenting the real validation path rather than the attribute that looks
        // like it should fire.
        var dto = new CreateAssetDetailsDto { AssetId = 1, OrganizationId = 0 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDetailsDto.OrganizationId))
            && r.ErrorMessage == "AMS_AssetDetails_OrganizationId_InvalidRange");
    }

    [Fact]
    public void Create_WithInvalidPinCode_IsInvalid()
    {
        var dto = new CreateAssetDetailsDto { AssetId = 1, OrganizationId = 1, PinCode = "12A45" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDetailsDto.PinCode))
            && r.ErrorMessage == "AMS_AssetDetails_PinCode_Invalid");
    }

    [Fact]
    public void Create_WithValidPinCode_IsValid()
    {
        var dto = new CreateAssetDetailsDto { AssetId = 1, OrganizationId = 1, PinCode = "411001" };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithInvalidEmail_IsInvalid()
    {
        var dto = new CreateAssetDetailsDto { AssetId = 1, OrganizationId = 1, InChargeEmail = "not-an-email" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDetailsDto.InChargeEmail))
            && r.ErrorMessage == "AMS_AssetDetails_InChargeEmail_Invalid");
    }

    [Fact]
    public void Create_WithAddressExceeding500Characters_IsInvalid()
    {
        var dto = new CreateAssetDetailsDto { AssetId = 1, OrganizationId = 1, Address = new string('A', 501) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDetailsDto.Address))
            && r.ErrorMessage == "AMS_AssetDetails_Address_MaxLengthExceeded_500");
    }

    [Fact]
    public void Create_WithLatitudeOutOfRange_IsInvalid()
    {
        var dto = new CreateAssetDetailsDto { AssetId = 1, OrganizationId = 1, Latitude = 91m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDetailsDto.Latitude))
            && r.ErrorMessage == "AMS_AssetDetails_Latitude_InvalidRange");
    }

    [Fact]
    public void Create_WithLongitudeOutOfRange_IsInvalid()
    {
        var dto = new CreateAssetDetailsDto { AssetId = 1, OrganizationId = 1, Longitude = 181m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDetailsDto.Longitude))
            && r.ErrorMessage == "AMS_AssetDetails_Longitude_InvalidRange");
    }

    [Fact]
    public void Create_WithAllOptionalFieldsNull_IsValid()
    {
        // Every field besides AssetId/OrganizationId is genuinely optional.
        var dto = new CreateAssetDetailsDto { AssetId = 1, OrganizationId = 1 };

        Assert.Empty(ValidateModel(dto));
    }

    #endregion

    #region UpdateAssetDetailsDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetDetailsDto { OrganizationId = 1 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithZeroOrganizationId_IsInvalid()
    {
        // See the Create-side note: [Required] can't fire on a non-nullable int, so the
        // default-value case is actually caught by [Range(1, ...)].
        var dto = new UpdateAssetDetailsDto { OrganizationId = 0 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetDetailsDto.OrganizationId))
            && r.ErrorMessage == "AMS_AssetDetails_OrganizationId_InvalidRange");
    }

    [Fact]
    public void Update_WithInvalidPinCode_IsInvalid()
    {
        var dto = new UpdateAssetDetailsDto { OrganizationId = 1, PinCode = "1234" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetDetailsDto.PinCode))
            && r.ErrorMessage == "AMS_AssetDetails_PinCode_Invalid");
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
