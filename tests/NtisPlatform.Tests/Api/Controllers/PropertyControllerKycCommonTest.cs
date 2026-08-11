using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyKyc;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Tests.Api.Controllers;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

using NtisPlatform.Application.Interfaces.Property;

public class PropertyControllerKycCommonTest
{
    private readonly Mock<IPropertyService> _propertyServiceMock;
    private readonly Mock<IPropertyKycService> _propertyKycServiceMock;
    private readonly Mock<ILogger<PropertyController>> _loggerMock;
    private readonly PropertyController _controller;

    public PropertyControllerKycCommonTest()
    {
        _propertyServiceMock = new Mock<IPropertyService>();
        _propertyKycServiceMock = new Mock<IPropertyKycService>();
        _loggerMock = new Mock<ILogger<PropertyController>>();

        _controller = PropertyControllerTestHelper.CreateController(
            _propertyServiceMock,
            _loggerMock,
            kycService: _propertyKycServiceMock);
    }

    [Fact]
    public async Task GetKycDetailsCommon_WhenRecordExists_ReturnsOkResponse()
    {
        // Arrange
        var request = new PropertyKycDetailsQueryParameters
        {
            WardId = 89,
            PropertyNo = "10",
            PartitionNo = null
        };

        var serviceResult = new PropertyKycDetailsCommonDto
        {
            PropertyId = 753362,
            PropertyTypeId = 1,
            CategoryId = 2,
            OwnerName = "Test Owner",
            MobileNo = "9876543210"
        };

        _propertyKycServiceMock
            .Setup(x => x.GetKycDetailsCommon(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetKycDetailsCommon(
            request,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status200OK,
            okResult.StatusCode);

        var response =
            Assert.IsType<ApiResponse<PropertyKycDetailsCommonDto>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("Record fetched successfully", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(753362, response.Items.PropertyId);
        Assert.Equal("Test Owner", response.Items.OwnerName);
      

        _propertyKycServiceMock.Verify(
            x => x.GetKycDetailsCommon(
                request,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetKycDetailsCommon_WhenRecordDoesNotExist_ReturnsNotFoundResponse()
    {
        // Arrange
        var request = new PropertyKycDetailsQueryParameters
        {
            WardId = 89,
            PropertyNo = "99999",
            PartitionNo = null
        };

        _propertyKycServiceMock
            .Setup(x => x.GetKycDetailsCommon(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyKycDetailsCommonDto?)null);

        // Act
        var result = await _controller.GetKycDetailsCommon(
            request,
            CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            notFoundResult.StatusCode);

        var response =
            Assert.IsType<ApiResponse<PropertyKycDetailsCommonDto>>(
                notFoundResult.Value);

        Assert.False(response.Success);
        Assert.Equal(
            "Property not found for the given criteria",
            response.Message);

        Assert.Null(response.Items);

        _propertyKycServiceMock.Verify(
            x => x.GetKycDetailsCommon(
                request,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetKycDetailsCommon_PassesCancellationTokenToService()
    {
        // Arrange
        var request = new PropertyKycDetailsQueryParameters
        {
            WardId = 89,
            PropertyNo = "10"
        };

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var serviceResult = new PropertyKycDetailsCommonDto
        {
            PropertyId = 753362
        };

        _propertyKycServiceMock
            .Setup(x => x.GetKycDetailsCommon(
                request,
                cancellationToken))
            .ReturnsAsync(serviceResult);

        // Act
        await _controller.GetKycDetailsCommon(
            request,
            cancellationToken);

        // Assert
        _propertyKycServiceMock.Verify(
            x => x.GetKycDetailsCommon(
                request,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetKycDetailsCommon_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var request = new PropertyKycDetailsQueryParameters
        {
            WardId = 89,
            PropertyNo = "10"
        };

        _propertyKycServiceMock
            .Setup(x => x.GetKycDetailsCommon(
                request,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Database operation failed"));

        // Act and Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.GetKycDetailsCommon(
                    request,
                    CancellationToken.None));

        Assert.Equal(
            "Database operation failed",
            exception.Message);
    }
}