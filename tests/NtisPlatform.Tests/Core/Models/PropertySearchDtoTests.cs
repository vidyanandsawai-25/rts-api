using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Core.Models;

/// <summary>
/// Test class for Property Search DTOs to achieve 100% line coverage
/// </summary>
public class PropertySearchDtoTests
{
    #region PropertySearchRequestDto Tests

    [Fact]
    public void PropertySearchRequestDto_Constructor_CreatesInstance_WithDefaultValues()
    {
        // Act
        var dto = new PropertySearchRequestDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Null(dto.PropertyTypeId);
        Assert.Null(dto.TypeOfUseId);
        Assert.Null(dto.ZoneId);
        Assert.Null(dto.WardId);
        Assert.Null(dto.CategoryId);
        Assert.Null(dto.PropertyAssessmentStatusId);
        Assert.Null(dto.PropertyNoFrom);
        Assert.Null(dto.PropertyNoTo);
        Assert.Null(dto.OldPropertyNo);
        Assert.Null(dto.UPICId);
        Assert.Null(dto.CSN);
        Assert.Null(dto.SubZoneNo);
        Assert.Null(dto.PlotNo);
        Assert.Null(dto.MobileNo);
        Assert.Null(dto.OwnerName);
        Assert.Null(dto.OccupierName);
        Assert.Null(dto.FlatOrShopName);
        Assert.Null(dto.SocietyName);
        Assert.Null(dto.Address);
    }

    [Fact]
    public void PropertySearchRequestDto_AllProperties_CanBeSet_AndRetrieved()
    {
        // Arrange & Act
        var dto = new PropertySearchRequestDto
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
        Assert.Equal(1, dto.PropertyTypeId);
        Assert.Equal(2, dto.TypeOfUseId);
        Assert.Equal(3, dto.ZoneId);
        Assert.Equal(4, dto.WardId);
        Assert.Equal(5, dto.CategoryId);
        Assert.Equal(6, dto.PropertyAssessmentStatusId);
        Assert.Equal("001", dto.PropertyNoFrom);
        Assert.Equal("100", dto.PropertyNoTo);
        Assert.Equal("OLD123", dto.OldPropertyNo);
        Assert.Equal("UPIC001", dto.UPICId);
        Assert.Equal("CSN001", dto.CSN);
        Assert.Equal("SZ001", dto.SubZoneNo);
        Assert.Equal("PLOT001", dto.PlotNo);
        Assert.Equal("9876543210", dto.MobileNo);
        Assert.Equal("John Doe", dto.OwnerName);
        Assert.Equal("Jane Smith", dto.OccupierName);
        Assert.Equal("Shop A", dto.FlatOrShopName);
        Assert.Equal("Green Valley", dto.SocietyName);
        Assert.Equal("123 Main Street", dto.Address);
    }

    [Fact]
    public void PropertySearchRequestDto_NumericProperties_CanBeSetIndividually()
    {
        // Arrange
        var dto = new PropertySearchRequestDto();

        // Act & Assert
        dto.PropertyTypeId = 10;
        Assert.Equal(10, dto.PropertyTypeId);

        dto.TypeOfUseId = 20;
        Assert.Equal(20, dto.TypeOfUseId);

        dto.ZoneId = 30;
        Assert.Equal(30, dto.ZoneId);

        dto.WardId = 40;
        Assert.Equal(40, dto.WardId);

        dto.CategoryId = 50;
        Assert.Equal(50, dto.CategoryId);

        dto.PropertyAssessmentStatusId = 60;
        Assert.Equal(60, dto.PropertyAssessmentStatusId);
    }

    [Fact]
    public void PropertySearchRequestDto_StringProperties_CanBeSetIndividually()
    {
        // Arrange
        var dto = new PropertySearchRequestDto();

        // Act & Assert
        dto.PropertyNoFrom = "AAA";
        Assert.Equal("AAA", dto.PropertyNoFrom);

        dto.PropertyNoTo = "ZZZ";
        Assert.Equal("ZZZ", dto.PropertyNoTo);

        dto.OldPropertyNo = "OLD999";
        Assert.Equal("OLD999", dto.OldPropertyNo);

        dto.UPICId = "UPIC999";
        Assert.Equal("UPIC999", dto.UPICId);

        dto.CSN = "CSN999";
        Assert.Equal("CSN999", dto.CSN);

        dto.SubZoneNo = "SZ999";
        Assert.Equal("SZ999", dto.SubZoneNo);

        dto.PlotNo = "PLOT999";
        Assert.Equal("PLOT999", dto.PlotNo);

        dto.MobileNo = "1234567890";
        Assert.Equal("1234567890", dto.MobileNo);

        dto.OwnerName = "Owner Test";
        Assert.Equal("Owner Test", dto.OwnerName);

        dto.OccupierName = "Occupier Test";
        Assert.Equal("Occupier Test", dto.OccupierName);

        dto.FlatOrShopName = "Flat 101";
        Assert.Equal("Flat 101", dto.FlatOrShopName);

        dto.SocietyName = "Test Society";
        Assert.Equal("Test Society", dto.SocietyName);

        dto.Address = "Test Address";
        Assert.Equal("Test Address", dto.Address);
    }

