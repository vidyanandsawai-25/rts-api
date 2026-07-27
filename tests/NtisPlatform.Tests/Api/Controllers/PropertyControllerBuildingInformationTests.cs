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
    public async Task SearchBuildingInformation_WithResults_ReturnsOkWithPagedResult()
    {
        // Arrange
        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1",
            OldSocietyName = "ABC Society",
            MapId = 10,
            PageNumber = 1,
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertyBuildingInformationDto>
        {
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10,
            Items =
            [
                new PropertyBuildingInformationDto
                {
                    PropertyId = 101,
                    Id = 501,
                    OldPropertyNo = "OLD-001",
                    OldWing = "A",
                    OldFlatOrShopNumber = "101",
                    OldOwnerName = "Test Owner",
                    Identify = true
                }
            ]
        };

        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                queryParameters,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchBuildingInformation(
            queryParameters,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response = Assert.IsType<
            ApiResponse<PagedResult<PropertyBuildingInformationDto>>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("1 record(s) found", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items!.TotalCount);
        Assert.Single(response.Items.Items);

        var item = response.Items.Items.First();

        Assert.Equal(101, item.PropertyId);
        Assert.Equal(501, item.Id);
        Assert.Equal("OLD-001", item.OldPropertyNo);
        Assert.True(item.Identify);

        _mockPropertyService.Verify(
            service => service.SearchBuildingInformationAsync(
                queryParameters,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchBuildingInformation_WithNoResults_ReturnsOkWithNoRecordsMessage()
    {
        // Arrange
        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1",
            PageNumber = 1,
            PageSize = 10
        };

        var expectedResult = new PagedResult<PropertyBuildingInformationDto>
        {
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10,
            Items = []
        };

        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                queryParameters,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchBuildingInformation(
            queryParameters,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response = Assert.IsType<
            ApiResponse<PagedResult<PropertyBuildingInformationDto>>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.Equal(
            "No records found matching the search criteria",
            response.Message);

        Assert.NotNull(response.Items);
        Assert.Equal(0, response.Items!.TotalCount);
        Assert.Empty(response.Items.Items);
    }

    [Fact]
    public async Task SearchBuildingInformation_PropagatesCancellationToken()
    {
        // Arrange
        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1"
        };

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var token = cancellationTokenSource.Token;

        var expectedResult = new PagedResult<PropertyBuildingInformationDto>
        {
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10,
            Items = []
        };

        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                queryParameters,
                token))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.SearchBuildingInformation(
            queryParameters,
            token);

        // Assert
        _mockPropertyService.Verify(
            service => service.SearchBuildingInformationAsync(
                queryParameters,
                token),
            Times.Once);
    }

    [Fact]
    public async Task SearchBuildingInformation_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1"
        };

        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                queryParameters,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Building information search failed."));

        // Act and Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.SearchBuildingInformation(
                queryParameters,
                CancellationToken.None));

        Assert.Equal(
            "Building information search failed.",
            exception.Message);
    }

    [Fact]
    public async Task SearchBuildingInformation_WithNullParameters_PropagatesArgumentNullException()
    {
        // Arrange
        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                null!,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentNullException("queryParameters"));

        // Act and Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _controller.SearchBuildingInformation(
                null!,
                CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchBuildingInformation_WithMissingOldWardNo_PropagatesInvalidOperationException(
        string? oldWardNo)
    {
        // Arrange
        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = oldWardNo
        };

        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                queryParameters,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("BuildingInformation_OldWardNo_Required"));

        // Act and Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.SearchBuildingInformation(
                queryParameters,
                CancellationToken.None));

        Assert.Equal("BuildingInformation_OldWardNo_Required", exception.Message);
    }

    [Fact]
    public async Task SearchBuildingInformation_WithOnlyRequiredParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1"
        };

        var expectedResult = new PagedResult<PropertyBuildingInformationDto>
        {
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10,
            Items =
            [
                new PropertyBuildingInformationDto { Id = 1, OldPropertyNo = "OLD-1" },
                new PropertyBuildingInformationDto { Id = 2, OldPropertyNo = "OLD-2" }
            ]
        };

        _mockPropertyService
            .Setup(service => service.SearchBuildingInformationAsync(
                queryParameters,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchBuildingInformation(
            queryParameters,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertyBuildingInformationDto>>>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("2 record(s) found", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(2, response.Items.TotalCount);
        Assert.Equal(2, response.Items.Items.Count());
    }
}