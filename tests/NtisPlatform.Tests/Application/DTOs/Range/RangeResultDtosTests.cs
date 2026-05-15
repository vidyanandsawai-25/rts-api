using Xunit;
using NtisPlatform.Application.DTOs.Range;

namespace NtisPlatform.Tests.Application.DTOs.Range;

public class RangeResultDtosTests
{
    [Fact]
    public void RangeResult_WithSuccessfulResults_SetsPropertiesCorrectly()
    {
        // Arrange
        var results = new List<string> { "Item1", "Item2", "Item3" };

        // Act
        var rangeResult = new RangeResult<string>(3, 0, results);

        // Assert
        Assert.Equal(3, rangeResult.SuccessCount);
        Assert.Equal(0, rangeResult.FailedCount);
        Assert.Equal(3, rangeResult.Results.Count);
        Assert.False(rangeResult.HasFailures);
        Assert.True(rangeResult.AllSucceeded);
        Assert.Null(rangeResult.Errors);
    }

    [Fact]
    public void RangeResult_WithFailures_SetsPropertiesCorrectly()
    {
        // Arrange
        var results = new List<string> { "Item1" };
        var errors = new List<string> { "Error1", "Error2" };

        // Act
        var rangeResult = new RangeResult<string>(1, 2, results, errors);

        // Assert
        Assert.Equal(1, rangeResult.SuccessCount);
        Assert.Equal(2, rangeResult.FailedCount);
        Assert.Single(rangeResult.Results);
        Assert.True(rangeResult.HasFailures);
        Assert.False(rangeResult.AllSucceeded);
        Assert.Equal(2, rangeResult.Errors?.Count);
    }

    [Fact]
    public void RangeResult_WithNoFailures_HasFailuresReturnsFalse()
    {
        // Arrange & Act
        var rangeResult = new RangeResult<int>(5, 0, new List<int> { 1, 2, 3, 4, 5 });

        // Assert
        Assert.False(rangeResult.HasFailures);
        Assert.True(rangeResult.AllSucceeded);
    }

    [Fact]
    public void RangeResult_WithEmptyResults_WorksCorrectly()
    {
        // Arrange & Act
        var rangeResult = new RangeResult<string>(0, 3, new List<string>(), new List<string> { "Error1", "Error2", "Error3" });

        // Assert
        Assert.Equal(0, rangeResult.SuccessCount);
        Assert.Equal(3, rangeResult.FailedCount);
        Assert.Empty(rangeResult.Results);
        Assert.True(rangeResult.HasFailures);
        Assert.False(rangeResult.AllSucceeded);
    }

    [Fact]
    public void RangeResult_WithComplexType_WorksCorrectly()
    {
        // Arrange
        var complexResults = new List<(int Id, string Name)>
        {
            (1, "Test1"),
            (2, "Test2")
        };

        // Act
        var rangeResult = new RangeResult<(int, string)>(2, 0, complexResults);

        // Assert
        Assert.Equal(2, rangeResult.SuccessCount);
        Assert.Equal(2, rangeResult.Results.Count);
        Assert.Equal(1, rangeResult.Results[0].Item1);
        Assert.Equal("Test1", rangeResult.Results[0].Item2);
    }
}