    [Fact]
    public void PropertySearchRequestDto_ValuesAndDuesProperties_DefaultValues_ShouldBeNull()
    {
        // Act
        var dto = new PropertySearchRequestDto();

        // Assert
        Assert.Null(dto.RVorCV);
        Assert.Null(dto.AmountFilterOperator);
        Assert.Null(dto.AmountValue);
        Assert.Null(dto.AmountTo);
    }

    [Fact]
    public void PropertySearchRequestDto_ValuesAndDuesProperties_CanBeSet_AndRetrieved()
    {
        // Arrange
        var dto = new PropertySearchRequestDto();

        // Act
        dto.RVorCV = "CV";
        dto.AmountFilterOperator = "Between";
        dto.AmountValue = 30000m;
        dto.AmountTo = 60000m;

        // Assert
        Assert.Equal("CV", dto.RVorCV);
        Assert.Equal("Between", dto.AmountFilterOperator);
        Assert.Equal(30000m, dto.AmountValue);
        Assert.Equal(60000m, dto.AmountTo);
    }

    [Fact]
    public void PropertySearchRequestDto_AmountFilterOperator_CanStoreSwaggerNumericEnumValue()
    {
        // Arrange
        var dto = new PropertySearchRequestDto();

        // Act
        dto.AmountFilterOperator = "0";
        dto.AmountValue = 1224666m;

        // Assert
        Assert.Equal("0", dto.AmountFilterOperator);
        Assert.Equal(1224666m, dto.AmountValue);
    }

    #endregion

    #region PropertySearchResponseDto Tests

    [Fact]
    public void PropertySearchResponseDto_Constructor_CreatesInstance_WithDefaultValues()
    {
        // Act
        var dto = new PropertySearchResponseDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(0, dto.PropertyId);
        Assert.Null(dto.UPICId);
        Assert.Null(dto.ZoneName);
        Assert.Null(dto.WardName);
        Assert.Null(dto.PropertyNo);
        Assert.Null(dto.PartitionNo);
        Assert.Null(dto.OldPropertyNo);
        Assert.Null(dto.CitySurveyNo);
        Assert.Null(dto.PlotNo);
        Assert.Null(dto.WingFlatNo);
        Assert.Null(dto.CategoryName);
        Assert.Null(dto.PropertyDescription);
        Assert.Null(dto.Mobile);
        Assert.Null(dto.PropertyHolderName);
        Assert.Null(dto.OccupierName);
        Assert.Null(dto.ShopBuildingName);
        Assert.Null(dto.SocietyName);
        Assert.Null(dto.Address);
        Assert.Null(dto.RV);
        Assert.Null(dto.CV);
        Assert.Null(dto.TotalTax);

    }

    [Fact]
    public void PropertySearchResponseDto_AllProperties_CanBeSet_AndRetrieved()
    {
        // Arrange & Act
        var dto = new PropertySearchResponseDto
        {
            PropertyId = 1,
            UPICId = "UPIC001",
            ZoneName = "Zone1",
            WardName = "Ward1",
            PropertyNo = "P001",
            PartitionNo = "A",
            OldPropertyNo = "OLD001",
            CitySurveyNo = "CS001",
            PlotNo = "PLOT001",
            WingFlatNo = "A-101",
            CategoryName = "Residential",
            PropertyDescription = "House",
            Mobile = "9876543210",
            PropertyHolderName = "John Doe",
            OccupierName = "Jane Smith",
            ShopBuildingName = "Building A",
            SocietyName = "Green Valley",
            Address = "123 Main St",
            RV = 100000.50m,
            CV = 50000.25m,
            TotalTax = 5000.25m
        };

        // Assert
        Assert.Equal(1, dto.PropertyId);
        Assert.Equal("UPIC001", dto.UPICId);
        Assert.Equal("Zone1", dto.ZoneName);
        Assert.Equal("Ward1", dto.WardName);
        Assert.Equal("P001", dto.PropertyNo);
        Assert.Equal("A", dto.PartitionNo);
        Assert.Equal("OLD001", dto.OldPropertyNo);
        Assert.Equal("CS001", dto.CitySurveyNo);
        Assert.Equal("PLOT001", dto.PlotNo);
        Assert.Equal("A-101", dto.WingFlatNo);
        Assert.Equal("Residential", dto.CategoryName);
        Assert.Equal("House", dto.PropertyDescription);
        Assert.Equal("9876543210", dto.Mobile);
        Assert.Equal("John Doe", dto.PropertyHolderName);
        Assert.Equal("Jane Smith", dto.OccupierName);
        Assert.Equal("Building A", dto.ShopBuildingName);
        Assert.Equal("Green Valley", dto.SocietyName);
        Assert.Equal("123 Main St", dto.Address);
        Assert.Equal(100000.50m, dto.RV);
        Assert.Equal(50000.25m, dto.CV);
        Assert.Equal(5000.25m, dto.TotalTax);
    }

