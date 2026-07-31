using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for ShopWiseDetailsDto - a flat, denormalized read shape combining shop/asset and renter
/// information for shop-wise listing views. No DataAnnotations; round-trip + defaults only.
/// </summary>
public class ShopWiseDetailsDtoTests
{
    [Fact]
    public void ShopWiseDetailsDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new ShopWiseDetailsDto
        {
            SerialNo = 1,
            AssetId = "10",
            Floor = "Ground Floor",
            ShopNo = "S-1",
            ShopName = "Shop One",
            Area = 250.5m,
            Occupier = "John Doe",
            Contact = "9999999999",
            AnnualRent = 60000m,
            AgreementPeriod = "2023-2028",
            Status = "Active",
            Condition = "Good"
        };

        Assert.Equal(1, dto.SerialNo);
        Assert.Equal("10", dto.AssetId);
        Assert.Equal("Ground Floor", dto.Floor);
        Assert.Equal("S-1", dto.ShopNo);
        Assert.Equal("Shop One", dto.ShopName);
        Assert.Equal(250.5m, dto.Area);
        Assert.Equal("John Doe", dto.Occupier);
        Assert.Equal("9999999999", dto.Contact);
        Assert.Equal(60000m, dto.AnnualRent);
        Assert.Equal("2023-2028", dto.AgreementPeriod);
        Assert.Equal("Active", dto.Status);
        Assert.Equal("Good", dto.Condition);
    }

    [Fact]
    public void ShopWiseDetailsDto_Defaults_AllOptionalFieldsAreNull_SerialNoIsZero()
    {
        var dto = new ShopWiseDetailsDto();

        Assert.Equal(0, dto.SerialNo);
        Assert.Null(dto.AssetId);
        Assert.Null(dto.Floor);
        Assert.Null(dto.ShopNo);
        Assert.Null(dto.ShopName);
        Assert.Null(dto.Area);
        Assert.Null(dto.Occupier);
        Assert.Null(dto.Contact);
        Assert.Null(dto.AnnualRent);
        Assert.Null(dto.AgreementPeriod);
        Assert.Null(dto.Status);
        Assert.Null(dto.Condition);
    }
}
