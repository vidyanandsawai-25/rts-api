using NtisPlatform.Application.DTOs.Property;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Property;

public class PropertyDtoTests
{
    [Fact]
    public void DisplayProperty_BothPropertyNoAndPartitionNo_ReturnsCombined()
    {
        var dto = new PropertyDto { PropertyNo = "P123", PartitionNo = "PART1" };
        Assert.Equal("P123-PART1", dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_OnlyPropertyNo_ReturnsPropertyNo()
    {
        var dto = new PropertyDto { PropertyNo = "P123", PartitionNo = null };
        Assert.Equal("P123", dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_OnlyPartitionNo_ReturnsPartitionNoWithDash()
    {
        var dto = new PropertyDto { PropertyNo = null, PartitionNo = "PART1" };
        Assert.Equal("-PART1", dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_BothNull_ReturnsEmpty()
    {
        var dto = new PropertyDto { PropertyNo = null, PartitionNo = null };
        Assert.Equal(string.Empty, dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_PropertyNoEmpty_PartitionNoNull_ReturnsEmpty()
    {
        var dto = new PropertyDto { PropertyNo = "", PartitionNo = null };
        Assert.Equal(string.Empty, dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_PropertyNoWhitespace_PartitionNoNull_ReturnsEmpty()
    {
        var dto = new PropertyDto { PropertyNo = "   ", PartitionNo = null };
        Assert.Equal(string.Empty, dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_PropertyNoNull_PartitionNoEmpty_ReturnsEmpty()
    {
        var dto = new PropertyDto { PropertyNo = null, PartitionNo = "" };
        Assert.Equal(string.Empty, dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_PropertyNoNull_PartitionNoWhitespace_ReturnsEmpty()
    {
        var dto = new PropertyDto { PropertyNo = null, PartitionNo = "   " };
        Assert.Equal(string.Empty, dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_PropertyNoEmpty_PartitionNoValue_ReturnsDashAndPartitionNo()
    {
        var dto = new PropertyDto { PropertyNo = "", PartitionNo = "PART1" };
        Assert.Equal("-PART1", dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_PropertyNoValue_PartitionNoEmpty_ReturnsPropertyNo()
    {
        var dto = new PropertyDto { PropertyNo = "P123", PartitionNo = "" };
        Assert.Equal("P123", dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_BothEmptyStrings_ReturnsEmpty()
    {
        var dto = new PropertyDto { PropertyNo = "", PartitionNo = "" };
        Assert.Equal(string.Empty, dto.DisplayProperty);
    }

    [Fact]
    public void DisplayProperty_BothWhitespace_ReturnsEmpty()
    {
        var dto = new PropertyDto { PropertyNo = "   ", PartitionNo = "   " };
        Assert.Equal(string.Empty, dto.DisplayProperty);
    }

    [Fact]
    public void PropertyDto_AllProperties_CanBeSet()
    {
        var dto = new PropertyDto
        {
            Id = 1,
            TaxZoneId = 10,
            WardId = 20,
            PropertyNo = "P123",
            PartitionNo = "PART1",
            PropertyTypeId = 1,
            UPICId = "UPIC123",
            OpenPlot = true,
            CSN = "CSN001",
            SubZoneNo = "SZ01",
            PlotNo = "PLOT123",
            CategoryId = 5,
            Type = "Residential",
            OwnerTitle = "Mr.",
            OwnerName = "John Doe",
            OwnerTitleEnglish = "Mr.",
            OwnerNameEnglish = "John Doe",
            OccupierTitle = "Mrs.",
            OccupierName = "Jane Smith",
            OccupierTitleEnglish = "Mrs.",
            OccupierNameEnglish = "Jane Smith",
            FlatOrShopNo = "101",
            FlatOrShopName = "Shop Name",
            FlatOrShopNoEnglish = "101",
            FlatOrShopNameEnglish = "Shop Name",
            Address = "123 Main St",
            Location = "Downtown",
            AddressEnglish = "123 Main St",
            LocationEnglish = "Downtown",
            MobileNo = "9876543210",
            EmailId = "test@example.com",
            SocietyDetailId = 15,
            MarkedForDeletion = false
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.TaxZoneId);
        Assert.Equal(20, dto.WardId);
        Assert.Equal("P123", dto.PropertyNo);
        Assert.Equal("PART1", dto.PartitionNo);
        Assert.Equal(1, dto.PropertyTypeId);
        Assert.Equal("UPIC123", dto.UPICId);
        Assert.True(dto.OpenPlot);
        Assert.Equal("CSN001", dto.CSN);
        Assert.Equal("SZ01", dto.SubZoneNo);
        Assert.Equal("PLOT123", dto.PlotNo);
        Assert.Equal(5, dto.CategoryId);
        Assert.Equal("Residential", dto.Type);
        Assert.Equal("Mr.", dto.OwnerTitle);
        Assert.Equal("John Doe", dto.OwnerName);
        Assert.Equal("Mr.", dto.OwnerTitleEnglish);
        Assert.Equal("John Doe", dto.OwnerNameEnglish);
        Assert.Equal("Mrs.", dto.OccupierTitle);
        Assert.Equal("Jane Smith", dto.OccupierName);
        Assert.Equal("Mrs.", dto.OccupierTitleEnglish);
        Assert.Equal("Jane Smith", dto.OccupierNameEnglish);
        Assert.Equal("101", dto.FlatOrShopNo);
        Assert.Equal("Shop Name", dto.FlatOrShopName);
        Assert.Equal("101", dto.FlatOrShopNoEnglish);
        Assert.Equal("Shop Name", dto.FlatOrShopNameEnglish);
        Assert.Equal("123 Main St", dto.Address);
        Assert.Equal("Downtown", dto.Location);
        Assert.Equal("123 Main St", dto.AddressEnglish);
        Assert.Equal("Downtown", dto.LocationEnglish);
        Assert.Equal("9876543210", dto.MobileNo);
        Assert.Equal("test@example.com", dto.EmailId);
        Assert.Equal(15, dto.SocietyDetailId);
        Assert.False(dto.MarkedForDeletion);
    }

    [Fact]
    public void PropertyDto_DefaultValues()
    {
        var dto = new PropertyDto();

        Assert.Equal(0, dto.Id);
        Assert.Equal(0, dto.TaxZoneId);
        Assert.Equal(0, dto.WardId);
        Assert.False(dto.MarkedForDeletion);
        Assert.Null(dto.PropertyNo);
        Assert.Null(dto.PartitionNo);
        Assert.Null(dto.PropertyTypeId);
        Assert.Null(dto.UPICId);
        Assert.Null(dto.OpenPlot);
        Assert.Equal(string.Empty, dto.DisplayProperty);
    }
}
