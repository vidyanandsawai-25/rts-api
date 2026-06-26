using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Comprehensive tests for PropertyController.Search.cs
/// Covers GET /api/Property/search and GET /api/Property/search/dashboard-stats endpoints
/// Tests all edge cases including PageSize=-1, out-of-range pages, and null handling
/// </summary>
public class PropertyControllerSearchEndpointTests
{
    private readonly Mock<IPropertySearchService> _mockSearchService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly PropertyController _controller;

    public PropertyControllerSearchEndpointTests()
    {
        _mockSearchService = new Mock<IPropertySearchService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();
        _controller = PropertyControllerTestHelper.CreateController(new Mock<IPropertyService>(), _mockLogger, searchService: _mockSearchService);
    }

    #region GET /api/Property/search Tests

    [Fact]
    public async Task SearchProperties_WithValidParameters_ReturnsOkWithResults()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            ZoneId = 1,
            WardId = 2,
            PageNumber = 1,
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            new List<PropertySearchResponseDto>
            {
                new PropertySearchResponseDto { PropertyId = 1, PropertyNo = "001" },
                new PropertySearchResponseDto { PropertyId = 2, PropertyNo = "002" }
            },
            totalCount: 2,
            pageNumber: 1,
            pageSize: 10
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertySearchResponseDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("2 record(s) found", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(2, response.Items!.TotalCount);
    }

    [Fact]
    public async Task SearchProperties_WithNoResults_ReturnsOkWithEmptyList()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            ZoneId = 999,
            PageNumber = 1,
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            new List<PropertySearchResponseDto>(),
            totalCount: 0,
            pageNumber: 1,
            pageSize: 10
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertySearchResponseDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("No records found matching the search criteria", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(0, response.Items!.TotalCount);
    }

    [Fact]
    public async Task SearchProperties_WithNullQueryParameters_ReturnsBadRequest()
    {
        // Arrange
        PropertySearchQueryParameters? nullParameters = null;

        // Act
        var result = await _controller.SearchProperties(nullParameters!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertySearchResponseDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Query parameters cannot be null", response.Message);

        _mockSearchService.Verify(
            s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchProperties_WithPropertyNoToOnly_AppliesFilter()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            PropertyNoTo = "100",
            PageNumber = 1,
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            new List<PropertySearchResponseDto>
            {
                new PropertySearchResponseDto { PropertyId = 1, PropertyNo = "050" }
            },
            totalCount: 1,
            pageNumber: 1,
            pageSize: 10
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(
                It.Is<PropertySearchQueryParameters>(q => q.PropertyNoTo == "100" && q.PropertyNoFrom == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertySearchResponseDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Items!.TotalCount);

        _mockSearchService.Verify(
            s => s.SearchPropertiesAsync(
                It.Is<PropertySearchQueryParameters>(q => q.PropertyNoTo == "100"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchProperties_WithPropertyNoFromOnly_AppliesFilter()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            PropertyNoFrom = "050",
            PageNumber = 1,
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            new List<PropertySearchResponseDto>
            {
                new PropertySearchResponseDto { PropertyId = 1, PropertyNo = "075" }
            },
            totalCount: 1,
            pageNumber: 1,
            pageSize: 10
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(
                It.Is<PropertySearchQueryParameters>(q => q.PropertyNoFrom == "050" && q.PropertyNoTo == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertySearchResponseDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Items!.TotalCount);
    }

    [Fact]
    public async Task SearchProperties_WithPageSizeMinusOne_ReturnsAllResults()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            PageNumber = 1,
            PageSize = -1
        };

        var allItems = Enumerable.Range(1, 100)
            .Select(i => new PropertySearchResponseDto { PropertyId = i })
            .ToList();

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            allItems,
            totalCount: 100,
            pageNumber: 1,
            pageSize: -1
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertySearchResponseDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(100, response.Items!.TotalCount);
        Assert.Equal(100, response.Items!.Items.Count());
    }

    [Fact]
    public async Task SearchProperties_WithOutOfRangePage_ReturnsEmptyResults()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            PageNumber = 100,
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            new List<PropertySearchResponseDto>(),
            totalCount: 50,
            pageNumber: 100,
            pageSize: 10
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertySearchResponseDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("50 record(s) found", response.Message); // TotalCount is 50, so message should reflect that
        Assert.Empty(response.Items!.Items); // But current page has no items
        Assert.Equal(50, response.Items!.TotalCount); // Total count is still 50
    }

    [Fact]
    public async Task SearchProperties_WithAllNewFilters_ReturnsFilteredResults()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            PropertyAssessmentStatusId = 1,
            PlotNo = "PLOT001",
            MobileNo = "9876543210",
            PageNumber = 1,
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            new List<PropertySearchResponseDto>
            {
                new PropertySearchResponseDto
                {
                    PropertyId = 1,
                    PlotNo = "PLOT001",
                    Mobile = "9876543210"
                }
            },
            totalCount: 1,
            pageNumber: 1,
            pageSize: 10
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertySearchResponseDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Items!.TotalCount);

        _mockSearchService.Verify(
            s => s.SearchPropertiesAsync(
                It.Is<PropertySearchQueryParameters>(q =>
                    q.PropertyAssessmentStatusId == 1 &&
                    q.PlotNo == "PLOT001" &&
                    q.MobileNo == "9876543210"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchProperties_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var queryParameters = new PropertySearchQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            new List<PropertySearchResponseDto>(),
            totalCount: 0,
            pageNumber: 1,
            pageSize: 10
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), token))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.SearchProperties(queryParameters, token);

        // Assert
        _mockSearchService.Verify(
            s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), token),
            Times.Once);
    }

