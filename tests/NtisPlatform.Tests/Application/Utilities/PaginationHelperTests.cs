using NtisPlatform.Application.Utilities;
using Xunit;

namespace NtisPlatform.Tests.Application.Utilities;

public class PaginationHelperTests
{
    [Fact]
    public void Calculate_WithNormalPagination_ReturnsCorrectValues()
    {
        // Arrange
        int requestedPageNumber = 2;
        int requestedPageSize = 10;
        int totalCount = 50;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(2, pageNumber);
        Assert.Equal(10, pageSize);
        Assert.Equal(10, skip); // (2-1) * 10
        Assert.Equal(10, take);
    }

    [Fact]
    public void Calculate_WithFirstPage_ReturnsZeroSkip()
    {
        // Arrange
        int requestedPageNumber = 1;
        int requestedPageSize = 10;
        int totalCount = 50;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(1, pageNumber);
        Assert.Equal(10, pageSize);
        Assert.Equal(0, skip); // (1-1) * 10
        Assert.Equal(10, take);
    }

    [Fact]
    public void Calculate_WithPageSizeMinusOne_ReturnsAllRecords()
    {
        // Arrange
        int requestedPageNumber = 5; // Should be ignored
        int requestedPageSize = -1;
        int totalCount = 100;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(1, pageNumber); // Should reset to 1
        Assert.Equal(100, pageSize); // Should be totalCount
        Assert.Equal(0, skip);
        Assert.Equal(100, take);
    }

    [Fact]
    public void Calculate_WithPageSizeMinusOneAndZeroTotalCount_ReturnsMinimumPageSize()
    {
        // Arrange
        int requestedPageNumber = 1;
        int requestedPageSize = -1;
        int totalCount = 0;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(1, pageNumber);
        Assert.Equal(1, pageSize); // Math.Max(1, 0) = 1
        Assert.Equal(0, skip);
        Assert.Equal(0, take);
    }

    [Fact]
    public void Calculate_WithLargePageNumber_ReturnsCorrectSkipValue()
    {
        // Arrange
        int requestedPageNumber = 10;
        int requestedPageSize = 20;
        int totalCount = 500;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(10, pageNumber);
        Assert.Equal(20, pageSize);
        Assert.Equal(180, skip); // (10-1) * 20
        Assert.Equal(20, take);
    }

    [Fact]
    public void Calculate_WithSmallPageSize_ReturnsCorrectValues()
    {
        // Arrange
        int requestedPageNumber = 3;
        int requestedPageSize = 5;
        int totalCount = 50;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(3, pageNumber);
        Assert.Equal(5, pageSize);
        Assert.Equal(10, skip); // (3-1) * 5
        Assert.Equal(5, take);
    }

    [Fact]
    public void Calculate_WithLargePageSize_ReturnsCorrectValues()
    {
        // Arrange
        int requestedPageNumber = 1;
        int requestedPageSize = 100;
        int totalCount = 50;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(1, pageNumber);
        Assert.Equal(100, pageSize);
        Assert.Equal(0, skip);
        Assert.Equal(100, take);
    }

    [Theory]
    [InlineData(1, 10, 100, 0, 10)]
    [InlineData(2, 10, 100, 10, 10)]
    [InlineData(5, 20, 200, 80, 20)]
    [InlineData(1, 50, 150, 0, 50)]
    [InlineData(3, 15, 100, 30, 15)]
    public void Calculate_WithVariousInputs_ReturnsExpectedSkipAndTake(
        int pageNumber, int pageSize, int totalCount, int expectedSkip, int expectedTake)
    {
        // Act
        var (_, _, skip, take) = PaginationHelper.Calculate(pageNumber, pageSize, totalCount);

        // Assert
        Assert.Equal(expectedSkip, skip);
        Assert.Equal(expectedTake, take);
    }

    [Fact]
    public void Calculate_WithPageSizeMinusOne_IgnoresPageNumber()
    {
        // Arrange
        int requestedPageNumber = 999;
        int requestedPageSize = -1;
        int totalCount = 75;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(1, pageNumber); // Always 1 when fetching all
        Assert.Equal(75, pageSize);
        Assert.Equal(0, skip);
        Assert.Equal(75, take);
    }

    [Theory]
    [InlineData(1, -1, 0, 1, 0)]
    [InlineData(1, -1, 1, 1, 1)]
    [InlineData(1, -1, 100, 100, 100)]
    [InlineData(1, -1, 1000, 1000, 1000)]
    public void Calculate_WithPageSizeMinusOne_ReturnsAllRecordsVariations(
        int pageNumber, int pageSize, int totalCount, int expectedPageSize, int expectedTake)
    {
        // Act
        var (_, actualPageSize, skip, take) = PaginationHelper.Calculate(pageNumber, pageSize, totalCount);

        // Assert
        Assert.Equal(expectedPageSize, actualPageSize);
        Assert.Equal(0, skip); // Always 0 when pageSize is -1
        Assert.Equal(expectedTake, take);
    }

    [Fact]
    public void Calculate_WithZeroPageNumber_ReturnsNegativeSkip()
    {
        // Arrange
        int requestedPageNumber = 0;
        int requestedPageSize = 10;
        int totalCount = 100;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(0, pageNumber);
        Assert.Equal(10, pageSize);
        Assert.Equal(-10, skip); // (0-1) * 10 = -10
        Assert.Equal(10, take);
    }

    [Fact]
    public void Calculate_PreservesRequestedValues_WhenNotFetchingAll()
    {
        // Arrange
        int requestedPageNumber = 7;
        int requestedPageSize = 25;
        int totalCount = 1000;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(requestedPageNumber, pageNumber);
        Assert.Equal(requestedPageSize, pageSize);
        Assert.Equal(requestedPageSize, take);
    }

    [Fact]
    public void Calculate_WithVeryLargeTotalCount_HandlesCorrectly()
    {
        // Arrange
        int requestedPageNumber = 1;
        int requestedPageSize = -1;
        int totalCount = int.MaxValue;

        // Act
        var (pageNumber, pageSize, skip, take) = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(1, pageNumber);
        Assert.Equal(int.MaxValue, pageSize);
        Assert.Equal(0, skip);
        Assert.Equal(int.MaxValue, take);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    [InlineData(1000, 1000)]
    public void Calculate_WithPageSizeMinusOne_EnsuresMinimumPageSizeOfOne(int totalCount, int expectedPageSize)
    {
        // Act
        var (_, pageSize, _, _) = PaginationHelper.Calculate(1, -1, totalCount);

        // Assert
        Assert.Equal(Math.Max(1, expectedPageSize), pageSize);
    }

    [Fact]
    public void Calculate_MultipleCallsSameInput_ReturnsConsistentResults()
    {
        // Arrange
        int requestedPageNumber = 5;
        int requestedPageSize = 15;
        int totalCount = 200;

        // Act
        var result1 = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);
        var result2 = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);
        var result3 = PaginationHelper.Calculate(requestedPageNumber, requestedPageSize, totalCount);

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }
}
