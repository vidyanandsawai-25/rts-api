using System.Reflection;
using Xunit;
using NtisPlatform.Core;

namespace NtisPlatform.Tests.Core.Attributes;

public class LocalizableEntityAttributeTests
{
    private class TestEntity1 { }
    private class TestEntity2 { }
    private class TestEntity3 { }

    [Fact]
    public void Constructor_WithSingleEntityType_SetsCorrectly()
    {
        // Arrange & Act
        var attribute = new LocalizableEntityAttribute(typeof(TestEntity1));

        // Assert
        Assert.Single(attribute.EntityTypes);
        Assert.Equal(typeof(TestEntity1), attribute.EntityTypes[0]);
    }

    [Fact]
    public void Constructor_WithMultipleEntityTypes_SetsCorrectly()
    {
        // Arrange & Act
        var attribute = new LocalizableEntityAttribute(typeof(TestEntity1), typeof(TestEntity2), typeof(TestEntity3));

        // Assert
        Assert.Equal(3, attribute.EntityTypes.Length);
        Assert.Equal(typeof(TestEntity1), attribute.EntityTypes[0]);
        Assert.Equal(typeof(TestEntity2), attribute.EntityTypes[1]);
        Assert.Equal(typeof(TestEntity3), attribute.EntityTypes[2]);
    }

    [Fact]
    public void Constructor_WithNullEntityTypes_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LocalizableEntityAttribute(null!));
        Assert.Contains("At least one entity type must be specified", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyEntityTypes_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LocalizableEntityAttribute(Array.Empty<Type>()));
        Assert.Contains("At least one entity type must be specified", exception.Message);
    }

    [Fact]
    public void Attribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var attributeType = typeof(LocalizableEntityAttribute);

        // Act
        var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Class, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
        Assert.False(attributeUsage.Inherited);
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var type = typeof(SingleEntityDto);
        var attributes = type.GetCustomAttributes<LocalizableEntityAttribute>(false);

        // Assert
        Assert.Single(attributes);
        Assert.Single(attributes.First().EntityTypes);
        Assert.Equal(typeof(TestEntity1), attributes.First().EntityTypes[0]);
    }

    [Fact]
    public void Attribute_WithMultipleEntities_RetrievesCorrectly()
    {
        // Arrange & Act
        var type = typeof(MultiEntityDto);
        var attribute = type.GetCustomAttribute<LocalizableEntityAttribute>(false);

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(2, attribute.EntityTypes.Length);
        Assert.Contains(typeof(TestEntity1), attribute.EntityTypes);
        Assert.Contains(typeof(TestEntity2), attribute.EntityTypes);
    }

    [Fact]
    public void Attribute_IsAttributeType()
    {
        // Arrange & Act
        var attribute = new LocalizableEntityAttribute(typeof(TestEntity1));

        // Assert
        Assert.IsAssignableFrom<Attribute>(attribute);
    }

    [Fact]
    public void Attribute_NotInherited_DoesNotApplyToDerivedClasses()
    {
        // Arrange & Act
        var type = typeof(DerivedDto);
        var attribute = type.GetCustomAttribute<LocalizableEntityAttribute>(false);

        // Assert
        Assert.Null(attribute);
    }

    [Fact]
    public void Attribute_WithThreeEntityTypes_StoresAll()
    {
        // Arrange & Act
        var attribute = new LocalizableEntityAttribute(
            typeof(TestEntity1),
            typeof(TestEntity2),
            typeof(TestEntity3)
        );

        // Assert
        Assert.Equal(3, attribute.EntityTypes.Length);
        Assert.Equal(typeof(TestEntity1), attribute.EntityTypes[0]);
        Assert.Equal(typeof(TestEntity2), attribute.EntityTypes[1]);
        Assert.Equal(typeof(TestEntity3), attribute.EntityTypes[2]);
    }

    [Fact]
    public void Attribute_DifferentInstances_HaveIndependentArrays()
    {
        // Arrange & Act
        var attribute1 = new LocalizableEntityAttribute(typeof(TestEntity1));
        var attribute2 = new LocalizableEntityAttribute(typeof(TestEntity2), typeof(TestEntity3));

        // Assert
        Assert.Single(attribute1.EntityTypes);
        Assert.Equal(2, attribute2.EntityTypes.Length);
        Assert.NotEqual(attribute1.EntityTypes, attribute2.EntityTypes);
    }

    [LocalizableEntity(typeof(TestEntity1))]
    private class SingleEntityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [LocalizableEntity(typeof(TestEntity1), typeof(TestEntity2))]
    private class MultiEntityDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    private class DerivedDto : SingleEntityDto
    {
        public string ExtraProperty { get; set; } = string.Empty;
    }
}
