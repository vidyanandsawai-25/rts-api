using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyMapDetails;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyMappingControllerTests
{
    private readonly Mock<IPropertyMappingService> _mockPropertyMappingService;
    private readonly Mock<ILogger<PropertyMappingController>> _mockLogger;
    private readonly PropertyMappingController _controller;

    public PropertyMappingControllerTests()
    {
        _mockPropertyMappingService = new Mock<IPropertyMappingService>();
        _mockLogger = new Mock<ILogger<PropertyMappingController>>();
        _controller = new PropertyMappingController(_mockPropertyMappingService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task AddPropertyMapDetails_Success_ReturnsOk()
    {
        // Arrange
        var createDto = new CreatePropertyMapDetailsDto { PropertyId = 1, Flag = "MAP" };
        var resultDto = new PropertyMapDetailDto { PropertyIdNew = 1 };

        _mockPropertyMappingService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.AddPropertyMapDetails(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMapDetailDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record inserted successfully", response.Message);
        Assert.NotNull(response.Items);
    }

    [Fact]
    public async Task AddPropertyMapDetails_Exception_ReturnsInternalServerError()
    {
        // Arrange
        var createDto = new CreatePropertyMapDetailsDto { PropertyId = 1, Flag = "MAP" };

        _mockPropertyMappingService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.AddPropertyMapDetails(createDto, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdatePropertyMapDetails_Success_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdatePropertyMapDetailsDto { PropertyId = 100 };
        var resultDto = new PropertyMapDetailDto { PropertyIdNew = 100 };

        _mockPropertyMappingService
            .Setup(s => s.UpdateAsync(updateDto.PropertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.UpdatePropertyMapDetails(updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMapDetailDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record updated successfully", response.Message);
        Assert.NotNull(response.Items);
    }

    [Fact]
    public async Task UpdatePropertyMapDetails_NotFound_ReturnsOkWithSuccessFalse()
    {
        // Arrange
        var updateDto = new UpdatePropertyMapDetailsDto { PropertyId = 999 };

        // Return null to simulate record not found (like when updateCount == 0)
        _mockPropertyMappingService
            .Setup(s => s.UpdateAsync(updateDto.PropertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapDetailDto?)null);

        // Act
        var result = await _controller.UpdatePropertyMapDetails(updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMapDetailDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }
}