    [Fact]
    public void PropertySearchResponseDto_PropertyId_CanBeSet()
    {
        // Arrange
        var dto = new PropertySearchResponseDto();

        // Act
        dto.PropertyId = 123;

        // Assert
        Assert.Equal(123, dto.PropertyId);
    }

    [Fact]
    public void PropertySearchResponseDto_StringProperties_CanBeSetIndividually()
    {
        // Arrange
        var dto = new PropertySearchResponseDto();

        // Act & Assert
        dto.UPICId = "TEST_UPIC";
        Assert.Equal("TEST_UPIC", dto.UPICId);

        dto.ZoneName = "TEST_ZONE";
        Assert.Equal("TEST_ZONE", dto.ZoneName);

        dto.WardName = "TEST_WARD";
        Assert.Equal("TEST_WARD", dto.WardName);

        dto.PropertyNo = "TEST_PROP";
        Assert.Equal("TEST_PROP", dto.PropertyNo);

        dto.PartitionNo = "TEST_PART";
        Assert.Equal("TEST_PART", dto.PartitionNo);

        dto.OldPropertyNo = "TEST_OLD";
        Assert.Equal("TEST_OLD", dto.OldPropertyNo);

        dto.CitySurveyNo = "TEST_CS";
        Assert.Equal("TEST_CS", dto.CitySurveyNo);

        dto.PlotNo = "TEST_PLOT";
        Assert.Equal("TEST_PLOT", dto.PlotNo);

        dto.WingFlatNo = "TEST_WING";
        Assert.Equal("TEST_WING", dto.WingFlatNo);

        dto.CategoryName = "TEST_CAT";
        Assert.Equal("TEST_CAT", dto.CategoryName);

        dto.PropertyDescription = "TEST_DESC";
        Assert.Equal("TEST_DESC", dto.PropertyDescription);

        dto.Mobile = "TEST_MOBILE";
        Assert.Equal("TEST_MOBILE", dto.Mobile);

        dto.PropertyHolderName = "TEST_HOLDER";
        Assert.Equal("TEST_HOLDER", dto.PropertyHolderName);

        dto.OccupierName = "TEST_OCCUPIER";
        Assert.Equal("TEST_OCCUPIER", dto.OccupierName);

        dto.ShopBuildingName = "TEST_SHOP";
        Assert.Equal("TEST_SHOP", dto.ShopBuildingName);

        dto.SocietyName = "TEST_SOCIETY";
        Assert.Equal("TEST_SOCIETY", dto.SocietyName);

        dto.Address = "TEST_ADDRESS";
        Assert.Equal("TEST_ADDRESS", dto.Address);
    }

    [Fact]
    public void PropertySearchResponseDto_NullableNumericProperties_CanBeSet()
    {
        // Arrange
        var dto = new PropertySearchResponseDto();

        // Act
        dto.RV = 50000.75m;
        dto.CV = 25000.50m;
        dto.TotalTax = 2500.50m;

        // Assert
        Assert.Equal(50000.75m, dto.RV);
        Assert.Equal(25000.50m, dto.CV);
        Assert.Equal(2500.50m, dto.TotalTax);
    }

    [Fact]
    public void PropertySearchResponseDto_NullableProperties_CanBeSetToNull()
    {
        // Arrange
        var dto = new PropertySearchResponseDto
        {
            RV = 1000m,
            CV = 500m,
            TotalTax = 500m
        };

        // Act
        dto.RV = null;
        dto.CV = null;
        dto.TotalTax = null;

        // Assert
        Assert.Null(dto.RV);
        Assert.Null(dto.CV);
        Assert.Null(dto.TotalTax);
    }

    [Fact]
    public void PropertySearchResponseDto_DecimalProperties_CanHandleZeroValues()
    {
        // Arrange & Act
        var dto = new PropertySearchResponseDto
        {
            RV = 0m,
            CV = 0m,
            TotalTax = 0m
        };

        // Assert
        Assert.Equal(0m, dto.RV);
        Assert.Equal(0m, dto.CV);
        Assert.Equal(0m, dto.TotalTax);
    }

    [Fact]
    public void PropertySearchResponseDto_DecimalProperties_CanHandleNegativeValues()
    {
        // Arrange & Act
        var dto = new PropertySearchResponseDto
        {
            RV = -1000m,
            CV = -500m,
            TotalTax = -500m
        };

        // Assert
        Assert.Equal(-1000m, dto.RV);
        Assert.Equal(-500m, dto.CV);
        Assert.Equal(-500m, dto.TotalTax);
    }

    [Fact]
    public void PropertySearchResponseDto_DecimalProperties_CanHandleLargeValues()
    {
        // Arrange & Act
        var dto = new PropertySearchResponseDto
        {
            RV = 999999999.99m,
            CV = 888888888.88m,
            TotalTax = 888888888.88m
        };

        // Assert
        Assert.Equal(999999999.99m, dto.RV);
        Assert.Equal(888888888.88m, dto.CV);
        Assert.Equal(888888888.88m, dto.TotalTax);
    }

    #endregion
}
