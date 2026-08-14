using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyBulkMerge;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyBulkMergeControllerTests
{
    private readonly Mock<IPropertyBulkMergeService> _mockPropertyBulkMergeService;
    private readonly Mock<ILogger<PropertyBulkMergeController>> _mockLogger;
    private readonly PropertyBulkMergeController _controller;

    public PropertyBulkMergeControllerTests()
    {
        _mockPropertyBulkMergeService = new Mock<IPropertyBulkMergeService>();
        _mockLogger = new Mock<ILogger<PropertyBulkMergeController>>();
        _controller = new PropertyBulkMergeController(_mockPropertyBulkMergeService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PropertyBulkMergeCreateAsync_Success_ReturnsOk()
    {
        // Arrange
        var createDto = new CreatePropertyBulkMergeDto();
        var returnedDto = new PropertyBulkMergeDto();

        _mockPropertyBulkMergeService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.PropertyBulkMergeCreateAsync(createDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyBulkMergeDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record inserted successfully");
        apiResponse.Items.Should().Be(returnedDto);
    }

    [Fact]
    public async Task PropertyBulkMergeUpdateAsync_Success_ReturnsOk()
    {
        // Arrange
        int propertyId = 10;
        var updateDto = new UpdatePropertyBulkMergeDto
        {
            PropertyIdList = new List<PropertyBulkMergeDetailsDto>
            {
                new PropertyBulkMergeDetailsDto { PropertyId = propertyId }
            }
        };
        var returnedDto = new PropertyBulkMergeDto();

        _mockPropertyBulkMergeService
            .Setup(s => s.UpdateAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.PropertyBulkMergeUpdateAsync(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyBulkMergeDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record updated successfully");
        apiResponse.Items.Should().Be(returnedDto);
    }

    [Fact]
    public async Task PropertyBulkMergeUpdateAsync_NotFound_ReturnsOkWithSuccessFalse()
    {
        // Arrange
        int propertyId = 10;
        var updateDto = new UpdatePropertyBulkMergeDto
        {
            PropertyIdList = new List<PropertyBulkMergeDetailsDto>
            {
                new PropertyBulkMergeDetailsDto { PropertyId = propertyId }
            }
        };

        _mockPropertyBulkMergeService
            .Setup(s => s.UpdateAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyBulkMergeDto?)null);

        // Act
        var result = await _controller.PropertyBulkMergeUpdateAsync(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyBulkMergeDto>>().Subject;

        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("Record not found for Update ");
        apiResponse.Items.Should().BeNull();
    }
}
