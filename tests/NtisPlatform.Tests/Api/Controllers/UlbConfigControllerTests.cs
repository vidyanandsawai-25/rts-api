using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class UlbConfigControllerTests
{
    private readonly Mock<IUlbConfigService> _mockService;
    private readonly Mock<ILogger<UlbConfigController>> _mockLogger;
    private readonly UlbConfigController _controller;

    public UlbConfigControllerTests()
    {
        _mockService = new Mock<IUlbConfigService>();
        _mockLogger = new Mock<ILogger<UlbConfigController>>();
        _controller = new UlbConfigController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetConfig_ReturnsOk_WhenConfigExists()
    {
        // Arrange
        var expectedConfig = new UlbConfigDto
        {
            UlbId = 1,
            UlbName = "Test ULB",
            UlbCode = "TEST001"
        };

        _mockService.Setup(x => x.GetUlbConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _controller.GetConfig(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedConfig, okResult.Value);
    }

    [Fact]
    public async Task GetConfig_ReturnsNotFound_WhenConfigDoesNotExist()
    {
        // Arrange
        _mockService.Setup(x => x.GetUlbConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((UlbConfigDto?)null);

        // Act
        var result = await _controller.GetConfig(CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetConfig_ReturnsInternalServerError_OnException()
    {
        // Arrange
        _mockService.Setup(x => x.GetUlbConfigAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetConfig(CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetConfig_ThrowsOperationCanceledException_WhenCancelled()
    {
        // Arrange
        _mockService.Setup(x => x.GetUlbConfigAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _controller.GetConfig(CancellationToken.None));
    }

    [Fact]
    public async Task GetConfig_LogsWarning_WhenConfigNotFound()
    {
        // Arrange
        _mockService.Setup(x => x.GetUlbConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((UlbConfigDto?)null);

        // Act
        await _controller.GetConfig(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No active ULB configuration found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConfig_LogsError_OnException()
    {
        // Arrange
        var exception = new Exception("Database error");
        _mockService.Setup(x => x.GetUlbConfigAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _controller.GetConfig(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving ULB configuration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
