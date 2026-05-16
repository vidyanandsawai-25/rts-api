using NtisPlatform.Application.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Application.Helpers;

public class RangeGeneratorTests
{
    #region Numeric Range Tests

    [Fact]
    public void GenerateRangeValues_WithSimpleNumericRange_GeneratesCorrectValues()
    {
        // Arrange
        var rangeFrom = "1";
        var rangeTo = "5";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithNumericRangeAndPrefix_AppliesPrefix()
    {
        // Arrange
        var rangeFrom = "1";
        var rangeTo = "3";
        var prefix = "Unit-";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, prefix);

        // Assert
        Assert.Equal(new[] { "Unit-1", "Unit-2", "Unit-3" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithNumericRangeAndSuffix_AppliesSuffix()
    {
        // Arrange
        var rangeFrom = "1";
        var rangeTo = "3";
        var suffix = "-Floor";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, suffix: suffix);

        // Assert
        Assert.Equal(new[] { "1-Floor", "2-Floor", "3-Floor" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithNumericRangeAndPrefixSuffix_AppliesBoth()
    {
        // Arrange
        var rangeFrom = "1";
        var rangeTo = "3";
        var prefix = "Unit-";
        var suffix = "-A";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, prefix, suffix);

        // Assert
        Assert.Equal(new[] { "Unit-1-A", "Unit-2-A", "Unit-3-A" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithZeroPaddedNumeric_MaintainsPadding()
    {
        // Arrange
        var rangeFrom = "001";
        var rangeTo = "005";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(new[] { "001", "002", "003", "004", "005" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithMixedPadding_UseMaxPadding()
    {
        // Arrange
        var rangeFrom = "01";
        var rangeTo = "005";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(new[] { "001", "002", "003", "004", "005" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithSingleZeroPadded_AppliesPadding()
    {
        // Arrange
        var rangeFrom = "08";
        var rangeTo = "12";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(new[] { "08", "09", "10", "11", "12" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithoutZeroPadding_NoExtraPadding()
    {
        // Arrange
        var rangeFrom = "8";
        var rangeTo = "12";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(new[] { "8", "9", "10", "11", "12" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithNumericRangeFromGreaterThanTo_ThrowsException()
    {
        // Arrange
        var rangeFrom = "10";
        var rangeTo = "5";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
        Assert.Contains("cannot be greater than", exception.Message);
    }

    [Fact]
    public void GenerateRangeValues_WithSameNumericRange_ReturnsSingleValue()
    {
        // Arrange
        var rangeFrom = "5";
        var rangeTo = "5";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Single(result);
        Assert.Equal("5", result[0]);
    }

    #endregion

    #region Alphabetic Range Tests

    [Fact]
    public void GenerateRangeValues_WithSimpleAlphabeticRange_GeneratesCorrectValues()
    {
        // Arrange
        var rangeFrom = "A";
        var rangeTo = "E";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithLowercaseAlphabetic_ConvertsToUppercase()
    {
        // Arrange
        var rangeFrom = "a";
        var rangeTo = "c";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(new[] { "A", "B", "C" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithMixedCaseAlphabetic_ConvertsToUppercase()
    {
        // Arrange
        var rangeFrom = "a";
        var rangeTo = "C";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(new[] { "A", "B", "C" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithTwoLetterAlphabetic_GeneratesCorrectly()
    {
        // Arrange
        var rangeFrom = "AA";
        var rangeTo = "AC";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(new[] { "AA", "AB", "AC" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithAlphabeticAndPrefix_AppliesPrefix()
    {
        // Arrange
        var rangeFrom = "A";
        var rangeTo = "C";
        var prefix = "Wing-";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, prefix);

        // Assert
        Assert.Equal(new[] { "Wing-A", "Wing-B", "Wing-C" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithAlphabeticAndSuffix_AppliesSuffix()
    {
        // Arrange
        var rangeFrom = "A";
        var rangeTo = "C";
        var suffix = "-Block";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, suffix: suffix);

        // Assert
        Assert.Equal(new[] { "A-Block", "B-Block", "C-Block" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithAlphabeticRangeFromGreaterThanTo_ThrowsException()
    {
        // Arrange
        var rangeFrom = "E";
        var rangeTo = "A";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
        Assert.Contains("cannot be greater than", exception.Message);
    }

    [Fact]
    public void GenerateRangeValues_WithMixedLengthAlphabetic_ThrowsException()
    {
        // Arrange
        var rangeFrom = "A";
        var rangeTo = "AA";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
        Assert.Contains("Mixed-length", exception.Message);
    }

    [Fact]
    public void GenerateRangeValues_WithSameAlphabeticRange_ReturnsSingleValue()
    {
        // Arrange
        var rangeFrom = "D";
        var rangeTo = "D";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Single(result);
        Assert.Equal("D", result[0]);
    }

    [Fact]
    public void GenerateRangeValues_WithFullAlphabet_Generates26Values()
    {
        // Arrange
        var rangeFrom = "A";
        var rangeTo = "Z";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(26, result.Count);
        Assert.Equal("A", result.First());
        Assert.Equal("Z", result.Last());
    }

    [Fact]
    public void GenerateRangeValues_WithMultiLetterRange_GeneratesSequentially()
    {
        // Arrange
        var rangeFrom = "AY";
        var rangeTo = "BA";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Contains("AY", result);
        Assert.Contains("AZ", result);
        Assert.Contains("BA", result);
    }

    #endregion

    #region Error Cases

    [Fact]
    public void GenerateRangeValues_WithNullRangeFrom_ThrowsException()
    {
        // Arrange
        string rangeFrom = null!;
        var rangeTo = "5";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
    }

    [Fact]
    public void GenerateRangeValues_WithNullRangeTo_ThrowsException()
    {
        // Arrange
        var rangeFrom = "1";
        string rangeTo = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
    }

    [Fact]
    public void GenerateRangeValues_WithEmptyRangeFrom_ThrowsException()
    {
        // Arrange
        var rangeFrom = "";
        var rangeTo = "5";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
    }

    [Fact]
    public void GenerateRangeValues_WithWhitespaceRangeFrom_ThrowsException()
    {
        // Arrange
        var rangeFrom = "   ";
        var rangeTo = "5";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
    }

    [Fact]
    public void GenerateRangeValues_WithMixedNumericAlphabetic_ThrowsException()
    {
        // Arrange
        var rangeFrom = "1";
        var rangeTo = "A";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
        Assert.Contains("Invalid range", exception.Message);
    }

    [Fact]
    public void GenerateRangeValues_WithAlphanumericString_ThrowsException()
    {
        // Arrange
        var rangeFrom = "A1";
        var rangeTo = "A5";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
        Assert.Contains("Invalid range", exception.Message);
    }

    [Fact]
    public void GenerateRangeValues_WithSpecialCharacters_ThrowsException()
    {
        // Arrange
        var rangeFrom = "A-";
        var rangeTo = "C-";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GenerateRangeValues_WithLargeNumericRange_GeneratesAllValues()
    {
        // Arrange
        var rangeFrom = "1";
        var rangeTo = "100";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(100, result.Count);
        Assert.Equal("1", result.First());
        Assert.Equal("100", result.Last());
    }

    [Fact]
    public void GenerateRangeValues_WithNullPrefixAndSuffix_WorksCorrectly()
    {
        // Arrange
        var rangeFrom = "1";
        var rangeTo = "3";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, null, null);

        // Assert
        Assert.Equal(new[] { "1", "2", "3" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithEmptyStringPrefixAndSuffix_WorksCorrectly()
    {
        // Arrange
        var rangeFrom = "1";
        var rangeTo = "3";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, "", "");

        // Assert
        Assert.Equal(new[] { "1", "2", "3" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithNegativeNumbers_GeneratesRange()
    {
        // Arrange - Negative numbers will parse as int
        var rangeFrom = "-5";
        var rangeTo = "-1";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert - Should successfully generate the range
        Assert.Equal(5, result.Count);
        Assert.Equal(new[] { "-5", "-4", "-3", "-2", "-1" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithThreeLetterRange_WorksCorrectly()
    {
        // Arrange
        var rangeFrom = "AAA";
        var rangeTo = "AAC";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(new[] { "AAA", "AAB", "AAC" }, result);
    }

    [Fact]
    public void GenerateRangeValues_CrossingAlphabeticBoundary_WorksCorrectly()
    {
        // Arrange
        var rangeFrom = "Y";
        var rangeTo = "AB";

        // Act & Assert - Different lengths should throw
        Assert.Throws<ArgumentException>(() =>
            RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo));
    }

    [Fact]
    public void GenerateRangeValues_WithZeroStart_WorksCorrectly()
    {
        // Arrange
        var rangeFrom = "0";
        var rangeTo = "3";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(new[] { "0", "1", "2", "3" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithZeroPaddedZero_MaintainsPadding()
    {
        // Arrange
        var rangeFrom = "00";
        var rangeTo = "03";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo);

        // Assert
        Assert.Equal(new[] { "00", "01", "02", "03" }, result);
    }

    [Fact]
    public void GenerateRangeValues_WithComplexPrefix_AppliesCorrectly()
    {
        // Arrange
        var rangeFrom = "1";
        var rangeTo = "2";
        var prefix = "Building-A-Floor-";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, prefix);

        // Assert
        Assert.Equal(new[] { "Building-A-Floor-1", "Building-A-Floor-2" }, result);
    }

    #endregion

    #region Real-world Scenarios

    [Fact]
    public void GenerateRangeValues_PropertyUnits_Scenario()
    {
        // Arrange - Generate property units like "Unit-101" to "Unit-105"
        var rangeFrom = "101";
        var rangeTo = "105";
        var prefix = "Unit-";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, prefix);

        // Assert
        Assert.Equal(new[] { "Unit-101", "Unit-102", "Unit-103", "Unit-104", "Unit-105" }, result);
    }

    [Fact]
    public void GenerateRangeValues_BuildingWings_Scenario()
    {
        // Arrange - Generate wings like "Wing-A" to "Wing-D"
        var rangeFrom = "A";
        var rangeTo = "D";
        var prefix = "Wing-";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, prefix);

        // Assert
        Assert.Equal(new[] { "Wing-A", "Wing-B", "Wing-C", "Wing-D" }, result);
    }

    [Fact]
    public void GenerateRangeValues_FloorNumbers_Scenario()
    {
        // Arrange - Generate floor numbers like "Floor-1" to "Floor-10"
        var rangeFrom = "1";
        var rangeTo = "10";
        var prefix = "Floor-";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, prefix);

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal("Floor-1", result.First());
        Assert.Equal("Floor-10", result.Last());
    }

    [Fact]
    public void GenerateRangeValues_ParkingSpots_Scenario()
    {
        // Arrange - Generate parking spots like "P-001" to "P-050"
        var rangeFrom = "001";
        var rangeTo = "050";
        var prefix = "P-";

        // Act
        var result = RangeGenerator.GenerateRangeValues(rangeFrom, rangeTo, prefix);

        // Assert
        Assert.Equal(50, result.Count);
        Assert.All(result, spot => Assert.StartsWith("P-", spot));
        Assert.All(result, spot => Assert.Equal(5, spot.Length)); // P-XXX format
    }

    #endregion
}
