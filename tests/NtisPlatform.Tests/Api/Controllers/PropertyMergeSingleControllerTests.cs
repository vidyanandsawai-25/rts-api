using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyMergeSingle;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyMergeSingleControllerTests
{
    private readonly Mock<IPropertyMergeSingleService> _mockPropertyMergeSingleService;
    private readonly Mock<ILogger<PropertyMergeSingleController>> _mockLogger;
    private readonly PropertyMergeSingleController _controller;

    public PropertyMergeSingleControllerTests()
    {
        _mockPropertyMergeSingleService = new Mock<IPropertyMergeSingleService>();
        _mockLogger = new Mock<ILogger<PropertyMergeSingleController>>();
        _controller = new PropertyMergeSingleController(_mockPropertyMergeSingleService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PropertyMergeSingleCreateAsync_Success_ReturnsOk()
    {
        // Arrange
        var createDto = new CreatePropertyMergeSingleDto();
        var returnedDto = new PropertyMergeSingleDto();

        _mockPropertyMergeSingleService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.PropertyMergeSingleCreateAsync(createDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeSingleDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record inserted successfully");
        apiResponse.Items.Should().Be(returnedDto);
    }

    [Fact]
    public async Task PropertyMergeSingleUpdateAsync_Success_ReturnsOk()
    {
        // Arrange
        int propertyId = 10;
        var updateDto = new UpdatePropertyMergeSingleDto { PropertyId = propertyId };
        var returnedDto = new PropertyMergeSingleDto();

        _mockPropertyMergeSingleService
            .Setup(s => s.UpdateAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.PropertyMergeSingleUpdateAsync(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeSingleDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record updated successfully");
        apiResponse.Items.Should().Be(returnedDto);
    }

    [Fact]
    public async Task PropertyMergeSingleUpdateAsync_NotFound_ReturnsOkWithSuccessFalse()
    {
        // Arrange
        int propertyId = 10;
        var updateDto = new UpdatePropertyMergeSingleDto { PropertyId = propertyId };

        _mockPropertyMergeSingleService
            .Setup(s => s.UpdateAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMergeSingleDto?)null);

        // Act
        var result = await _controller.PropertyMergeSingleUpdateAsync(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeSingleDto>>().Subject;

        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("Record not found for Update ");
        apiResponse.Items.Should().BeNull();
    }
}
