using NtisPlatform.Core;
using Xunit;

namespace NtisPlatform.Tests.Core;

/// <summary>
/// Comprehensive tests for Core attributes to achieve 100% code coverage
/// </summary>
public class AttributesTests
{
    #region IsLocalizableAttribute Tests

    [Fact]
    public void IsLocalizableAttribute_Constructor_SetsResource()
    {
        // Arrange
        var resource = "PropertyMaster";

        // Act
        var attribute = new IsLocalizableAttribute(resource);

        // Assert
        Assert.Equal(resource, attribute.Resource);
    }

    [Fact]
    public void IsLocalizableAttribute_IdProperty_DefaultsToId()
    {
        // Arrange
        var resource = "PropertyMaster";

        // Act
        var attribute = new IsLocalizableAttribute(resource);

        // Assert
        Assert.Equal("Id", attribute.IdProperty);
    }

    [Fact]
    public void IsLocalizableAttribute_IdProperty_CanBeSet()
    {
        // Arrange
        var resource = "PropertyMaster";
        var attribute = new IsLocalizableAttribute(resource);

        // Act
        attribute.IdProperty = "CustomId";

        // Assert
        Assert.Equal("CustomId", attribute.IdProperty);
    }

    [Fact]
    public void IsLocalizableAttribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var type = typeof(IsLocalizableAttribute);

        // Act
        var usageAttributes = (AttributeUsageAttribute[])type.GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        // Assert
        Assert.Single(usageAttributes);
        Assert.Equal(AttributeTargets.Property, usageAttributes[0].ValidOn);
    }

    [Fact]
    public void IsLocalizableAttribute_IsSealed()
    {
        // Arrange
        var type = typeof(IsLocalizableAttribute);

        // Act & Assert
        Assert.True(type.IsSealed);
    }

    #endregion

    #region LocalizableEntityAttribute Tests

    [Fact]
    public void LocalizableEntityAttribute_Constructor_WithSingleType_SetsEntityTypes()
    {
        // Arrange
        var entityType = typeof(TestEntity);

        // Act
        var attribute = new LocalizableEntityAttribute(entityType);

        // Assert
        Assert.Single(attribute.EntityTypes);
        Assert.Contains(entityType, attribute.EntityTypes);
    }

    [Fact]
    public void LocalizableEntityAttribute_Constructor_WithMultipleTypes_SetsEntityTypes()
    {
        // Arrange
        var entityType1 = typeof(TestEntity);
        var entityType2 = typeof(TestEntity2);

        // Act
        var attribute = new LocalizableEntityAttribute(entityType1, entityType2);

        // Assert
        Assert.Equal(2, attribute.EntityTypes.Length);
        Assert.Contains(entityType1, attribute.EntityTypes);
        Assert.Contains(entityType2, attribute.EntityTypes);
    }

    [Fact]
    public void LocalizableEntityAttribute_Constructor_WithNullArray_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LocalizableEntityAttribute(null!));
        Assert.Contains("At least one entity type must be specified", exception.Message);
    }

    [Fact]
    public void LocalizableEntityAttribute_Constructor_WithEmptyArray_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LocalizableEntityAttribute(Array.Empty<Type>()));
        Assert.Contains("At least one entity type must be specified", exception.Message);
    }

    [Fact]
    public void LocalizableEntityAttribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var type = typeof(LocalizableEntityAttribute);

        // Act
        var usageAttributes = (AttributeUsageAttribute[])type.GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        // Assert
        Assert.Single(usageAttributes);
        Assert.Equal(AttributeTargets.Class, usageAttributes[0].ValidOn);
        Assert.False(usageAttributes[0].AllowMultiple);
        Assert.False(usageAttributes[0].Inherited);
    }

    [Fact]
    public void LocalizableEntityAttribute_IsSealed()
    {
        // Arrange
        var type = typeof(LocalizableEntityAttribute);

        // Act & Assert
        Assert.True(type.IsSealed);
    }

    #endregion

    #region Test Helper Classes

    private class TestEntity
    {
        public int Id { get; set; }
    }

    private class TestEntity2
    {
        public int Id { get; set; }
    }

    #endregion
}
