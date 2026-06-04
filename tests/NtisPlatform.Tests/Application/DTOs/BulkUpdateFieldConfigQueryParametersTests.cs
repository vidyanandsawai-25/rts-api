using FluentAssertions;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.CommonDetails;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class BulkUpdateFieldConfigQueryParametersTests
{
    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_InheritsFromBaseQueryParameters()
    {
        // Arrange & Act
        var queryParameters = new BulkUpdateFieldConfigQueryParameters();

        // Assert
        queryParameters.Should().BeAssignableTo<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>();
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_BulkUpdateMasterId_HasFilterableAttribute()
    {
        // Arrange
        var property = typeof(BulkUpdateFieldConfigQueryParameters).GetProperty(nameof(BulkUpdateFieldConfigQueryParameters.BulkUpdateMasterId));

        // Act
        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();

        // Assert
        hasFilterable.Should().BeTrue();
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_FieldName_HasFilterableSortableSearchableAttributes()
    {
        // Arrange
        var property = typeof(BulkUpdateFieldConfigQueryParameters).GetProperty(nameof(BulkUpdateFieldConfigQueryParameters.FieldName));

        // Act
        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();
        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();
        var hasSearchable = property?.GetCustomAttributes(typeof(SearchableAttribute), false).Any();

        // Assert
        hasFilterable.Should().BeTrue();
        hasSortable.Should().BeTrue();
        hasSearchable.Should().BeTrue();
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_DisplayName_HasFilterableSortableSearchableAttributes()
    {
        // Arrange
        var property = typeof(BulkUpdateFieldConfigQueryParameters).GetProperty(nameof(BulkUpdateFieldConfigQueryParameters.DisplayName));

        // Act
        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();
        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();
        var hasSearchable = property?.GetCustomAttributes(typeof(SearchableAttribute), false).Any();

        // Assert
        hasFilterable.Should().BeTrue();
        hasSortable.Should().BeTrue();
        hasSearchable.Should().BeTrue();
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_ControlType_HasFilterableAndSortableAttributes()
    {
        // Arrange
        var property = typeof(BulkUpdateFieldConfigQueryParameters).GetProperty(nameof(BulkUpdateFieldConfigQueryParameters.ControlType));

        // Act
        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();
        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        // Assert
        hasFilterable.Should().BeTrue();
        hasSortable.Should().BeTrue();
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_DataType_HasFilterableAndSortableAttributes()
    {
        // Arrange
        var property = typeof(BulkUpdateFieldConfigQueryParameters).GetProperty(nameof(BulkUpdateFieldConfigQueryParameters.DataType));

        // Act
        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();
        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        // Assert
        hasFilterable.Should().BeTrue();
        hasSortable.Should().BeTrue();
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_SequenceNo_HasFilterableAndSortableAttributes()
    {
        // Arrange
        var property = typeof(BulkUpdateFieldConfigQueryParameters).GetProperty(nameof(BulkUpdateFieldConfigQueryParameters.SequenceNo));

        // Act
        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();
        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        // Assert
        hasFilterable.Should().BeTrue();
        hasSortable.Should().BeTrue();
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_CanSetBulkUpdateMasterId()
    {
        // Arrange
        var queryParameters = new BulkUpdateFieldConfigQueryParameters();

        // Act
        queryParameters.BulkUpdateMasterId = 1;

        // Assert
        queryParameters.BulkUpdateMasterId.Should().Be(1);
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_CanSetFieldName()
    {
        // Arrange
        var queryParameters = new BulkUpdateFieldConfigQueryParameters();

        // Act
        queryParameters.FieldName = "PropertyType";

        // Assert
        queryParameters.FieldName.Should().Be("PropertyType");
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_CanSetDisplayName()
    {
        // Arrange
        var queryParameters = new BulkUpdateFieldConfigQueryParameters();

        // Act
        queryParameters.DisplayName = "Property Type";

        // Assert
        queryParameters.DisplayName.Should().Be("Property Type");
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_CanSetControlType()
    {
        // Arrange
        var queryParameters = new BulkUpdateFieldConfigQueryParameters();

        // Act
        queryParameters.ControlType = "Dropdown";

        // Assert
        queryParameters.ControlType.Should().Be("Dropdown");
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_CanSetDataType()
    {
        // Arrange
        var queryParameters = new BulkUpdateFieldConfigQueryParameters();

        // Act
        queryParameters.DataType = "String";

        // Assert
        queryParameters.DataType.Should().Be("String");
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_CanSetSequenceNo()
    {
        // Arrange
        var queryParameters = new BulkUpdateFieldConfigQueryParameters();

        // Act
        queryParameters.SequenceNo = 5;

        // Assert
        queryParameters.SequenceNo.Should().Be(5);
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_AllPropertiesAreNullableOrOptional()
    {
        // Arrange & Act
        var queryParameters = new BulkUpdateFieldConfigQueryParameters();

        // Assert - All properties should be nullable (no required values)
        queryParameters.BulkUpdateMasterId.Should().BeNull();
        queryParameters.FieldName.Should().BeNull();
        queryParameters.DisplayName.Should().BeNull();
        queryParameters.ControlType.Should().BeNull();
        queryParameters.DataType.Should().BeNull();
        queryParameters.SequenceNo.Should().BeNull();
    }

    [Fact]
    public void BulkUpdateFieldConfigQueryParameters_CanSetMultipleFilters()
    {
        // Arrange
        var queryParameters = new BulkUpdateFieldConfigQueryParameters();

        // Act
        queryParameters.BulkUpdateMasterId = 1;
        queryParameters.FieldName = "Ward";
        queryParameters.ControlType = "Dropdown";
        queryParameters.SequenceNo = 2;

        // Assert
        queryParameters.BulkUpdateMasterId.Should().Be(1);
        queryParameters.FieldName.Should().Be("Ward");
        queryParameters.ControlType.Should().Be("Dropdown");
        queryParameters.SequenceNo.Should().Be(2);
    }
}
