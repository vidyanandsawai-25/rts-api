using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertySplit;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertySplitControllerTests
{
    private readonly Mock<IPropertySplitService> _mockPropertySplitService;
    private readonly Mock<ILogger<PropertySplitController>> _mockLogger;
    private readonly PropertySplitController _controller;

    public PropertySplitControllerTests()
    {
        _mockPropertySplitService = new Mock<IPropertySplitService>();
        _mockLogger = new Mock<ILogger<PropertySplitController>>();
        _controller = new PropertySplitController(_mockPropertySplitService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PropertySplitCreateAsync_Success_ReturnsOk()
    {
        // Arrange
        var createDto = new CreatePropertySplitDto();
        var returnedDto = new PropertySplitDto();

        _mockPropertySplitService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.PropertySplitCreateAsync(createDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertySplitDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record inserted successfully");
        apiResponse.Items.Should().Be(returnedDto);
    }

    [Fact]
    public async Task PropertySplitUpdateAsync_Success_ReturnsOk()
    {
        // Arrange
        int propertyOldId = 10;
        var updateDto = new UpdatePropertySplitDto { PropertyOldId = propertyOldId };
        var returnedDto = new PropertySplitDto();

        _mockPropertySplitService
            .Setup(s => s.UpdateAsync(propertyOldId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.PropertySplitUpdateAsync(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertySplitDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record updated successfully");
        apiResponse.Items.Should().Be(returnedDto);
    }

    [Fact]
    public async Task PropertySplitUpdateAsync_NotFound_ReturnsOkWithSuccessFalse()
    {
        // Arrange
        int propertyOldId = 10;
        var updateDto = new UpdatePropertySplitDto { PropertyOldId = propertyOldId };

        _mockPropertySplitService
            .Setup(s => s.UpdateAsync(propertyOldId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySplitDto?)null);

        // Act
        var result = await _controller.PropertySplitUpdateAsync(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertySplitDto>>().Subject;

        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("Record not found for Update ");
        apiResponse.Items.Should().BeNull();
    }
}
