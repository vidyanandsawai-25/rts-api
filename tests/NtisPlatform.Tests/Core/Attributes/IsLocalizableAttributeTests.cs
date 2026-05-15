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
    public void IdProperty_DefaultValue_IsId()
    {
        // Arrange & Act
        var attribute = new IsLocalizableAttribute("TestResource");

        // Assert
        Assert.Equal("Id", attribute.IdProperty);
    }

    [Fact]
    public void IdProperty_CanBeSet_WorksCorrectly()
    {
        // Arrange
        var attribute = new IsLocalizableAttribute("TestResource")
        {
            IdProperty = "CustomId"
        };

        // Act & Assert
        Assert.Equal("CustomId", attribute.IdProperty);
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
    public void Attribute_WithCustomIdProperty_RetrievesCorrectly()
    {
        // Arrange
        var type = typeof(TestClass);
        var property = type.GetProperty(nameof(TestClass.Description));

        // Act
        var attribute = property?.GetCustomAttribute<IsLocalizableAttribute>(false);

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("DescriptionResource", attribute.Resource);
        Assert.Equal("EntityId", attribute.IdProperty);
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
    public void Attribute_MultiplePropertiesWithDifferentConfigurations_WorkCorrectly()
    {
        // Arrange
        var type = typeof(TestClass);

        // Act
        var nameProperty = type.GetProperty(nameof(TestClass.Name));
        var descProperty = type.GetProperty(nameof(TestClass.Description));
        var titleProperty = type.GetProperty(nameof(TestClass.Title));

        var nameAttr = nameProperty?.GetCustomAttribute<IsLocalizableAttribute>();
        var descAttr = descProperty?.GetCustomAttribute<IsLocalizableAttribute>();
        var titleAttr = titleProperty?.GetCustomAttribute<IsLocalizableAttribute>();

        // Assert
        Assert.NotNull(nameAttr);
        Assert.Equal("TestResource", nameAttr.Resource);
        Assert.Equal("Id", nameAttr.IdProperty);

        Assert.NotNull(descAttr);
        Assert.Equal("DescriptionResource", descAttr.Resource);
        Assert.Equal("EntityId", descAttr.IdProperty);

        Assert.NotNull(titleAttr);
        Assert.Equal("TitleResource", titleAttr.Resource);
        Assert.Equal("Id", titleAttr.IdProperty);
    }

    private class TestClass
    {
        public int Id { get; set; }
        public int EntityId { get; set; }

        [IsLocalizable("TestResource")]
        public string Name { get; set; } = string.Empty;

        [IsLocalizable("DescriptionResource", IdProperty = "EntityId")]
        public string Description { get; set; } = string.Empty;

        [IsLocalizable("TitleResource")]
        public string Title { get; set; } = string.Empty;

        public string NonLocalizable { get; set; } = string.Empty;
    }
}