    #endregion

    #region GET /api/Property/search/dashboard-stats Tests

    [Fact]
    public async Task GetDashboardStats_ReturnsOkWithStatistics()
    {
        // Arrange
        var expectedStats = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = 100,
            GeoSequencingPropertyCount = 100,
            SurveyPropertyCount = 0,
            DataProcessingPropertyCount = 0,
            QualityAnalysisPropertyCount = 0,
            AssessmentCompletedPropertyCount = 0
        };

        _mockSearchService
            .Setup(s => s.GetPropertyDashboardStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetDashboardStats(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var response = Assert.IsType<ApiResponse<PropertyDashboardStatsDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Dashboard statistics retrieved successfully", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(100, response.Items.RegisteredPropertyCount);
        Assert.Equal(100, response.Items.GeoSequencingPropertyCount);
        Assert.Equal(0, response.Items.SurveyPropertyCount);
        Assert.Equal(0, response.Items.DataProcessingPropertyCount);
        Assert.Equal(0, response.Items.QualityAnalysisPropertyCount);
        Assert.Equal(0, response.Items.AssessmentCompletedPropertyCount);
    }

    [Fact]
    public async Task GetDashboardStats_WithZeroCounts_ReturnsOkWithZeros()
    {
        // Arrange
        var expectedStats = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = 0,
            GeoSequencingPropertyCount = 0,
            SurveyPropertyCount = 0,
            DataProcessingPropertyCount = 0,
            QualityAnalysisPropertyCount = 0,
            AssessmentCompletedPropertyCount = 0
        };

        _mockSearchService
            .Setup(s => s.GetPropertyDashboardStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetDashboardStats(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDashboardStatsDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(0, response.Items.RegisteredPropertyCount);
    }

    [Fact]
    public async Task GetDashboardStats_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var expectedStats = new PropertyDashboardStatsDto();

        _mockSearchService
            .Setup(s => s.GetPropertyDashboardStatsAsync(token))
            .ReturnsAsync(expectedStats);

        // Act
        await _controller.GetDashboardStats(token);

        // Assert
        _mockSearchService.Verify(
            s => s.GetPropertyDashboardStatsAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task GetDashboardStats_CallsService_OnlyOnce()
    {
        // Arrange
        var expectedStats = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = 50
        };

        _mockSearchService
            .Setup(s => s.GetPropertyDashboardStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        await _controller.GetDashboardStats(CancellationToken.None);

        // Assert
        _mockSearchService.Verify(
            s => s.GetPropertyDashboardStatsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task SearchProperties_WithMaxPageSize_HandlesGracefully()
    {
        // Arrange
        // Note: BaseQueryParameters clamps PageSize to MaxPageSize (100) unless PageSize == -1
        var queryParameters = new PropertySearchQueryParameters
        {
            PageNumber = 1,
            PageSize = int.MaxValue // Will be clamped to 100
        };

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            new List<PropertySearchResponseDto>(),
            totalCount: 100,
            pageNumber: 1,
            pageSize: 100 // Actual clamped value
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Verify that PageSize was clamped
        Assert.Equal(100, queryParameters.PageSize);
    }

    [Fact]
    public async Task SearchProperties_WithZeroPageNumber_HandlesGracefully()
    {
        // Arrange
        // Note: BaseQueryParameters clamps PageNumber < 1 to 1
        var queryParameters = new PropertySearchQueryParameters
        {
            PageNumber = 0, // Will be clamped to 1
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            new List<PropertySearchResponseDto>(),
            totalCount: 0,
            pageNumber: 1, // Actual clamped value
            pageSize: 10
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Verify that PageNumber was clamped
        Assert.Equal(1, queryParameters.PageNumber);
    }

    [Fact]
    public async Task SearchProperties_WithAllFiltersNull_ReturnsAllProperties()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertySearchResponseDto>(
            new List<PropertySearchResponseDto>
            {
                new PropertySearchResponseDto { PropertyId = 1 },
                new PropertySearchResponseDto { PropertyId = 2 },
                new PropertySearchResponseDto { PropertyId = 3 }
            },
            totalCount: 3,
            pageNumber: 1,
            pageSize: 10
        );

        _mockSearchService
            .Setup(s => s.SearchPropertiesAsync(It.IsAny<PropertySearchQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertySearchResponseDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(3, response.Items!.TotalCount);
    }

    #endregion

    #region GET /api/Property/search/scope-options Tests

    [Fact]
    public void GetScopeOptions_WithNullCategory_ReturnsAllCategories()
    {
        // Arrange
        var mockCategories = new List<ScopeCategoryDto>
        {
            new ScopeCategoryDto { Id = 1, Name = "AllProperties", DisplayName = "All Properties", Description = "Entire corporation", Options = new List<string>() },
            new ScopeCategoryDto { Id = 2, Name = "WardSector", DisplayName = "Ward / Sector", Description = "Multi ward selection", Options = new List<string> { "Zone", "Ward", "Property Type" } },
            new ScopeCategoryDto { Id = 3, Name = "BuildingWise", DisplayName = "Building Wise", Description = "Building level", Options = new List<string> { "Zone", "Ward", "Property No" } },
            new ScopeCategoryDto { Id = 4, Name = "PropertyRange", DisplayName = "Property Range", Description = "From-to property range", Options = new List<string> { "Ward", "From Property", "To Property" } }
        };
        _mockSearchService.Setup(s => s.GetScopeOptions(null)).Returns(mockCategories);

        // Act
        var result = _controller.GetScopeOptions(category: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<ScopeCategoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("All scope category options retrieved successfully", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(4, response.Items.Count);

        // Verify some specific items to ensure proper mapping
        var allProps = response.Items.Find(c => c.Id == (int)ScopeCategory.AllProperties);
        Assert.NotNull(allProps);
        Assert.Equal("AllProperties", allProps.Name);
        Assert.Equal("All Properties", allProps.DisplayName);
        Assert.Equal("Entire corporation", allProps.Description);
        Assert.Empty(allProps.Options);

    }

    [Fact]
    public void GetScopeOptions_WithValidCategory_ReturnsFilteredCategory()
    {
        // Arrange
        var mockCategories = new List<ScopeCategoryDto>
        {
            new ScopeCategoryDto { Id = 2, Name = "WardSector", DisplayName = "Ward / Sector", Description = "Multi ward selection", Options = new List<string> { "Zone", "Ward", "Property Type" } }
        };
        _mockSearchService.Setup(s => s.GetScopeOptions(ScopeCategory.WardSector)).Returns(mockCategories);

        // Act
        var result = _controller.GetScopeOptions(category: ScopeCategory.WardSector);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<ScopeCategoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Scope category 'WardSector' options retrieved successfully", response.Message);
        Assert.NotNull(response.Items);
        Assert.Single(response.Items);

        var wardSector = response.Items[0];
        Assert.Equal((int)ScopeCategory.WardSector, wardSector.Id);
        Assert.Equal("WardSector", wardSector.Name);
        Assert.Equal("Ward / Sector", wardSector.DisplayName);
        Assert.Equal("Multi ward selection", wardSector.Description);
        Assert.Equal(new List<string> { "Zone", "Ward", "Property Type" }, wardSector.Options);
    }

    #endregion
}
