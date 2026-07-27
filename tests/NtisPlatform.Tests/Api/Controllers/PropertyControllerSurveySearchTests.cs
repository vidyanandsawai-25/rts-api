using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertySurveySearch;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerSurveySearchTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly PropertyController _controller;

    public PropertyControllerSurveySearchTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();
        
        _controller = PropertyControllerTestHelper.CreateController(_mockPropertyService, _mockLogger);
    }

    [Fact]
    public async Task SearchSurveyProperties_WithValidParameters_ReturnsOkWithResults()
    {
        // Arrange
        var queryParameters = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
            Status = "NEW"
        };

        var expectedResult = new ApiResponse<PropertySurveySearchPaginatedResponseDto>
        {
            Success = true,
            Message = "Property search fetched successfully.",
            Items = new PropertySurveySearchPaginatedResponseDto
            {
                Count = 1,
                Data = new List<PropertySurveySearchResponseDto>
                {
                    new PropertySurveySearchResponseDto { PropertyId = 1, PropertyNo = "001" }
                }
            }
        };

        _mockPropertyService
            .Setup(s => s.SearchSurveyPropertiesAsync(It.IsAny<PropertySurveySearchQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchSurveyProperties(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertySurveySearchPaginatedResponseDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Property search fetched successfully.", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items!.Count);
    }

    [Fact]
    public async Task SearchSurveyProperties_WithNullParameters_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchSurveyProperties(null!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertySurveySearchPaginatedResponseDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Query parameters cannot be null.", response.Message);
    }
}
