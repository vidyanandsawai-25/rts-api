using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerGetPropertiesTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly PropertyController _controller;

    public PropertyControllerGetPropertiesTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();
        _controller = PropertyControllerTestHelper.CreateController(_mockPropertyService, _mockLogger);
    }

    [Fact]
    public async Task GetProperties_WithValidParameters_ReturnsOk()
    {
        // Arrange
        var queryParams = new GetPropertiesQueryParameters
        {
            WardNo = "1",
            FromPropertyNo = "1",
            ToPropertyNo = "10",
            UserId = 5,
            Flag = "ALL"
        };

        var items = new List<GetPropertiesItemDto>
        {
            new() { PropertyId = 1, WardId = 1, WardNo = "1", PropertyNo = "1", OwnerName = "Owner 1" }
        };

        _mockPropertyService
            .Setup(s => s.GetPropertiesAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        // Act
        var result = await _controller.GetProperties(queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<GetPropertiesItemDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Records fetched successfully", response.Message);
        Assert.NotNull(response.Items);
        Assert.Single(response.Items!);
        Assert.Equal(1, response.Items![0].PropertyId);
    }

    [Fact]
    public async Task GetProperties_WithNullParameters_ReturnsBadRequest()
    {
        var result = await _controller.GetProperties(null!, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<GetPropertiesItemDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Query parameters are required", response.Message);
    }

    [Theory]
    [InlineData("", "1", "10", 1, "ALL", "wardNo is required")]
    [InlineData("1", "", "10", 1, "ALL", "fromPropertyNo is required")]
    [InlineData("1", "1", "", 1, "ALL", "toPropertyNo is required")]
    [InlineData("1", "1", "10", 0, "ALL", "userId is required and must be greater than 0")]
    [InlineData("1", "1", "10", 1, "", "flag is required")]
    public async Task GetProperties_WithInvalidParameters_ReturnsBadRequest(
        string wardNo,
        string fromPropertyNo,
        string toPropertyNo,
        int userId,
        string flag,
        string expectedMessage)
    {
        var queryParams = new GetPropertiesQueryParameters
        {
            WardNo = wardNo,
            FromPropertyNo = fromPropertyNo,
            ToPropertyNo = toPropertyNo,
            UserId = userId,
            Flag = flag
        };

        var result = await _controller.GetProperties(queryParams, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<GetPropertiesItemDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal(expectedMessage, response.Message);
    }

    [Fact]
    public async Task GetProperties_WhenArgumentExceptionThrown_ReturnsBadRequest()
    {
        var queryParams = new GetPropertiesQueryParameters
        {
            WardNo = "1",
            FromPropertyNo = "1",
            ToPropertyNo = "10",
            UserId = 1,
            Flag = "INVALID"
        };

        _mockPropertyService
            .Setup(s => s.GetPropertiesAsync(queryParams, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Flag must be one of: New, Old, All."));

        var result = await _controller.GetProperties(queryParams, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<GetPropertiesItemDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Flag must be one of: New, Old, All.", response.Message);
    }
}
