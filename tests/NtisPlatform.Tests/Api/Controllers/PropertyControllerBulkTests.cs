using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Comprehensive tests for PropertyController Bulk operations
/// Target: 100% line coverage and branch coverage
/// </summary>
public class PropertyControllerBulkTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly PropertyController _controller;

    public PropertyControllerBulkTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();
        _controller = new PropertyController(_mockPropertyService.Object, _mockLogger.Object);
    }

    #region BulkCreate Tests

    [Fact]
    public async Task BulkCreate_WithValidItems_ReturnsOk()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 },
            new CreateBulkPropertyDto { PropertyNo = "PROP-002", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var expectedResult = new BulkResult<CreateBulkPropertyResponseDto>(
            2, 0,
            new List<CreateBulkPropertyResponseDto>
            {
                new() { Success = true, PropertyId = 1 },
                new() { Success = true, PropertyId = 2 }
            });

        _mockPropertyService
            .Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var bulkResult = Assert.IsType<BulkResult<CreateBulkPropertyResponseDto>>(okResult.Value);
        Assert.Equal(2, bulkResult.SuccessCount);
        Assert.Equal(0, bulkResult.FailedCount);
        _mockPropertyService.Verify(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreate_WithNullItems_ReturnsBadRequest()
    {
        // Arrange
        CreateBulkPropertyDto[]? items = null;

        // Act
        var result = await _controller.BulkCreate(items!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<CreateBulkPropertyResponseDto>(badRequestResult.Value);
        Assert.Equal("Please enter property details.", response.Message);
        _mockPropertyService.Verify(x => x.BulkCreateAsync(It.IsAny<CreateBulkPropertyDto[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreate_WithEmptyArray_ReturnsBadRequest()
    {
        // Arrange
        var items = Array.Empty<CreateBulkPropertyDto>();

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<CreateBulkPropertyResponseDto>(badRequestResult.Value);
        Assert.Equal("Please enter property details.", response.Message);
        _mockPropertyService.Verify(x => x.BulkCreateAsync(It.IsAny<CreateBulkPropertyDto[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreate_WithServiceException_ReturnsBadRequestWithErrorMessage()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var exceptionMessage = "Database connection failed";
        _mockPropertyService
            .Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<CreateBulkPropertyResponseDto>(badRequestResult.Value);
        Assert.Equal(exceptionMessage, response.Message);
    }

    [Fact]
    public async Task BulkCreate_WithSingleItem_ReturnsOk()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var expectedResult = new BulkResult<CreateBulkPropertyResponseDto>(
            1, 0,
            new List<CreateBulkPropertyResponseDto>
            {
                new() { Success = true, PropertyId = 1 }
            });

        _mockPropertyService
            .Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var bulkResult = Assert.IsType<BulkResult<CreateBulkPropertyResponseDto>>(okResult.Value);
        Assert.Equal(1, bulkResult.SuccessCount);
        Assert.Single(bulkResult.Results);
    }

    [Fact]
    public async Task BulkCreate_WithPartialFailure_ReturnsOkWithFailureInfo()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 },
            new CreateBulkPropertyDto { PropertyNo = "PROP-002", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var expectedResult = new BulkResult<CreateBulkPropertyResponseDto>(
            0, 2,
            new List<CreateBulkPropertyResponseDto>(),
            new List<string> { "1: Duplicate property" });

        _mockPropertyService
            .Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var bulkResult = Assert.IsType<BulkResult<CreateBulkPropertyResponseDto>>(okResult.Value);
        Assert.Equal(0, bulkResult.SuccessCount);
        Assert.Equal(2, bulkResult.FailedCount);
        Assert.True(bulkResult.HasFailures);
        Assert.NotNull(bulkResult.Errors);
    }

    [Fact]
    public async Task BulkCreate_WithNullResult_ReturnsOkWithNull()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        _mockPropertyService
            .Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkResult<CreateBulkPropertyResponseDto>?)null);

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Null(okResult.Value);
    }

    [Fact]
    public async Task BulkCreate_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        _mockPropertyService
            .Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid property data"));

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<CreateBulkPropertyResponseDto>(badRequestResult.Value);
        Assert.Equal("Invalid property data", response.Message);
    }

    [Fact]
    public async Task BulkCreate_LogsErrorOnException()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var exception = new InvalidOperationException("Test exception");
        _mockPropertyService
            .Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred while creating bulk properties")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BulkCreate_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var expectedResult = new BulkResult<CreateBulkPropertyResponseDto>(1, 0, new List<CreateBulkPropertyResponseDto>());

        _mockPropertyService
            .Setup(x => x.BulkCreateAsync(items, token))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BulkCreate(items, token);

        // Assert
        _mockPropertyService.Verify(x => x.BulkCreateAsync(items, token), Times.Once);
    }

    [Fact]
    public async Task BulkCreate_WithLargeDataSet_ReturnsOk()
    {
        // Arrange
        var items = Enumerable.Range(1, 100)
            .Select(i => new CreateBulkPropertyDto
            {
                PropertyNo = $"PROP-{i:D4}",
                WardId = 1,
                TaxZoneId = 1,
                PropertyTypeId = 1,
                CategoryId = 1
            })
            .ToArray();

        var responses = Enumerable.Range(1, 100)
            .Select(i => new CreateBulkPropertyResponseDto { Success = true, PropertyId = i })
            .ToList();

        var expectedResult = new BulkResult<CreateBulkPropertyResponseDto>(100, 0, responses);

        _mockPropertyService
            .Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var bulkResult = Assert.IsType<BulkResult<CreateBulkPropertyResponseDto>>(okResult.Value);
        Assert.Equal(100, bulkResult.SuccessCount);
        Assert.Equal(100, bulkResult.Results.Count);
    }

    #endregion
}
