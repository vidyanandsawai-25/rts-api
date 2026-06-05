using NtisPlatform.Application.DTOs.Master.PropertyPhotoType;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// Comprehensive tests for PropertyPhotoTypeQueryParameters to achieve 100% code coverage
/// </summary>
public class PropertyPhotoTypeQueryParametersTests
{
    #region Constructor and Property Tests

    [Fact]
    public void QueryParameters_DefaultConstructor_CreatesInstance()
    {
        // Act
        var queryParams = new PropertyPhotoTypeQueryParameters();

        // Assert
        Assert.NotNull(queryParams);
    }

    [Fact]
    public void QueryParameters_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = "Front facade",
            DisplayOrder = 1,
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "test",
            SortBy = "PhotoTypeCode",
            SortOrder = "asc",
            FilterLogic = FilterLogic.And
        };

        // Assert
        Assert.Equal("FRONT", queryParams.PhotoTypeCode);
        Assert.Equal("Front View", queryParams.PhotoTypeName);
        Assert.Equal("Front facade", queryParams.Description);
        Assert.Equal(1, queryParams.DisplayOrder);
        Assert.Equal(2, queryParams.PageNumber);
        Assert.Equal(20, queryParams.PageSize);
        Assert.Equal("test", queryParams.SearchTerm);
        Assert.Equal("PhotoTypeCode", queryParams.SortBy);
        Assert.Equal("asc", queryParams.SortOrder);
        Assert.Equal(FilterLogic.And, queryParams.FilterLogic);
    }

    [Fact]
    public void QueryParameters_DefaultValues()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters();

        // Assert
        Assert.Null(queryParams.PhotoTypeCode);
        Assert.Null(queryParams.PhotoTypeName);
        Assert.Null(queryParams.Description);
        Assert.Null(queryParams.DisplayOrder);
    }

    #endregion

    #region PhotoTypeCode Filter Tests

    [Fact]
    public void QueryParameters_PhotoTypeCode_CanBeNull()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeCode = null
        };

        // Assert
        Assert.Null(queryParams.PhotoTypeCode);
    }

    [Fact]
    public void QueryParameters_PhotoTypeCode_CanBeEmptyString()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeCode = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, queryParams.PhotoTypeCode);
    }

    [Fact]
    public void QueryParameters_PhotoTypeCode_CanBeSet()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeCode = "BACK"
        };

        // Assert
        Assert.Equal("BACK", queryParams.PhotoTypeCode);
    }

    [Fact]
    public void QueryParameters_PhotoTypeCode_AcceptsLongString()
    {
        // Arrange
        var longCode = new string('A', 50);

        // Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeCode = longCode
        };

        // Assert
        Assert.Equal(longCode, queryParams.PhotoTypeCode);
        Assert.Equal(50, queryParams.PhotoTypeCode.Length);
    }

    #endregion

    #region PhotoTypeName Filter Tests

    [Fact]
    public void QueryParameters_PhotoTypeName_CanBeNull()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeName = null
        };

        // Assert
        Assert.Null(queryParams.PhotoTypeName);
    }

    [Fact]
    public void QueryParameters_PhotoTypeName_CanBeSet()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeName = "Back View"
        };

        // Assert
        Assert.Equal("Back View", queryParams.PhotoTypeName);
    }

    [Fact]
    public void QueryParameters_PhotoTypeName_AcceptsLongString()
    {
        // Arrange
        var longName = new string('B', 200);

        // Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeName = longName
        };

        // Assert
        Assert.Equal(longName, queryParams.PhotoTypeName);
        Assert.Equal(200, queryParams.PhotoTypeName.Length);
    }

    #endregion

    #region Description Filter Tests

    [Fact]
    public void QueryParameters_Description_CanBeNull()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            Description = null
        };

        // Assert
        Assert.Null(queryParams.Description);
    }

    [Fact]
    public void QueryParameters_Description_CanBeSet()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            Description = "Test description"
        };

        // Assert
        Assert.Equal("Test description", queryParams.Description);
    }

    [Fact]
    public void QueryParameters_Description_AcceptsLongString()
    {
        // Arrange
        var longDescription = new string('C', 500);

        // Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            Description = longDescription
        };

        // Assert
        Assert.Equal(longDescription, queryParams.Description);
        Assert.Equal(500, queryParams.Description.Length);
    }

    #endregion

    #region DisplayOrder Filter Tests

    [Fact]
    public void QueryParameters_DisplayOrder_CanBeNull()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            DisplayOrder = null
        };

        // Assert
        Assert.Null(queryParams.DisplayOrder);
    }

    [Fact]
    public void QueryParameters_DisplayOrder_CanBePositive()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            DisplayOrder = 10
        };

        // Assert
        Assert.Equal(10, queryParams.DisplayOrder);
    }

    [Fact]
    public void QueryParameters_DisplayOrder_CanBeZero()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            DisplayOrder = 0
        };

        // Assert
        Assert.Equal(0, queryParams.DisplayOrder);
    }

    [Fact]
    public void QueryParameters_DisplayOrder_CanBeNegative()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            DisplayOrder = -1
        };

        // Assert
        Assert.Equal(-1, queryParams.DisplayOrder);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public void QueryParameters_PageNumber_CanBeSet()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PageNumber = 5
        };

        // Assert
        Assert.Equal(5, queryParams.PageNumber);
    }

    [Fact]
    public void QueryParameters_PageSize_CanBeSet()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PageSize = 50
        };

        // Assert
        Assert.Equal(50, queryParams.PageSize);
    }

    #endregion

    #region SearchTerm Tests

    [Fact]
    public void QueryParameters_SearchTerm_CanBeSet()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            SearchTerm = "front"
        };

        // Assert
        Assert.Equal("front", queryParams.SearchTerm);
    }

    [Fact]
    public void QueryParameters_SearchTerm_CanBeNull()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            SearchTerm = null
        };

        // Assert
        Assert.Null(queryParams.SearchTerm);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public void QueryParameters_SortBy_CanBePhotoTypeCode()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            SortBy = "PhotoTypeCode"
        };

        // Assert
        Assert.Equal("PhotoTypeCode", queryParams.SortBy);
    }

    [Fact]
    public void QueryParameters_SortBy_CanBePhotoTypeName()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            SortBy = "PhotoTypeName"
        };

        // Assert
        Assert.Equal("PhotoTypeName", queryParams.SortBy);
    }

    [Fact]
    public void QueryParameters_SortBy_CanBeDisplayOrder()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            SortBy = "DisplayOrder"
        };

        // Assert
        Assert.Equal("DisplayOrder", queryParams.SortBy);
    }

    [Fact]
    public void QueryParameters_SortOrder_CanBeAscending()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            SortOrder = "asc"
        };

        // Assert
        Assert.Equal("asc", queryParams.SortOrder);
    }

    [Fact]
    public void QueryParameters_SortOrder_CanBeDescending()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            SortOrder = "desc"
        };

        // Assert
        Assert.Equal("desc", queryParams.SortOrder);
    }

    #endregion

    #region FilterLogic Tests

    [Fact]
    public void QueryParameters_FilterLogic_CanBeAnd()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            FilterLogic = FilterLogic.And
        };

        // Assert
        Assert.Equal(FilterLogic.And, queryParams.FilterLogic);
    }

    [Fact]
    public void QueryParameters_FilterLogic_CanBeOr()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            FilterLogic = FilterLogic.Or
        };

        // Assert
        Assert.Equal(FilterLogic.Or, queryParams.FilterLogic);
    }

    #endregion

    #region Combined Scenarios Tests

    [Fact]
    public void QueryParameters_AllFiltersSet_WorksTogether()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front",
            Description = "facade",
            DisplayOrder = 1,
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "test",
            SortBy = "DisplayOrder",
            SortOrder = "asc",
            FilterLogic = FilterLogic.And
        };

        // Assert
        Assert.Equal("FRONT", queryParams.PhotoTypeCode);
        Assert.Equal("Front", queryParams.PhotoTypeName);
        Assert.Equal("facade", queryParams.Description);
        Assert.Equal(1, queryParams.DisplayOrder);
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(10, queryParams.PageSize);
        Assert.Equal("test", queryParams.SearchTerm);
        Assert.Equal("DisplayOrder", queryParams.SortBy);
        Assert.Equal("asc", queryParams.SortOrder);
        Assert.Equal(FilterLogic.And, queryParams.FilterLogic);
    }

    [Fact]
    public void QueryParameters_OnlyRequiredFilters_OthersNull()
    {
        // Arrange & Act
        var queryParams = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeCode = "TEST",
            PageNumber = 1,
            PageSize = 10
        };

        // Assert
        Assert.Equal("TEST", queryParams.PhotoTypeCode);
        Assert.Null(queryParams.PhotoTypeName);
        Assert.Null(queryParams.Description);
        Assert.Null(queryParams.DisplayOrder);
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(10, queryParams.PageSize);
    }

    [Fact]
    public void QueryParameters_MultipleInstancesIndependent()
    {
        // Arrange & Act
        var queryParams1 = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeCode = "FRONT",
            DisplayOrder = 1
        };

        var queryParams2 = new PropertyPhotoTypeQueryParameters
        {
            PhotoTypeCode = "BACK",
            DisplayOrder = 2
        };

        // Assert
        Assert.NotEqual(queryParams1.PhotoTypeCode, queryParams2.PhotoTypeCode);
        Assert.NotEqual(queryParams1.DisplayOrder, queryParams2.DisplayOrder);
    }

    #endregion
}
