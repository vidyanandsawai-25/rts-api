using NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// Unit tests for PropertyWorkflowStageMasterQueryParameters
/// </summary>
public class PropertyWorkflowStageMasterQueryParametersTests
{
    #region Initialization Tests

    [Fact]
    public void Create_DefaultQueryParameters_AllPropertiesInitialized()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters();

        // Assert
        Assert.NotNull(queryParams);
        Assert.Null(queryParams.StageName);
        Assert.Null(queryParams.DisplayOrder);
        Assert.Null(queryParams.IsActive);
    }

    [Fact]
    public void Create_QueryParameters_WithStageName()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = "GeoSequencing"
        };

        // Assert
        Assert.Equal("GeoSequencing", queryParams.StageName);
        Assert.Null(queryParams.DisplayOrder);
        Assert.Null(queryParams.IsActive);
    }

    [Fact]
    public void Create_QueryParameters_WithDisplayOrder()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            DisplayOrder = 1
        };

        // Assert
        Assert.Null(queryParams.StageName);
        Assert.Equal(1, queryParams.DisplayOrder);
        Assert.Null(queryParams.IsActive);
    }

    [Fact]
    public void Create_QueryParameters_WithIsActive()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            IsActive = true
        };

        // Assert
        Assert.Null(queryParams.StageName);
        Assert.Null(queryParams.DisplayOrder);
        Assert.True(queryParams.IsActive);
    }

    [Fact]
    public void Create_QueryParameters_WithAllFilters()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = "Survey",
            DisplayOrder = 2,
            IsActive = true
        };

        // Assert
        Assert.Equal("Survey", queryParams.StageName);
        Assert.Equal(2, queryParams.DisplayOrder);
        Assert.True(queryParams.IsActive);
    }

    #endregion

    #region StageName Filter Tests

    [Fact]
    public void StageName_CanBeSetToNull()
    {
        // Arrange
        var queryParams = new PropertyWorkflowStageMasterQueryParameters { StageName = "Test" };

        // Act
        queryParams.StageName = null;

        // Assert
        Assert.Null(queryParams.StageName);
    }

    [Fact]
    public void StageName_CanBeSetToEmptyString()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, queryParams.StageName);
    }

    [Fact]
    public void StageName_CanBeSetToMaxLength()
    {
        // Arrange
        var maxLengthName = new string('A', 100);

        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = maxLengthName
        };

        // Assert
        Assert.Equal(maxLengthName, queryParams.StageName);
    }

    [Fact]
    public void StageName_CanBeSetToPartialMatch()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = "Geo"
        };

        // Assert
        Assert.Equal("Geo", queryParams.StageName);
    }

    [Fact]
    public void StageName_CaseSensitive()
    {
        // Arrange
        var queryParams1 = new PropertyWorkflowStageMasterQueryParameters { StageName = "GeoSequencing" };
        var queryParams2 = new PropertyWorkflowStageMasterQueryParameters { StageName = "geosequencing" };

        // Assert
        Assert.NotEqual(queryParams1.StageName, queryParams2.StageName);
    }

    #endregion

    #region DisplayOrder Filter Tests

    [Fact]
    public void DisplayOrder_CanBeSetToNull()
    {
        // Arrange
        var queryParams = new PropertyWorkflowStageMasterQueryParameters { DisplayOrder = 1 };

        // Act
        queryParams.DisplayOrder = null;

        // Assert
        Assert.Null(queryParams.DisplayOrder);
    }

    [Fact]
    public void DisplayOrder_CanBeSetToZero()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            DisplayOrder = 0
        };

        // Assert
        Assert.Equal(0, queryParams.DisplayOrder);
    }

    [Fact]
    public void DisplayOrder_CanBeSetToPositiveValue()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            DisplayOrder = 9
        };

        // Assert
        Assert.Equal(9, queryParams.DisplayOrder);
    }

    [Fact]
    public void DisplayOrder_CanBeSetToNegativeValue()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            DisplayOrder = -1
        };

        // Assert
        Assert.Equal(-1, queryParams.DisplayOrder);
    }

    [Fact]
    public void DisplayOrder_CanBeSetToMaxIntValue()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            DisplayOrder = int.MaxValue
        };

        // Assert
        Assert.Equal(int.MaxValue, queryParams.DisplayOrder);
    }

    [Fact]
    public void DisplayOrder_MultipleValues_AllValid()
    {
        // Arrange
        var orders = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // Act & Assert
        foreach (var order in orders)
        {
            var queryParams = new PropertyWorkflowStageMasterQueryParameters { DisplayOrder = order };
            Assert.Equal(order, queryParams.DisplayOrder);
        }
    }

    #endregion

    #region IsActive Filter Tests

    [Fact]
    public void IsActive_CanBeSetToNull()
    {
        // Arrange
        var queryParams = new PropertyWorkflowStageMasterQueryParameters { IsActive = true };

        // Act
        queryParams.IsActive = null;

        // Assert
        Assert.Null(queryParams.IsActive);
    }

    [Fact]
    public void IsActive_CanBeSetToTrue()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            IsActive = true
        };

        // Assert
        Assert.True(queryParams.IsActive);
    }

    [Fact]
    public void IsActive_CanBeSetToFalse()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            IsActive = false
        };

        // Assert
        Assert.False(queryParams.IsActive);
    }

    [Fact]
    public void IsActive_CanToggleBetweenValues()
    {
        // Arrange
        var queryParams = new PropertyWorkflowStageMasterQueryParameters { IsActive = true };

        // Act
        queryParams.IsActive = false;

        // Assert
        Assert.False(queryParams.IsActive);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public void Pagination_DefaultValues_AreSet()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters();

        // Assert
        // Default pagination values from BaseQueryParameters
        Assert.True(queryParams.PageNumber > 0);
        Assert.True(queryParams.PageSize > 0);
    }

    [Fact]
    public void Pagination_CanSetPageNumber()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            PageNumber = 2
        };

        // Assert
        Assert.Equal(2, queryParams.PageNumber);
    }

    [Fact]
    public void Pagination_CanSetPageSize()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            PageSize = 25
        };

        // Assert
        Assert.Equal(25, queryParams.PageSize);
    }

    [Fact]
    public void Pagination_CanSetBothPageNumberAndSize()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            PageNumber = 3,
            PageSize = 50
        };

        // Assert
        Assert.Equal(3, queryParams.PageNumber);
        Assert.Equal(50, queryParams.PageSize);
    }

    #endregion

    #region Combined Filter Tests

    [Fact]
    public void Filters_StageName_And_DisplayOrder_Combined()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = "Geo",
            DisplayOrder = 1
        };

        // Assert
        Assert.Equal("Geo", queryParams.StageName);
        Assert.Equal(1, queryParams.DisplayOrder);
        Assert.Null(queryParams.IsActive);
    }

    [Fact]
    public void Filters_StageName_And_IsActive_Combined()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = "Survey",
            IsActive = true
        };

        // Assert
        Assert.Equal("Survey", queryParams.StageName);
        Assert.True(queryParams.IsActive);
        Assert.Null(queryParams.DisplayOrder);
    }

    [Fact]
    public void Filters_DisplayOrder_And_IsActive_Combined()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            DisplayOrder = 5,
            IsActive = true
        };

        // Assert
        Assert.Equal(5, queryParams.DisplayOrder);
        Assert.True(queryParams.IsActive);
        Assert.Null(queryParams.StageName);
    }

    [Fact]
    public void Filters_AllThree_Combined()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = "Assessment",
            DisplayOrder = 3,
            IsActive = true
        };

        // Assert
        Assert.Equal("Assessment", queryParams.StageName);
        Assert.Equal(3, queryParams.DisplayOrder);
        Assert.True(queryParams.IsActive);
    }

    [Fact]
    public void Filters_AllThree_WithPagination()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = "Bill",
            DisplayOrder = 9,
            IsActive = false,
            PageNumber = 2,
            PageSize = 20
        };

        // Assert
        Assert.Equal("Bill", queryParams.StageName);
        Assert.Equal(9, queryParams.DisplayOrder);
        Assert.False(queryParams.IsActive);
        Assert.Equal(2, queryParams.PageNumber);
        Assert.Equal(20, queryParams.PageSize);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void Inherits_From_BaseQueryParameters()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters();

        // Assert
        Assert.IsAssignableFrom<PropertyWorkflowStageMasterQueryParameters>(queryParams);
    }

    [Fact]
    public void Sorting_CanBeConfigured()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            SortBy = "StageName"
        };

        // Assert
        Assert.NotNull(queryParams);
        // SortBy from BaseQueryParameters
        Assert.Equal("StageName", queryParams.SortBy);
    }

    #endregion

    #region Equality and Comparison Tests

    [Fact]
    public void Different_QueryParameters_AreNotEqual()
    {
        // Arrange
        var queryParams1 = new PropertyWorkflowStageMasterQueryParameters { StageName = "Stage1" };
        var queryParams2 = new PropertyWorkflowStageMasterQueryParameters { StageName = "Stage2" };

        // Assert
        Assert.NotEqual(queryParams1.StageName, queryParams2.StageName);
    }

    [Fact]
    public void Same_QueryParameters_HaveSameValues()
    {
        // Arrange
        var stageName = "GeoSequencing";
        var queryParams1 = new PropertyWorkflowStageMasterQueryParameters { StageName = stageName };
        var queryParams2 = new PropertyWorkflowStageMasterQueryParameters { StageName = stageName };

        // Assert
        Assert.Equal(queryParams1.StageName, queryParams2.StageName);
    }

    #endregion

    #region DTO Validation Tests

    [Fact]
    public void QueryParameters_IsClass()
    {
        // Arrange
        var queryParams = new PropertyWorkflowStageMasterQueryParameters();

        // Act & Assert
        Assert.NotNull(queryParams);
        Assert.IsType<PropertyWorkflowStageMasterQueryParameters>(queryParams);
    }

    [Fact]
    public void QueryParameters_HasExpectedProperties()
    {
        // Arrange
        var queryParams = new PropertyWorkflowStageMasterQueryParameters();
        var properties = queryParams.GetType().GetProperties();

        // Assert
        Assert.Contains(properties, p => p.Name == "StageName");
        Assert.Contains(properties, p => p.Name == "DisplayOrder");
        Assert.Contains(properties, p => p.Name == "IsActive");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void QueryParameters_WithWhitespaceOnlyName()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = "   "
        };

        // Assert
        Assert.Equal("   ", queryParams.StageName);
    }

    [Fact]
    public void QueryParameters_WithSpecialCharactersInName()
    {
        // Act
        var queryParams = new PropertyWorkflowStageMasterQueryParameters
        {
            StageName = "Stage-@#$"
        };

        // Assert
        Assert.Equal("Stage-@#$", queryParams.StageName);
    }

    [Fact]
    public void QueryParameters_MultipleInstances_Independent()
    {
        // Arrange
        var queryParams1 = new PropertyWorkflowStageMasterQueryParameters { StageName = "Stage1" };
        var queryParams2 = new PropertyWorkflowStageMasterQueryParameters { StageName = "Stage2" };

        // Act
        queryParams1.StageName = "Updated";

        // Assert
        Assert.Equal("Updated", queryParams1.StageName);
        Assert.Equal("Stage2", queryParams2.StageName);
    }

    #endregion
}
