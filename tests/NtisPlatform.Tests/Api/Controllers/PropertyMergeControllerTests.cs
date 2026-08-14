using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyMergeDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyMergeControllerTests
{
    private readonly Mock<IPropertyMergeService> _mockPropertyMergeService;
    private readonly Mock<ILogger<PropertyMergeController>> _mockLogger;
    private readonly PropertyMergeController _controller;

    public PropertyMergeControllerTests()
    {
        _mockPropertyMergeService = new Mock<IPropertyMergeService>();
        _mockLogger = new Mock<ILogger<PropertyMergeController>>();
        _controller = new PropertyMergeController(_mockPropertyMergeService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PropertyMergeCreateAsync_Success_ReturnsOk()
    {
        // Arrange
        var createDto = new CreatePropertyMergeDto();
        var returnedDto = new PropertyMergeDto();

        _mockPropertyMergeService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.PropertyMergeCreateAsync(createDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record inserted successfully");
        apiResponse.Items.Should().Be(returnedDto);
    }

    [Fact]
    public async Task PropertyMergeUpdateAsync_Success_ReturnsOk()
    {
        // Arrange
        int propertyId = 10;
        var updateDto = new UpdatePropertyMergeDto { PropertyId = propertyId };
        var returnedDto = new PropertyMergeDto();

        _mockPropertyMergeService
            .Setup(s => s.UpdateAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.PropertyMergeUpdateAsync(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record updated successfully");
        apiResponse.Items.Should().Be(returnedDto);
    }

    [Fact]
    public async Task PropertyMergeUpdateAsync_NotFound_ReturnsOkWithSuccessFalse()
    {
        // Arrange
        int propertyId = 10;
        var updateDto = new UpdatePropertyMergeDto { PropertyId = propertyId };

        _mockPropertyMergeService
            .Setup(s => s.UpdateAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMergeDto?)null);

        // Act
        var result = await _controller.PropertyMergeUpdateAsync(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("Record not found for Update ");
        apiResponse.Items.Should().BeNull();
    }

    [Fact]
    public async Task GetPropertyMergeDetailsById_Success_ReturnsOk()
    {
        // Arrange
        int propertyId = 15;
        var returnedDto = new PropertyMergeDto();

        _mockPropertyMergeService
            .Setup(s => s.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.GetPropertyMergeDetailsById(propertyId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(returnedDto);
    }

    [Fact]
    public async Task GetPropertyMergeDetailsById_NotFound_ReturnsNotFound()
    {
        // Arrange
        int propertyId = 15;

        _mockPropertyMergeService
            .Setup(s => s.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMergeDto?)null);

        // Act
        var result = await _controller.GetPropertyMergeDetailsById(propertyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
