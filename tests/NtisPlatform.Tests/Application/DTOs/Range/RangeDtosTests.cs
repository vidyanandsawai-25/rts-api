using NtisPlatform.Application.DTOs.Range;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Range;

public class RangeDtosTests
{
    [Fact]
    public void RangeCreateItem_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var item = new RangeCreateItem<int, string>(1, "test-data");

        // Assert
        Assert.Equal(1, item.Id);
        Assert.Equal("test-data", item.Data);
    }

    [Fact]
    public void RangeCreateItem_WithStringRange_WorksCorrectly()
    {
        // Arrange & Act
        var item = new RangeCreateItem<string, string>("key-A", "value-Z");

        // Assert
        Assert.Equal("key-A", item.Id);
        Assert.Equal("value-Z", item.Data);
    }

    [Fact]
    public void RangeCreateItem_WithIntegerIdAndData_WorksCorrectly()
    {
        // Arrange & Act
        var item = new RangeCreateItem<int, int>(5, 15);

        // Assert
        Assert.Equal(5, item.Id);
        Assert.Equal(15, item.Data);
    }

    [Fact]
    public void RangeCreateItem_WithEmptyString_WorksCorrectly()
    {
        // Arrange & Act
        var item = new RangeCreateItem<int, string>(1, "");

        // Assert
        Assert.Equal(1, item.Id);
        Assert.Equal("", item.Data);
    }
}
