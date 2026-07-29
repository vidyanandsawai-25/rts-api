using NtisPlatform.Application.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class PropertyTypeToUseMapperTests
{
    [Fact]
    public void GetUseCategory_WithTypeCodeC_ReturnsNonResidential()
    {
        var result = PropertyTypeToUseMapper.GetUseCategory("C");
        Assert.Equal("Non Residential", result);
    }

    [Fact]
    public void GetUseCategory_WithTypeCodeI_ReturnsNonResidential()
    {
        var result = PropertyTypeToUseMapper.GetUseCategory("I");
        Assert.Equal("Non Residential", result);
    }

    [Fact]
    public void GetUseCategory_WithTypeCodeICMinus_ReturnsMixed()
    {
        var result = PropertyTypeToUseMapper.GetUseCategory("I-C");
        Assert.Equal("Mixed", result);
    }

    [Fact]
    public void GetUseCategory_WithTypeCodeN_ReturnsNonTaxable()
    {
        var result = PropertyTypeToUseMapper.GetUseCategory("N");
        Assert.Equal("Non Taxable", result);
    }

    [Fact]
    public void GetUseCategory_WithTypeCodeR_ReturnsResidential()
    {
        var result = PropertyTypeToUseMapper.GetUseCategory("R");
        Assert.Equal("Residential", result);
    }

    [Fact]
    public void GetUseCategory_WithTypeCodeRCMinus_ReturnsMixed()
    {
        var result = PropertyTypeToUseMapper.GetUseCategory("R-C");
        Assert.Equal("Mixed", result);
    }

    [Fact]
    public void GetUseCategory_WithNullInput_ReturnsUnknown()
    {
        var result = PropertyTypeToUseMapper.GetUseCategory(null);
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void GetUseCategory_WithEmptyString_ReturnsUnknown()
    {
        var result = PropertyTypeToUseMapper.GetUseCategory("");
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void GetUseCategory_WithInvalidTypeCode_ReturnsUnknown()
    {
        var result = PropertyTypeToUseMapper.GetUseCategory("INVALID");
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void GetUseCategory_IsCaseInsensitive()
    {
        var resultLower = PropertyTypeToUseMapper.GetUseCategory("r");
        var resultUpper = PropertyTypeToUseMapper.GetUseCategory("R");
        var resultMixed = PropertyTypeToUseMapper.GetUseCategory("i-c");

        Assert.Equal("Residential", resultLower);
        Assert.Equal("Residential", resultUpper);
        Assert.Equal("Mixed", resultMixed);
    }

    [Fact]
    public void HasUseChanged_WhenOldAndNewAreSame_ReturnsFalse()
    {
        var result = PropertyTypeToUseMapper.HasUseChanged("R", "R");
        Assert.False(result);
    }

    [Fact]
    public void HasUseChanged_WhenBothMapToSameCategory_ReturnsFalse()
    {
        var result = PropertyTypeToUseMapper.HasUseChanged("C", "I");
        Assert.False(result); // Both map to Non Residential
    }

    [Fact]
    public void HasUseChanged_WhenOldAndNewAreDifferent_ReturnsTrue()
    {
        var result = PropertyTypeToUseMapper.HasUseChanged("R", "C");
        Assert.True(result); // Residential to Non Residential
    }

    [Fact]
    public void HasUseChanged_WhenResidentialToMixed_ReturnsTrue()
    {
        var result = PropertyTypeToUseMapper.HasUseChanged("R", "R-C");
        Assert.True(result);
    }

    [Fact]
    public void HasUseChanged_WhenNullOldAndNewDifferent_ReturnsTrue()
    {
        var result = PropertyTypeToUseMapper.HasUseChanged(null, "R");
        Assert.True(result); // Unknown to Residential
    }

    [Fact]
    public void HasUseChanged_WhenBothNull_ReturnsFalse()
    {
        var result = PropertyTypeToUseMapper.HasUseChanged(null, null);
        Assert.False(result); // Unknown to Unknown
    }
}
