using NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;

namespace NtisPlatform.Tests.Application.DTOs;

public class PropertyDescriptionAndTypeOfUseValidationQueryParametersTests
{
    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_AllProperties_GetSet_WorksCorrectly()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "test",
            SortBy = "PropertyTypeId",
            SortOrder = "desc"
        };

        Assert.Equal(5, queryParams.PropertyTypeId);
        Assert.Equal(10, queryParams.TypeOfUseId);
        Assert.Equal(2, queryParams.PageNumber);
        Assert.Equal(20, queryParams.PageSize);
        Assert.Equal("test", queryParams.SearchTerm);
        Assert.Equal("PropertyTypeId", queryParams.SortBy);
        Assert.Equal("desc", queryParams.SortOrder);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_InheritsFromBaseQueryParameters()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters();
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>(queryParams);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_DefaultValues_SetCorrectly()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters();

        Assert.Null(queryParams.PropertyTypeId);
        Assert.Null(queryParams.TypeOfUseId);
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(10, queryParams.PageSize);
        Assert.Null(queryParams.SearchTerm);
        Assert.Null(queryParams.SortBy);
        Assert.Equal("asc", queryParams.SortOrder);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_PropertyTypeId_CanBeNull()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PropertyTypeId = null
        };

        Assert.Null(queryParams.PropertyTypeId);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_TypeOfUseId_CanBeNull()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            TypeOfUseId = null
        };

        Assert.Null(queryParams.TypeOfUseId);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_FilterByPropertyTypeId_WorksCorrectly()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PropertyTypeId = 5
        };

        Assert.Equal(5, queryParams.PropertyTypeId);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_FilterByTypeOfUseId_WorksCorrectly()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            TypeOfUseId = 10
        };

        Assert.Equal(10, queryParams.TypeOfUseId);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_CombinedFilters_WorksCorrectly()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            PageNumber = 1,
            PageSize = 10
        };

        Assert.Equal(5, queryParams.PropertyTypeId);
        Assert.Equal(10, queryParams.TypeOfUseId);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_SortingParameters_WorksCorrectly()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            SortBy = "PropertyTypeId",
            SortOrder = "asc"
        };

        Assert.Equal("PropertyTypeId", queryParams.SortBy);
        Assert.Equal("asc", queryParams.SortOrder);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_PaginationParameters_WorksCorrectly()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PageNumber = 3,
            PageSize = 25
        };

        Assert.Equal(3, queryParams.PageNumber);
        Assert.Equal(25, queryParams.PageSize);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_PageSizeLimit_EnforcedCorrectly()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PageSize = 200
        };

        Assert.Equal(100, queryParams.PageSize);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_PageNumberMinimum_EnforcedCorrectly()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PageNumber = 0
        };

        Assert.Equal(1, queryParams.PageNumber);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationQueryParameters_SearchTerm_WorksCorrectly()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            SearchTerm = "validation"
        };

        Assert.Equal("validation", queryParams.SearchTerm);
    }
}
