using NtisPlatform.Application.DTOs.Property;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Property;

/// <summary>
/// Comprehensive tests for Property DTOs to achieve 100% code coverage
/// Tests CreatePropertyDto and UpdatePropertyDto property setters with trim logic
/// </summary>
public class PropertyDtosTests
{
    #region PropertyDto Tests

    [Theory]
    [InlineData("PROP123", "PART456", "PROP123-PART456")]
    [InlineData("PROP123", null, "PROP123")]
    [InlineData(null, "PART456", "-PART456")]
    [InlineData(null, null, "")]
    public void PropertyDto_DisplayProperty_ReturnsExpectedValue(string? propertyNo, string? partitionNo, string expected)
    {
        // Arrange
        var dto = new PropertyDto
        {
            PropertyNo = propertyNo,
            PartitionNo = partitionNo
        };

        // Act
        var result = dto.DisplayProperty;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PropertyDto_DisplayProperty_WithEmptyPropertyNoAndPartitionNo_ReturnsEmpty()
    {
        // Arrange
        var dto = new PropertyDto
        {
            PropertyNo = "",
            PartitionNo = ""
        };

        // Act
        var result = dto.DisplayProperty;

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void PropertyDto_DisplayProperty_WithWhitespacePropertyNo_ReturnsEmpty()
    {
        // Arrange
        var dto = new PropertyDto
        {
            PropertyNo = "   ",
            PartitionNo = null
        };

        // Act
        var result = dto.DisplayProperty;

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region CreatePropertyDto Auto-Trim Tests

    [Fact]
    public void CreatePropertyDto_PropertyNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { PropertyNo = "  PROP123  " };

        // Assert
        Assert.Equal("PROP123", dto.PropertyNo);
    }

    [Fact]
    public void CreatePropertyDto_PropertyNo_ConvertsWhitespaceToNull()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { PropertyNo = "   " };

        // Assert
        Assert.Null(dto.PropertyNo);
    }

    [Fact]
    public void CreatePropertyDto_PartitionNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { PartitionNo = "  PART456  " };

        // Assert
        Assert.Equal("PART456", dto.PartitionNo);
    }

    [Fact]
    public void CreatePropertyDto_PartitionNo_ConvertsWhitespaceToNull()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { PartitionNo = "   " };

