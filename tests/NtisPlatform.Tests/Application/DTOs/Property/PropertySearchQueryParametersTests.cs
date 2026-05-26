using NtisPlatform.Application.DTOs.Property;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Property;

/// <summary>
/// Test class for PropertySearchQueryParameters to achieve 100% line coverage
/// </summary>
public class PropertySearchQueryParametersTests
{
    [Fact]
    public void Constructor_CreatesInstance_WithDefaultValues()
    {
        // Act
        var parameters = new PropertySearchQueryParameters();

        // Assert
        Assert.NotNull(parameters);
        Assert.Null(parameters.PropertyTypeId);
        Assert.Null(parameters.TypeOfUseId);
        Assert.Null(parameters.ZoneId);
        Assert.Null(parameters.WardId);
        Assert.Null(parameters.CategoryId);
        Assert.Null(parameters.PropertyNoFrom);
        Assert.Null(parameters.PropertyNoTo);
        Assert.Null(parameters.OldPropertyNo);
        Assert.Null(parameters.UPICId);
        Assert.Null(parameters.CSN);
        Assert.Null(parameters.SubZoneNo);
        Assert.Null(parameters.PlotNo);
        Assert.Null(parameters.PropertyAssessmentStatusId);
        Assert.Null(parameters.MobileNo);
        Assert.Null(parameters.OwnerName);
        Assert.Null(parameters.OccupierName);
        Assert.Null(parameters.FlatOrShopName);
        Assert.Null(parameters.SocietyName);
        Assert.Null(parameters.Address);
    }

    [Fact]
    public void PropertyTypeId_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.PropertyTypeId = 1;

        // Assert
        Assert.Equal(1, parameters.PropertyTypeId);
    }

    [Fact]
    public void TypeOfUseId_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.TypeOfUseId = 2;

        // Assert
        Assert.Equal(2, parameters.TypeOfUseId);
    }

    [Fact]
    public void ZoneId_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.ZoneId = 3;

        // Assert
        Assert.Equal(3, parameters.ZoneId);
    }

    [Fact]
    public void WardId_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.WardId = 4;

        // Assert
        Assert.Equal(4, parameters.WardId);
    }

    [Fact]
    public void CategoryId_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.CategoryId = 5;

        // Assert
        Assert.Equal(5, parameters.CategoryId);
    }

    [Fact]
    public void PropertyAssessmentStatusId_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.PropertyAssessmentStatusId = 6;

        // Assert
        Assert.Equal(6, parameters.PropertyAssessmentStatusId);
    }

    [Fact]
    public void PropertyNoFrom_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.PropertyNoFrom = "001";

        // Assert
        Assert.Equal("001", parameters.PropertyNoFrom);
    }

    [Fact]
    public void PropertyNoTo_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.PropertyNoTo = "100";

        // Assert
        Assert.Equal("100", parameters.PropertyNoTo);
    }

    [Fact]
    public void OldPropertyNo_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.OldPropertyNo = "OLD123";

        // Assert
        Assert.Equal("OLD123", parameters.OldPropertyNo);
    }

    [Fact]
    public void UPICId_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.UPICId = "UPIC001";

        // Assert
        Assert.Equal("UPIC001", parameters.UPICId);
    }

    [Fact]
    public void CSN_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.CSN = "CSN001";

        // Assert
        Assert.Equal("CSN001", parameters.CSN);
    }

    [Fact]
    public void SubZoneNo_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.SubZoneNo = "SZ001";

        // Assert
        Assert.Equal("SZ001", parameters.SubZoneNo);
    }

    [Fact]
    public void PlotNo_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.PlotNo = "PLOT001";

        // Assert
        Assert.Equal("PLOT001", parameters.PlotNo);
    }

    [Fact]
    public void MobileNo_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.MobileNo = "9876543210";

        // Assert
        Assert.Equal("9876543210", parameters.MobileNo);
    }

    [Fact]
    public void OwnerName_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.OwnerName = "John Doe";

        // Assert
        Assert.Equal("John Doe", parameters.OwnerName);
    }

    [Fact]
    public void OccupierName_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.OccupierName = "Jane Smith";

        // Assert
        Assert.Equal("Jane Smith", parameters.OccupierName);
    }

    [Fact]
    public void FlatOrShopName_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.FlatOrShopName = "Shop A";

        // Assert
        Assert.Equal("Shop A", parameters.FlatOrShopName);
    }

    [Fact]
    public void SocietyName_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.SocietyName = "Green Valley";

        // Assert
        Assert.Equal("Green Valley", parameters.SocietyName);
    }

    [Fact]
    public void Address_CanBeSet_AndRetrieved()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.Address = "123 Main Street";

        // Assert
        Assert.Equal("123 Main Street", parameters.Address);
    }

    [Fact]
    public void AllProperties_CanBeSet_Simultaneously()
    {
        // Arrange & Act
        var parameters = new PropertySearchQueryParameters
        {
            PropertyTypeId = 1,
            TypeOfUseId = 2,
            ZoneId = 3,
            WardId = 4,
            CategoryId = 5,
            PropertyAssessmentStatusId = 6,
            PropertyNoFrom = "001",
            PropertyNoTo = "100",
            OldPropertyNo = "OLD123",
            UPICId = "UPIC001",
            CSN = "CSN001",
            SubZoneNo = "SZ001",
            PlotNo = "PLOT001",
            MobileNo = "9876543210",
            OwnerName = "John Doe",
            OccupierName = "Jane Smith",
            FlatOrShopName = "Shop A",
            SocietyName = "Green Valley",
            Address = "123 Main Street"
        };

        // Assert
        Assert.Equal(1, parameters.PropertyTypeId);
        Assert.Equal(2, parameters.TypeOfUseId);
        Assert.Equal(3, parameters.ZoneId);
        Assert.Equal(4, parameters.WardId);
        Assert.Equal(5, parameters.CategoryId);
        Assert.Equal(6, parameters.PropertyAssessmentStatusId);
        Assert.Equal("001", parameters.PropertyNoFrom);
        Assert.Equal("100", parameters.PropertyNoTo);
        Assert.Equal("OLD123", parameters.OldPropertyNo);
        Assert.Equal("UPIC001", parameters.UPICId);
        Assert.Equal("CSN001", parameters.CSN);
        Assert.Equal("SZ001", parameters.SubZoneNo);
        Assert.Equal("PLOT001", parameters.PlotNo);
        Assert.Equal("9876543210", parameters.MobileNo);
        Assert.Equal("John Doe", parameters.OwnerName);
        Assert.Equal("Jane Smith", parameters.OccupierName);
        Assert.Equal("Shop A", parameters.FlatOrShopName);
        Assert.Equal("Green Valley", parameters.SocietyName);
        Assert.Equal("123 Main Street", parameters.Address);
    }

    [Fact]
    public void NullableProperties_CanBeSetToNull()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters
        {
            PropertyTypeId = 1,
            CategoryId = 5
        };

        // Act
        parameters.PropertyTypeId = null;
        parameters.CategoryId = null;

        // Assert
        Assert.Null(parameters.PropertyTypeId);
        Assert.Null(parameters.CategoryId);
    }

    [Fact]
    public void StringProperties_CanBeSetToEmptyString()
    {
        // Arrange
        var parameters = new PropertySearchQueryParameters();

        // Act
        parameters.PropertyNoFrom = "";
        parameters.MobileNo = "";
        parameters.Address = "";

        // Assert
        Assert.Equal("", parameters.PropertyNoFrom);
        Assert.Equal("", parameters.MobileNo);
        Assert.Equal("", parameters.Address);
    }
}
