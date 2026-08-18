using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyChangeCategory;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyChangeCategoryControllerTests
{
    private readonly Mock<IPropertyChangeCategoryService> _mockPropertyChangeCategoryService;
    private readonly Mock<ILogger<PropertyChangeCategoryController>> _mockLogger;
    private readonly PropertyChangeCategoryController _controller;

    public PropertyChangeCategoryControllerTests()
    {
        _mockPropertyChangeCategoryService = new Mock<IPropertyChangeCategoryService>();
        _mockLogger = new Mock<ILogger<PropertyChangeCategoryController>>();
        _controller = new PropertyChangeCategoryController(_mockPropertyChangeCategoryService.Object, _mockLogger.Object);
    }



    [Fact]
    public async Task PropertyChangeCategoryUpdateAsync_Success_ReturnsOk()
    {
        // Arrange
        int propertyId = 10;
        var updateDto = new UpdatePropertyChangeCategoryDto { PropertyId = propertyId };
        var returnedDto = new PropertyChangeCategoryDto();

        _mockPropertyChangeCategoryService
            .Setup(s => s.UpdateAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.PropertyChangeCategoryUpdateAsync(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyChangeCategoryDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record updated successfully");
        apiResponse.Items.Should().Be(returnedDto);
    }

    [Fact]
    public async Task PropertyChangeCategoryUpdateAsync_NotFound_ReturnsOkWithSuccessFalse()
    {
        // Arrange
        int propertyId = 10;
        var updateDto = new UpdatePropertyChangeCategoryDto { PropertyId = propertyId };

        _mockPropertyChangeCategoryService
            .Setup(s => s.UpdateAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyChangeCategoryDto?)null);

        // Act
        var result = await _controller.PropertyChangeCategoryUpdateAsync(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyChangeCategoryDto>>().Subject;

        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("Record not found for Update ");
        apiResponse.Items.Should().BeNull();
    }

}
