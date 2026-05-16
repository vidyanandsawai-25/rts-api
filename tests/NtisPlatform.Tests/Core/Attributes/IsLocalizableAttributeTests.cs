using System.Reflection;
using Xunit;
using NtisPlatform.Core;

namespace NtisPlatform.Tests.Core.Attributes;

public class IsLocalizableAttributeTests
{
    [Fact]
    public void Constructor_WithResource_SetsResourceProperty()
    {
        // Arrange & Act
        var attribute = new IsLocalizableAttribute("TestResource");

        // Assert
        Assert.Equal("TestResource", attribute.Resource);
    }

    [Fact]
    public void Constructor_WithDifferentResources_SetsCorrectly()
    {
        // Arrange & Act
        var attribute1 = new IsLocalizableAttribute("Resource1");
        var attribute2 = new IsLocalizableAttribute("Resource2");

        // Assert
        Assert.Equal("Resource1", attribute1.Resource);
        Assert.Equal("Resource2", attribute2.Resource);
    }

    [Fact]
    public void Attribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var attributeType = typeof(IsLocalizableAttribute);

        // Act
        var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Property, attributeUsage.ValidOn);
    }

    [Fact]
    public void Attribute_CanBeAppliedToProperty()
    {
        // Arrange
        var type = typeof(TestClass);
        var property = type.GetProperty(nameof(TestClass.Name));

        // Act
        var attributes = property?.GetCustomAttributes<IsLocalizableAttribute>(false);

        // Assert
        Assert.NotNull(attributes);
        Assert.Single(attributes);
        Assert.Equal("TestResource", attributes.First().Resource);
    }

    [Fact]
    public void Attribute_IsAttributeType()
    {
        // Arrange & Act
        var attribute = new IsLocalizableAttribute("TestResource");

        // Assert
        Assert.IsAssignableFrom<Attribute>(attribute);
    }

    [Fact]
    public void Attribute_WithEmptyResource_SetsCorrectly()
    {
        // Arrange & Act
        var attribute = new IsLocalizableAttribute(string.Empty);

        // Assert
        Assert.Equal(string.Empty, attribute.Resource);
    }

    [Fact]
    public void Attribute_MultiplePropertiesWithDifferentResources_WorkCorrectly()
    {
        // Arrange
        var type = typeof(TestClass);

        // Act
        var nameAttr = type.GetProperty(nameof(TestClass.Name))?.GetCustomAttribute<IsLocalizableAttribute>();
        var descAttr = type.GetProperty(nameof(TestClass.Description))?.GetCustomAttribute<IsLocalizableAttribute>();
        var titleAttr = type.GetProperty(nameof(TestClass.Title))?.GetCustomAttribute<IsLocalizableAttribute>();

        // Assert
        Assert.NotNull(nameAttr);
        Assert.Equal("TestResource", nameAttr.Resource);

        Assert.NotNull(descAttr);
        Assert.Equal("DescriptionResource", descAttr.Resource);

        Assert.NotNull(titleAttr);
        Assert.Equal("TitleResource", titleAttr.Resource);
    }

    private class TestClass
    {
        public int Id { get; set; }

        [IsLocalizable("TestResource")]
        public string Name { get; set; } = string.Empty;

        [IsLocalizable("DescriptionResource")]
        public string Description { get; set; } = string.Empty;

        [IsLocalizable("TitleResource")]
        public string Title { get; set; } = string.Empty;

        public string NonLocalizable { get; set; } = string.Empty;
    }
}
