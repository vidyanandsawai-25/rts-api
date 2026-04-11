using NtisPlatform.Application.Attributes;

namespace NtisPlatform.Tests.Application.Attributes;

/// <summary>
/// Unit tests for SearchableAttribute to achieve 100% code coverage
/// </summary>
public class SearchableAttributeTests
{
    [Fact]
    public void SearchableAttribute_DefaultConstructor_WorksCorrectly()
    {
        var attribute = new SearchableAttribute();

        Assert.Null(attribute.EntityProperty);
    }

    [Fact]
    public void SearchableAttribute_ConstructorWithParameter_SetsEntityProperty()
    {
        var attribute = new SearchableAttribute("PropertyName");

        Assert.Equal("PropertyName", attribute.EntityProperty);
    }

    [Fact]
    public void SearchableAttribute_EntityProperty_GetSet_WorksCorrectly()
    {
        var attribute = new SearchableAttribute
        {
            EntityProperty = "TestProperty"
        };

        Assert.Equal("TestProperty", attribute.EntityProperty);
    }

    [Fact]
    public void SearchableAttribute_EntityProperty_CanBeNull()
    {
        var attribute = new SearchableAttribute
        {
            EntityProperty = null
        };

        Assert.Null(attribute.EntityProperty);
    }

    [Fact]
    public void SearchableAttribute_IsAttribute()
    {
        var attribute = new SearchableAttribute();

        Assert.IsAssignableFrom<Attribute>(attribute);
    }

    [Fact]
    public void SearchableAttribute_AttributeUsage_AllowsOneInstance()
    {
        var attributeType = typeof(SearchableAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Property, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
    }
}

/// <summary>
/// Unit tests for SortableAttribute to achieve 100% code coverage
/// </summary>
public class SortableAttributeTests
{
    [Fact]
    public void SortableAttribute_DefaultConstructor_WorksCorrectly()
    {
        var attribute = new SortableAttribute();

        Assert.Null(attribute.EntityProperty);
    }

    [Fact]
    public void SortableAttribute_ConstructorWithParameter_SetsEntityProperty()
    {
        var attribute = new SortableAttribute("PropertyName");

        Assert.Equal("PropertyName", attribute.EntityProperty);
    }

    [Fact]
    public void SortableAttribute_EntityProperty_GetSet_WorksCorrectly()
    {
        var attribute = new SortableAttribute
        {
            EntityProperty = "TestProperty"
        };

        Assert.Equal("TestProperty", attribute.EntityProperty);
    }

    [Fact]
    public void SortableAttribute_EntityProperty_CanBeNull()
    {
        var attribute = new SortableAttribute
        {
            EntityProperty = null
        };

        Assert.Null(attribute.EntityProperty);
    }

    [Fact]
    public void SortableAttribute_IsAttribute()
    {
        var attribute = new SortableAttribute();

        Assert.IsAssignableFrom<Attribute>(attribute);
    }

    [Fact]
    public void SortableAttribute_AttributeUsage_AllowsOneInstance()
    {
        var attributeType = typeof(SortableAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Property, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
    }
}