        // Assert
        Assert.Null(dto.PartitionNo);
    }

    [Fact]
    public void CreatePropertyDto_UPICId_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { UPICId = "  UPIC123  " };

        // Assert
        Assert.Equal("UPIC123", dto.UPICId);
    }

    [Fact]
    public void CreatePropertyDto_CSN_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { CSN = "  CSN123  " };

        // Assert
        Assert.Equal("CSN123", dto.CSN);
    }

    [Fact]
    public void CreatePropertyDto_SubZoneNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { SubZoneNo = "  SZ123  " };

        // Assert
        Assert.Equal("SZ123", dto.SubZoneNo);
    }

    [Fact]
    public void CreatePropertyDto_PlotNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { PlotNo = "  PLOT123  " };

        // Assert
        Assert.Equal("PLOT123", dto.PlotNo);
    }

    [Fact]
    public void CreatePropertyDto_PartType_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { PartType = "  TypeA  " };

        // Assert
        Assert.Equal("TypeA", dto.PartType);
    }

    [Fact]
    public void CreatePropertyDto_OwnerTitle_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { OwnerTitle = "  Mr.  " };

        // Assert
        Assert.Equal("Mr.", dto.OwnerTitle);
    }

    [Fact]
    public void CreatePropertyDto_OwnerName_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { OwnerName = "  John Doe  " };

        // Assert
        Assert.Equal("John Doe", dto.OwnerName);
    }

    [Fact]
    public void CreatePropertyDto_OwnerTitleEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { OwnerTitleEnglish = "  Mr.  " };

        // Assert
        Assert.Equal("Mr.", dto.OwnerTitleEnglish);
    }

    [Fact]
    public void CreatePropertyDto_OwnerNameEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { OwnerNameEnglish = "  John Doe  " };

        // Assert
        Assert.Equal("John Doe", dto.OwnerNameEnglish);
    }

    [Fact]
    public void CreatePropertyDto_OccupierTitle_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { OccupierTitle = "  Mrs.  " };

        // Assert
        Assert.Equal("Mrs.", dto.OccupierTitle);
    }

    [Fact]
    public void CreatePropertyDto_OccupierName_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { OccupierName = "  Jane Doe  " };

        // Assert
        Assert.Equal("Jane Doe", dto.OccupierName);
    }

    [Fact]
    public void CreatePropertyDto_OccupierTitleEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { OccupierTitleEnglish = "  Mrs.  " };

        // Assert
        Assert.Equal("Mrs.", dto.OccupierTitleEnglish);
    }

    [Fact]
    public void CreatePropertyDto_OccupierNameEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { OccupierNameEnglish = "  Jane Doe  " };

        // Assert
        Assert.Equal("Jane Doe", dto.OccupierNameEnglish);
    }

    [Fact]
    public void CreatePropertyDto_FlatOrShopNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { FlatOrShopNo = "  101  " };

        // Assert
        Assert.Equal("101", dto.FlatOrShopNo);
    }

    [Fact]
    public void CreatePropertyDto_FlatOrShopName_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { FlatOrShopName = "  Shop Name  " };

        // Assert
        Assert.Equal("Shop Name", dto.FlatOrShopName);
    }

    [Fact]
    public void CreatePropertyDto_FlatOrShopNoEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { FlatOrShopNoEnglish = "  101A  " };

        // Assert
        Assert.Equal("101A", dto.FlatOrShopNoEnglish);
    }

    [Fact]
    public void CreatePropertyDto_FlatOrShopNameEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { FlatOrShopNameEnglish = "  Shop Name  " };

        // Assert
        Assert.Equal("Shop Name", dto.FlatOrShopNameEnglish);
    }

    [Fact]
    public void CreatePropertyDto_Address_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { Address = "  123 Main St  " };

        // Assert
        Assert.Equal("123 Main St", dto.Address);
    }

    [Fact]
    public void CreatePropertyDto_Location_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { Location = "  Downtown  " };

        // Assert
        Assert.Equal("Downtown", dto.Location);
    }

    [Fact]
    public void CreatePropertyDto_AddressEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { AddressEnglish = "  123 Main St  " };

        // Assert
        Assert.Equal("123 Main St", dto.AddressEnglish);
    }

    [Fact]
    public void CreatePropertyDto_LocationEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { LocationEnglish = "  Downtown  " };

        // Assert
        Assert.Equal("Downtown", dto.LocationEnglish);
    }

    [Fact]
    public void CreatePropertyDto_MobileNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { MobileNo = "  1234567890  " };

        // Assert
        Assert.Equal("1234567890", dto.MobileNo);
    }

    [Fact]
    public void CreatePropertyDto_EmailId_TrimsWhitespaceAndConvertsToLower()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { EmailId = "  Test@Example.COM  " };

        // Assert
        Assert.Equal("test@example.com", dto.EmailId);
    }

    [Fact]
    public void CreatePropertyDto_EmailId_ConvertsWhitespaceToNull()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto { EmailId = "   " };

        // Assert
        Assert.Null(dto.EmailId);
    }

    #endregion

    #region UpdatePropertyDto Auto-Trim Tests

    [Fact]
    public void UpdatePropertyDto_PropertyNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { PropertyNo = "  PROP123  " };

        // Assert
        Assert.Equal("PROP123", dto.PropertyNo);
    }

    [Fact]
    public void UpdatePropertyDto_PropertyNo_ConvertsWhitespaceToNull()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { PropertyNo = "   " };

        // Assert
        Assert.Null(dto.PropertyNo);
    }

    [Fact]
    public void UpdatePropertyDto_PartitionNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { PartitionNo = "  PART456  " };

        // Assert
        Assert.Equal("PART456", dto.PartitionNo);
    }

    [Fact]
    public void UpdatePropertyDto_UPICId_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { UPICId = "  UPIC123  " };

        // Assert
        Assert.Equal("UPIC123", dto.UPICId);
    }

    [Fact]
    public void UpdatePropertyDto_CSN_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { CSN = "  CSN123  " };

        // Assert
        Assert.Equal("CSN123", dto.CSN);
    }

    [Fact]
    public void UpdatePropertyDto_SubZoneNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { SubZoneNo = "  SZ123  " };

        // Assert
        Assert.Equal("SZ123", dto.SubZoneNo);
    }

    [Fact]
    public void UpdatePropertyDto_PlotNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { PlotNo = "  PLOT123  " };

        // Assert
        Assert.Equal("PLOT123", dto.PlotNo);
    }

    [Fact]
    public void UpdatePropertyDto_PartType_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { PartType = "  TypeA  " };

        // Assert
        Assert.Equal("TypeA", dto.PartType);
    }

    [Fact]
    public void UpdatePropertyDto_OwnerTitle_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { OwnerTitle = "  Mr.  " };

        // Assert
        Assert.Equal("Mr.", dto.OwnerTitle);
    }

    [Fact]
    public void UpdatePropertyDto_OwnerName_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { OwnerName = "  John Doe  " };

        // Assert
        Assert.Equal("John Doe", dto.OwnerName);
    }

    [Fact]
    public void UpdatePropertyDto_OwnerTitleEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { OwnerTitleEnglish = "  Mr.  " };

        // Assert
        Assert.Equal("Mr.", dto.OwnerTitleEnglish);
    }

    [Fact]
    public void UpdatePropertyDto_OwnerNameEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { OwnerNameEnglish = "  John Doe  " };

        // Assert
        Assert.Equal("John Doe", dto.OwnerNameEnglish);
    }

    [Fact]
    public void UpdatePropertyDto_OccupierTitle_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { OccupierTitle = "  Mrs.  " };

        // Assert
        Assert.Equal("Mrs.", dto.OccupierTitle);
    }

    [Fact]
    public void UpdatePropertyDto_OccupierName_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { OccupierName = "  Jane Doe  " };

        // Assert
        Assert.Equal("Jane Doe", dto.OccupierName);
    }

    [Fact]
    public void UpdatePropertyDto_OccupierTitleEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { OccupierTitleEnglish = "  Mrs.  " };

        // Assert
        Assert.Equal("Mrs.", dto.OccupierTitleEnglish);
    }

    [Fact]
    public void UpdatePropertyDto_OccupierNameEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { OccupierNameEnglish = "  Jane Doe  " };

        // Assert
        Assert.Equal("Jane Doe", dto.OccupierNameEnglish);
    }

    [Fact]
    public void UpdatePropertyDto_FlatOrShopNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { FlatOrShopNo = "  101  " };

        // Assert
        Assert.Equal("101", dto.FlatOrShopNo);
    }

    [Fact]
    public void UpdatePropertyDto_FlatOrShopName_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { FlatOrShopName = "  Shop Name  " };

        // Assert
        Assert.Equal("Shop Name", dto.FlatOrShopName);
    }

    [Fact]
    public void UpdatePropertyDto_FlatOrShopNoEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { FlatOrShopNoEnglish = "  101A  " };

        // Assert
        Assert.Equal("101A", dto.FlatOrShopNoEnglish);
    }

    [Fact]
    public void UpdatePropertyDto_FlatOrShopNameEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { FlatOrShopNameEnglish = "  Shop Name  " };

        // Assert
        Assert.Equal("Shop Name", dto.FlatOrShopNameEnglish);
    }

    [Fact]
    public void UpdatePropertyDto_Address_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { Address = "  123 Main St  " };

        // Assert
        Assert.Equal("123 Main St", dto.Address);
    }

    [Fact]
    public void UpdatePropertyDto_Location_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { Location = "  Downtown  " };

        // Assert
        Assert.Equal("Downtown", dto.Location);
    }

    [Fact]
    public void UpdatePropertyDto_AddressEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { AddressEnglish = "  123 Main St  " };

        // Assert
        Assert.Equal("123 Main St", dto.AddressEnglish);
    }

    [Fact]
    public void UpdatePropertyDto_LocationEnglish_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { LocationEnglish = "  Downtown  " };

        // Assert
        Assert.Equal("Downtown", dto.LocationEnglish);
    }

    [Fact]
    public void UpdatePropertyDto_MobileNo_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { MobileNo = "  1234567890  " };

        // Assert
        Assert.Equal("1234567890", dto.MobileNo);
    }

    [Fact]
    public void UpdatePropertyDto_EmailId_TrimsWhitespaceAndConvertsToLower()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { EmailId = "  Test@Example.COM  " };

        // Assert
        Assert.Equal("test@example.com", dto.EmailId);
    }

    [Fact]
    public void UpdatePropertyDto_EmailId_ConvertsWhitespaceToNull()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto { EmailId = "   " };

        // Assert
        Assert.Null(dto.EmailId);
    }

    #endregion

    #region All Properties Coverage Tests

    [Fact]
    public void CreatePropertyDto_AllPropertiesCanBeSet()
    {
        // Arrange & Act
        var dto = new CreatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 2,
            PropertyNo = "PROP123",
            PartitionNo = "PART456",
            PropertyTypeId = 3,
            UPICId = "UPIC-123",
            OpenPlot = true,
            CSN = "CSN123",
            SubZoneNo = "SZ123",
            PlotNo = "PLOT123",
            CategoryId = 4,
            PartType = "TypeA",
            OwnerTitle = "Mr.",
            OwnerName = "John Doe",
            OwnerTitleEnglish = "Mr.",
            OwnerNameEnglish = "John Doe",
            OccupierTitle = "Mrs.",
            OccupierName = "Jane Doe",
            OccupierTitleEnglish = "Mrs.",
            OccupierNameEnglish = "Jane Doe",
            FlatOrShopNo = "101",
            FlatOrShopName = "Shop",
            FlatOrShopNoEnglish = "101",
            FlatOrShopNameEnglish = "Shop",
            Address = "Address",
            Location = "Location",
            AddressEnglish = "Address",
            LocationEnglish = "Location",
            MobileNo = "1234567890",
            EmailId = "test@test.com",
            SocietyDetailId = 5,
            MarkedForDeletion = true
        };

        // Assert
        Assert.Equal(1, dto.TaxZoneId);
        Assert.Equal(2, dto.WardId);
        Assert.Equal("PROP123", dto.PropertyNo);
        Assert.Equal("PART456", dto.PartitionNo);
        Assert.Equal(3, dto.PropertyTypeId);
        Assert.Equal("UPIC-123", dto.UPICId);
        Assert.True(dto.OpenPlot);
        Assert.Equal("CSN123", dto.CSN);
        Assert.Equal("SZ123", dto.SubZoneNo);
        Assert.Equal("PLOT123", dto.PlotNo);
        Assert.Equal(4, dto.CategoryId);
        Assert.Equal("TypeA", dto.PartType);
        Assert.Equal("Mr.", dto.OwnerTitle);
        Assert.Equal("John Doe", dto.OwnerName);
        Assert.Equal("Mr.", dto.OwnerTitleEnglish);
        Assert.Equal("John Doe", dto.OwnerNameEnglish);
        Assert.Equal("Mrs.", dto.OccupierTitle);
        Assert.Equal("Jane Doe", dto.OccupierName);
        Assert.Equal("Mrs.", dto.OccupierTitleEnglish);
        Assert.Equal("Jane Doe", dto.OccupierNameEnglish);
        Assert.Equal("101", dto.FlatOrShopNo);
        Assert.Equal("Shop", dto.FlatOrShopName);
        Assert.Equal("101", dto.FlatOrShopNoEnglish);
        Assert.Equal("Shop", dto.FlatOrShopNameEnglish);
        Assert.Equal("Address", dto.Address);
        Assert.Equal("Location", dto.Location);
        Assert.Equal("Address", dto.AddressEnglish);
        Assert.Equal("Location", dto.LocationEnglish);
        Assert.Equal("1234567890", dto.MobileNo);
        Assert.Equal("test@test.com", dto.EmailId);
        Assert.Equal(5, dto.SocietyDetailId);
        Assert.True(dto.MarkedForDeletion);
    }

    [Fact]
    public void UpdatePropertyDto_AllPropertiesCanBeSet()
    {
        // Arrange & Act
        var dto = new UpdatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 2,
            PropertyNo = "PROP123",
            PartitionNo = "PART456",
            PropertyTypeId = 3,
            UPICId = "UPIC-123",
            OpenPlot = true,
            CSN = "CSN123",
            SubZoneNo = "SZ123",
            PlotNo = "PLOT123",
            CategoryId = 4,
            PartType = "TypeA",
            OwnerTitle = "Mr.",
            OwnerName = "John Doe",
            OwnerTitleEnglish = "Mr.",
            OwnerNameEnglish = "John Doe",
            OccupierTitle = "Mrs.",
            OccupierName = "Jane Doe",
            OccupierTitleEnglish = "Mrs.",
            OccupierNameEnglish = "Jane Doe",
            FlatOrShopNo = "101",
            FlatOrShopName = "Shop",
            FlatOrShopNoEnglish = "101",
            FlatOrShopNameEnglish = "Shop",
            Address = "Address",
            Location = "Location",
            AddressEnglish = "Address",
            LocationEnglish = "Location",
            MobileNo = "1234567890",
            EmailId = "test@test.com",
            SocietyDetailId = 5,
            MarkedForDeletion = true
        };

        // Assert
        Assert.Equal(1, dto.TaxZoneId);
        Assert.Equal(2, dto.WardId);
        Assert.Equal("PROP123", dto.PropertyNo);
        Assert.Equal("PART456", dto.PartitionNo);
        Assert.Equal(3, dto.PropertyTypeId);
        Assert.Equal("UPIC-123", dto.UPICId);
        Assert.True(dto.OpenPlot);
        Assert.Equal("CSN123", dto.CSN);
        Assert.Equal("SZ123", dto.SubZoneNo);
        Assert.Equal("PLOT123", dto.PlotNo);
        Assert.Equal(4, dto.CategoryId);
        Assert.Equal("TypeA", dto.PartType);
        Assert.Equal("Mr.", dto.OwnerTitle);
        Assert.Equal("John Doe", dto.OwnerName);
        Assert.Equal("Mr.", dto.OwnerTitleEnglish);
        Assert.Equal("John Doe", dto.OwnerNameEnglish);
        Assert.Equal("Mrs.", dto.OccupierTitle);
        Assert.Equal("Jane Doe", dto.OccupierName);
        Assert.Equal("Mrs.", dto.OccupierTitleEnglish);
        Assert.Equal("Jane Doe", dto.OccupierNameEnglish);
        Assert.Equal("101", dto.FlatOrShopNo);
        Assert.Equal("Shop", dto.FlatOrShopName);
        Assert.Equal("101", dto.FlatOrShopNoEnglish);
        Assert.Equal("Shop", dto.FlatOrShopNameEnglish);
        Assert.Equal("Address", dto.Address);
        Assert.Equal("Location", dto.Location);
        Assert.Equal("Address", dto.AddressEnglish);
        Assert.Equal("Location", dto.LocationEnglish);
        Assert.Equal("1234567890", dto.MobileNo);
        Assert.Equal("test@test.com", dto.EmailId);
        Assert.Equal(5, dto.SocietyDetailId);
        Assert.True(dto.MarkedForDeletion);
    }

    #endregion
}
