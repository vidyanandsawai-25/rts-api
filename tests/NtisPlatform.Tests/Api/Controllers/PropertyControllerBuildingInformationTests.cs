using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyBuildingInformation;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerBuildingInformationTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly PropertyController _controller;

    public PropertyControllerBuildingInformationTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();

        _controller = PropertyControllerTestHelper.CreateController(
            _mockPropertyService,
            _mockLogger);
    }

    [Fact]
    public async Task SearchBuildingInformation_WithResults_ReturnsOkWithList()
    {
        // Arrange
        var dtos = new List<SearchBuildingInformationDto>
        {
            new()
            {
                OldWardNo = "W1",
                OldSocietyName = "ABC Society",
                MapId = 10
            }
        };

        var expectedResult = new List<PropertyBuildingInformationDto>
        {
            new()
            {
                PropertyId = 101,
                Id = 501,
                OldPropertyNo = "OLD-001",
                OldWing = "A",
                OldFlatOrShopNumber = "101",
                OldOwnerName = "Test Owner",
                Identify = true
            }
        };

        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                dtos,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchBuildingInformation(
            dtos,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response = Assert.IsType<ApiResponse<List<PropertyBuildingInformationDto>>>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("1 record(s) found", response.Message);
        Assert.NotNull(response.Items);
        Assert.Single(response.Items);

        var item = response.Items.First();

        Assert.Equal(101, item.PropertyId);
        Assert.Equal(501, item.Id);
        Assert.Equal("OLD-001", item.OldPropertyNo);
        Assert.True(item.Identify);

        _mockPropertyService.Verify(
            service => service.SearchBuildingInformationAsync(
                dtos,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchBuildingInformation_WithNoResults_ReturnsOkWithNoRecordsMessage()
    {
        // Arrange
        var dtos = new List<SearchBuildingInformationDto>
        {
            new() { OldWardNo = "W1" }
        };

        var expectedResult = new List<PropertyBuildingInformationDto>();

        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                dtos,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchBuildingInformation(
            dtos,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response = Assert.IsType<ApiResponse<List<PropertyBuildingInformationDto>>>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("No records found matching the search criteria", response.Message);
        Assert.NotNull(response.Items);
        Assert.Empty(response.Items);
    }

    [Fact]
    public async Task SearchBuildingInformation_PropagatesCancellationToken()
    {
        // Arrange
        var dtos = new List<SearchBuildingInformationDto>
        {
            new() { OldWardNo = "W1" }
        };

        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        var expectedResult = new List<PropertyBuildingInformationDto>();

        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                dtos,
                token))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.SearchBuildingInformation(
            dtos,
            token);

        // Assert
        _mockPropertyService.Verify(
            service => service.SearchBuildingInformationAsync(
                dtos,
                token),
            Times.Once);
    }

    [Fact]
    public async Task SearchBuildingInformation_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var dtos = new List<SearchBuildingInformationDto>
        {
            new() { OldWardNo = "W1" }
        };

        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                dtos,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Building information search failed."));

        // Act and Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.SearchBuildingInformation(
                dtos,
                CancellationToken.None));

        Assert.Equal("Building information search failed.", exception.Message);
    }

    [Fact]
    public async Task SearchBuildingInformation_WithNullParameters_ReturnsEmptyList()
    {
        // Arrange
        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                It.IsAny<List<SearchBuildingInformationDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyBuildingInformationDto>());

        // Act
        var result = await _controller.SearchBuildingInformation(
            null!,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<PropertyBuildingInformationDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("No records found matching the search criteria", response.Message);
    }
}