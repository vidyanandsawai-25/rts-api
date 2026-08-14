using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Test suite for PropertyController.GetMaxPartition (`getmaxpartition`) endpoint.
/// Covers every branch: found, not found (null), validation error (400) and unexpected error (500).
/// </summary>
public class PropertyControllerMaxPartitionTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly PropertyController _controller;

    public PropertyControllerMaxPartitionTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();
        _controller = PropertyControllerTestHelper.CreateController(_mockPropertyService, _mockLogger);
    }

    #region Happy Path Tests

    [Fact]
    public async Task GetMaxPartition_WithExistingProperty_ReturnsOkWithMaxPartition()
    {
        // Arrange
        var wardId = 1;
        var propertyNo = "P001";
        var expected = new MaxPartitionNoDto
        {
            WardNo = "W001",
            PropertyNo = propertyNo,
            Category = "Residential",
            MaxPartitionNo = "10"
        };

        _mockPropertyService
            .Setup(s => s.GetMaxPartition(wardId, propertyNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetMaxPartition(wardId, propertyNo, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<MaxPartitionNoDto>>().Subject;

        response.Success.Should().BeTrue();
        response.Message.Should().Be("10 MaxPartition found successfully");
        response.Items.Should().BeEquivalentTo(expected);

        _mockPropertyService.Verify(
            s => s.GetMaxPartition(wardId, propertyNo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("A10")]
    [InlineData("")]
    public async Task GetMaxPartition_WithVariousPartitionValues_ReturnsMessageContainingPartition(string maxPartitionNo)
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetMaxPartition(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaxPartitionNoDto { MaxPartitionNo = maxPartitionNo });

        // Act
        var result = await _controller.GetMaxPartition(5, "P010", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<MaxPartitionNoDto>>().Subject;

        response.Success.Should().BeTrue();
        response.Message.Should().Be($"{maxPartitionNo} MaxPartition found successfully");
        response.Items!.MaxPartitionNo.Should().Be(maxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_WithNullMaxPartitionNoOnDto_ReturnsOkWithSuccessMessage()
    {
        // Arrange - the DTO exists but its MaxPartitionNo is null (property has no partition recorded)
        _mockPropertyService
            .Setup(s => s.GetMaxPartition(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaxPartitionNoDto { WardNo = "W001", PropertyNo = "P001", MaxPartitionNo = null });

        // Act
        var result = await _controller.GetMaxPartition(1, "P001", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<MaxPartitionNoDto>>().Subject;

        response.Success.Should().BeTrue();
        response.Message.Should().Be(" MaxPartition found successfully");
        response.Items.Should().NotBeNull();
    }

    #endregion

    #region Not Found (null result) Tests

    [Fact]
    public async Task GetMaxPartition_WithNullResult_ReturnsOkWithEmptyDtoAndLogsWarning()
    {
        // Arrange
        var wardId = 999;
        var propertyNo = "P999";

        _mockPropertyService
            .Setup(s => s.GetMaxPartition(wardId, propertyNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaxPartitionNoDto?)null);

        // Act
        var result = await _controller.GetMaxPartition(wardId, propertyNo, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<MaxPartitionNoDto>>().Subject;

        response.Success.Should().BeTrue();
        response.Message.Should().Be("No building found for the given parameters");
        response.Items.Should().NotBeNull();
        response.Items!.WardNo.Should().BeEmpty();
        response.Items.PropertyNo.Should().BeEmpty();
        response.Items.Category.Should().BeEmpty();
        response.Items.MaxPartitionNo.Should().BeEmpty();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No building found for the given parameters")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Validation Error Tests (400 Bad Request)

    [Theory]
    [InlineData("Ward with ID 999 not found or is inactive")]
    [InlineData("Property number is required")]
    public async Task GetMaxPartition_WhenServiceThrowsInvalidOperationException_ReturnsBadRequest(string expectedMessage)
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetMaxPartition(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act
        var result = await _controller.GetMaxPartition(999, "P001", CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<MaxPartitionNoDto>>().Subject;

        response.Success.Should().BeFalse();
        response.Message.Should().Be(expectedMessage);
        response.Items.Should().BeNull();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation error getting max partition")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Exception Handling Tests (500 Internal Server Error)

    [Fact]
    public async Task GetMaxPartition_WithUnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetMaxPartition(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetMaxPartition(1, "P001", CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        var response = statusCodeResult.Value.Should().BeOfType<ApiResponse<MaxPartitionNoDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("An unexpected error occurred while getting max partition.");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting max partition")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMaxPartition_WithNullReferenceException_ReturnsInternalServerError()
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetMaxPartition(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NullReferenceException("Object reference not set"));

        // Act
        var result = await _controller.GetMaxPartition(1, "P001", CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        var response = statusCodeResult.Value.Should().BeOfType<ApiResponse<MaxPartitionNoDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("An unexpected error occurred while getting max partition.");
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task GetMaxPartition_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        _mockPropertyService
            .Setup(s => s.GetMaxPartition(1, "P001", cts.Token))
            .ReturnsAsync(new MaxPartitionNoDto { MaxPartitionNo = "3" });

        // Act
        var result = await _controller.GetMaxPartition(1, "P001", cts.Token);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockPropertyService.Verify(s => s.GetMaxPartition(1, "P001", cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetMaxPartition_WhenOperationCancelled_ReturnsInternalServerError()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockPropertyService
            .Setup(s => s.GetMaxPartition(It.IsAny<int>(), It.IsAny<string>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _controller.GetMaxPartition(1, "P001", cts.Token);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        var response = statusCodeResult.Value.Should().BeOfType<ApiResponse<MaxPartitionNoDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("An unexpected error occurred while getting max partition.");
    }

    #endregion

    #region Parameter Pass-through Tests

    [Theory]
    [InlineData(0, "")]
    [InlineData(-1, "P001")]
    [InlineData(int.MaxValue, "P-999/A")]
    public async Task GetMaxPartition_PassesParametersToServiceUnmodified(int wardId, string propertyNo)
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetMaxPartition(wardId, propertyNo, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaxPartitionNoDto?)null);

        // Act
        var result = await _controller.GetMaxPartition(wardId, propertyNo, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockPropertyService.Verify(
            s => s.GetMaxPartition(wardId, propertyNo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
