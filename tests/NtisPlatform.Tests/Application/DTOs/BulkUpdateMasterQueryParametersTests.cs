using FluentAssertions;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.CommonDetails;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class BulkUpdateMasterQueryParametersTests
{
    [Fact]
    public void BulkUpdateMasterQueryParameters_InheritsFromBaseQueryParameters()
    {
        // Arrange & Act
        var queryParameters = new BulkUpdateMasterQueryParameters();

        // Assert
        queryParameters.Should().BeAssignableTo<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>();
    }

    [Fact]
    public void BulkUpdateMasterQueryParameters_UpdateCode_HasFilterableSortableSearchableAttributes()
    {
        // Arrange
        var property = typeof(BulkUpdateMasterQueryParameters).GetProperty(nameof(BulkUpdateMasterQueryParameters.UpdateCode));

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
    public void BulkUpdateMasterQueryParameters_UpdateName_HasFilterableSortableSearchableAttributes()
    {
        // Arrange
        var property = typeof(BulkUpdateMasterQueryParameters).GetProperty(nameof(BulkUpdateMasterQueryParameters.UpdateName));

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
    public void BulkUpdateMasterQueryParameters_ReferenceTableName_HasFilterableSortableSearchableAttributes()
    {
        // Arrange
        var property = typeof(BulkUpdateMasterQueryParameters).GetProperty(nameof(BulkUpdateMasterQueryParameters.ReferenceTableName));

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
    public void BulkUpdateMasterQueryParameters_DisplaySequence_HasFilterableAndSortableAttributes()
    {
        // Arrange
        var property = typeof(BulkUpdateMasterQueryParameters).GetProperty(nameof(BulkUpdateMasterQueryParameters.DisplaySequence));

        // Act
        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();
        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        // Assert
        hasFilterable.Should().BeTrue();
        hasSortable.Should().BeTrue();
    }

    [Fact]
    public void BulkUpdateMasterQueryParameters_CanSetUpdateCode()
    {
        // Arrange
        var queryParameters = new BulkUpdateMasterQueryParameters();

        // Act
        queryParameters.UpdateCode = "PROP_TYPE";

        // Assert
        queryParameters.UpdateCode.Should().Be("PROP_TYPE");
    }

    [Fact]
    public void BulkUpdateMasterQueryParameters_CanSetUpdateName()
    {
        // Arrange
        var queryParameters = new BulkUpdateMasterQueryParameters();

        // Act
        queryParameters.UpdateName = "Property Type Update";

        // Assert
        queryParameters.UpdateName.Should().Be("Property Type Update");
    }

    [Fact]
    public void BulkUpdateMasterQueryParameters_CanSetReferenceTableName()
    {
        // Arrange
        var queryParameters = new BulkUpdateMasterQueryParameters();

        // Act
        queryParameters.ReferenceTableName = "PropertyTypeMaster";

        // Assert
        queryParameters.ReferenceTableName.Should().Be("PropertyTypeMaster");
    }

    [Fact]
    public void BulkUpdateMasterQueryParameters_CanSetDisplaySequence()
    {
        // Arrange
        var queryParameters = new BulkUpdateMasterQueryParameters();

        // Act
        queryParameters.DisplaySequence = 5;

        // Assert
        queryParameters.DisplaySequence.Should().Be(5);
    }

    [Fact]
    public void BulkUpdateMasterQueryParameters_AllPropertiesAreNullableOrOptional()
    {
        // Arrange & Act
        var queryParameters = new BulkUpdateMasterQueryParameters();

        // Assert - All properties should be nullable (no required values)
        queryParameters.UpdateCode.Should().BeNull();
        queryParameters.UpdateName.Should().BeNull();
        queryParameters.ReferenceTableName.Should().BeNull();
        queryParameters.DisplaySequence.Should().BeNull();
    }

    [Fact]
    public void BulkUpdateMasterQueryParameters_CanSetMultipleFilters()
    {
        // Arrange
        var queryParameters = new BulkUpdateMasterQueryParameters();

        // Act
        queryParameters.UpdateCode = "PROP";
        queryParameters.UpdateName = "Property";
        queryParameters.ReferenceTableName = "PropertyTypeMaster";
        queryParameters.DisplaySequence = 1;

        // Assert
        queryParameters.UpdateCode.Should().Be("PROP");
        queryParameters.UpdateName.Should().Be("Property");
        queryParameters.ReferenceTableName.Should().Be("PropertyTypeMaster");
        queryParameters.DisplaySequence.Should().Be(1);
    }
}
