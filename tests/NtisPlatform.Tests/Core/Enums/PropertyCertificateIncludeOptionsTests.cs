using NtisPlatform.Core.Enums;
using Xunit;

namespace NtisPlatform.Tests.Core.Enums;

/// <summary>
/// Comprehensive tests for PropertyCertificateIncludeOptions enum to achieve 100% code coverage
/// </summary>
public class PropertyCertificateIncludeOptionsTests
{
    [Fact]
    public void PropertyCertificateIncludeOptions_None_HasCorrectValue()
    {
        // Arrange & Act
        var value = PropertyCertificateIncludeOptions.None;

        // Assert
        Assert.Equal(0, (int)value);
    }

    [Fact]
    public void PropertyCertificateIncludeOptions_CertificateType_HasCorrectValue()
    {
        // Arrange & Act
        var value = PropertyCertificateIncludeOptions.CertificateType;

        // Assert
        Assert.Equal(1, (int)value);
    }

    [Fact]
    public void PropertyCertificateIncludeOptions_DocumentBinding_HasCorrectValue()
    {
        // Arrange & Act
        var value = PropertyCertificateIncludeOptions.DocumentBinding;

        // Assert
        Assert.Equal(2, (int)value);
    }

    [Fact]
    public void PropertyCertificateIncludeOptions_Document_HasCorrectValue()
    {
        // Arrange & Act
        var value = PropertyCertificateIncludeOptions.Document;

        // Assert
        Assert.Equal(4, (int)value);
    }

    [Fact]
    public void PropertyCertificateIncludeOptions_All_IncludesAllFlags()
    {
        // Arrange & Act
        var all = PropertyCertificateIncludeOptions.All;

        // Assert
        Assert.True(all.HasFlag(PropertyCertificateIncludeOptions.CertificateType));
        Assert.True(all.HasFlag(PropertyCertificateIncludeOptions.DocumentBinding));
        Assert.True(all.HasFlag(PropertyCertificateIncludeOptions.Document));
        Assert.Equal(7, (int)all); // 1 + 2 + 4 = 7
    }

    [Fact]
    public void PropertyCertificateIncludeOptions_CombinedFlags_WorkCorrectly()
    {
        // Arrange & Act
        var combined = PropertyCertificateIncludeOptions.CertificateType | PropertyCertificateIncludeOptions.DocumentBinding;

        // Assert
        Assert.True(combined.HasFlag(PropertyCertificateIncludeOptions.CertificateType));
        Assert.True(combined.HasFlag(PropertyCertificateIncludeOptions.DocumentBinding));
        Assert.False(combined.HasFlag(PropertyCertificateIncludeOptions.Document));
        Assert.Equal(3, (int)combined); // 1 + 2 = 3
    }

    [Fact]
    public void PropertyCertificateIncludeOptions_IsFlagsEnum()
    {
        // Arrange
        var type = typeof(PropertyCertificateIncludeOptions);

        // Act
        var hasFlagsAttribute = type.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;

        // Assert
        Assert.True(hasFlagsAttribute);
    }

    [Fact]
    public void PropertyCertificateIncludeOptions_AllValuesAreDefined()
    {
        // Arrange
        var expectedValues = new[]
        {
            PropertyCertificateIncludeOptions.None,
            PropertyCertificateIncludeOptions.CertificateType,
            PropertyCertificateIncludeOptions.DocumentBinding,
            PropertyCertificateIncludeOptions.Document,
            PropertyCertificateIncludeOptions.All
        };

        // Act
        var actualValues = Enum.GetValues<PropertyCertificateIncludeOptions>();

        // Assert
        Assert.Equal(expectedValues.Length, actualValues.Length);
        foreach (var expected in expectedValues)
        {
            Assert.Contains(expected, actualValues);
        }
    }
}
