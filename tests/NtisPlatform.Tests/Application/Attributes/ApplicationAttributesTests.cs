using NtisPlatform.Application.Attributes;
using System.ComponentModel;
using Xunit;

namespace NtisPlatform.Tests.Application.Attributes;

public class ApplicationAttributesTests
{
    [Fact]
    public void SearchableAttribute_CreatesInstance()
    {
        // Act
        var attribute = new SearchableAttribute();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void SearchableAttribute_IsAttributeType()
    {
        // Arrange
        var attribute = new SearchableAttribute();

        // Assert
        Assert.IsAssignableFrom<Attribute>(attribute);
    }

    [Fact]
    public void SearchableAttribute_CanBeAppliedToProperty()
    {
        // Arrange & Act
        var type = typeof(TestModel);
        var property = type.GetProperty(nameof(TestModel.SearchableName));
        var attributes = property?.GetCustomAttributes(typeof(SearchableAttribute), false);

        // Assert
        Assert.NotNull(attributes);
        Assert.NotEmpty(attributes!);
    }

    [Fact]
    public void SortableAttribute_CreatesInstance()
    {
        // Act
        var attribute = new SortableAttribute();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void SortableAttribute_IsAttributeType()
    {
        // Arrange
        var attribute = new SortableAttribute();

        // Assert
        Assert.IsAssignableFrom<Attribute>(attribute);
    }

    [Fact]
    public void SortableAttribute_CanBeAppliedToProperty()
    {
        // Arrange & Act
        var type = typeof(TestModel);
        var property = type.GetProperty(nameof(TestModel.SortableName));
        var attributes = property?.GetCustomAttributes(typeof(SortableAttribute), false);

        // Assert
        Assert.NotNull(attributes);
        Assert.NotEmpty(attributes!);
    }

    [Fact]
    public void FilterableAttribute_CreatesInstance()
    {
        // Act
        var attribute = new FilterableAttribute();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void FilterableAttribute_IsAttributeType()
    {
        // Arrange
        var attribute = new FilterableAttribute();

        // Assert
        Assert.IsAssignableFrom<Attribute>(attribute);
    }

    [Fact]
    public void FilterableAttribute_CanBeAppliedToProperty()
    {
        // Arrange & Act
        var type = typeof(TestModel);
        var property = type.GetProperty(nameof(TestModel.FilterableName));
        var attributes = property?.GetCustomAttributes(typeof(FilterableAttribute), false);

        // Assert
        Assert.NotNull(attributes);
        Assert.NotEmpty(attributes!);
    }

    [Fact]
    public void MultipleAttributes_CanBeAppliedTogether()
    {
        // Arrange & Act
        var type = typeof(TestModel);
        var property = type.GetProperty(nameof(TestModel.FullyAttributedName));
        var searchableAttrs = property?.GetCustomAttributes(typeof(SearchableAttribute), false);
        var sortableAttrs = property?.GetCustomAttributes(typeof(SortableAttribute), false);
        var filterableAttrs = property?.GetCustomAttributes(typeof(FilterableAttribute), false);

        // Assert
        Assert.NotEmpty(searchableAttrs!);
        Assert.NotEmpty(sortableAttrs!);
        Assert.NotEmpty(filterableAttrs!);
    }

    private class TestModel
    {
        [Searchable]
        public string SearchableName { get; set; } = string.Empty;

        [Sortable]
        public string SortableName { get; set; } = string.Empty;

        [Filterable]
        public string FilterableName { get; set; } = string.Empty;

        [Searchable]
        [Sortable]
        [Filterable]
        public string FullyAttributedName { get; set; } = string.Empty;
    }
}
