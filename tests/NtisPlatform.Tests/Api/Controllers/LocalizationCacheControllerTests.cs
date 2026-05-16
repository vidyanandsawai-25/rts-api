using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class LocalizationCacheControllerTests
{
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly Mock<ILogger<LocalizationCacheController>> _mockLogger;
    private readonly LocalizationCacheController _controller;

    public LocalizationCacheControllerTests()
    {
        _mockLocalizationService = new Mock<ILocalizationService>();
        _mockLogger = new Mock<ILogger<LocalizationCacheController>>();
        _controller = new LocalizationCacheController(_mockLocalizationService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Invalidate_WithNoParameters_ReturnsOk()
    {
        // Act
        var result = _controller.Invalidate();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.Invalidate(null, null, null), Times.Once);
    }

    [Fact]
    public void Invalidate_WithResource_ReturnsOk()
    {
        // Arrange
        var resource = "ValidationMessages";

        // Act
        var result = _controller.Invalidate(resource);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.Invalidate(resource, null, null), Times.Once);
    }

    [Fact]
    public void Invalidate_WithResourceAndLanguage_ReturnsOk()
    {
        // Arrange
        var resource = "ValidationMessages";
        var language = "hi";

        // Act
        var result = _controller.Invalidate(resource, language);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.Invalidate(resource, language, null), Times.Once);
    }

    [Fact]
    public void Invalidate_WithAllParameters_ReturnsOk()
    {
        // Arrange
        var resource = "ValidationMessages";
        var language = "hi";
        var key = "FloorID_Required";

        // Act
        var result = _controller.Invalidate(resource, language, key);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.Invalidate(resource, language, key), Times.Once);
    }

    [Fact]
    public void Invalidate_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockLocalizationService.Setup(s => s.Invalidate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Test exception"));

        // Act
        var result = _controller.Invalidate();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Reload_WithNoParameters_ReturnsOk()
    {
        // Arrange
        _mockLocalizationService.Setup(s => s.ReloadAsync(null, null, null, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Reload();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.ReloadAsync(null, null, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reload_WithResource_ReturnsOk()
    {
        // Arrange
        var resource = "ValidationMessages";
        _mockLocalizationService.Setup(s => s.ReloadAsync(resource, null, null, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Reload(resource);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.ReloadAsync(resource, null, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reload_WithResourceAndLanguage_ReturnsOk()
    {
        // Arrange
        var resource = "ValidationMessages";
        var language = "mr";
        _mockLocalizationService.Setup(s => s.ReloadAsync(resource, language, null, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Reload(resource, language);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.ReloadAsync(resource, language, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reload_WithAllParameters_ReturnsOk()
    {
        // Arrange
        var resource = "ValidationMessages";
        var language = "mr";
        var key = "FloorID_MaxLen_5";
        _mockLocalizationService.Setup(s => s.ReloadAsync(resource, language, key, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Reload(resource, language, key);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.ReloadAsync(resource, language, key, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reload_WhenCancelled_ReturnsStatusCode499()
    {
        // Arrange
        _mockLocalizationService.Setup(s => s.ReloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _controller.Reload();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(499, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Reload_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockLocalizationService.Setup(s => s.ReloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.Reload();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithNoParameters_ReturnsOk()
    {
        // Arrange
        _mockLocalizationService.Setup(s => s.RefreshAsync(null, null, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Refresh();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.RefreshAsync(null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithResource_ReturnsOk()
    {
        // Arrange
        var resource = "ValidationMessages";
        _mockLocalizationService.Setup(s => s.RefreshAsync(resource, null, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Refresh(resource);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.RefreshAsync(resource, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithResourceAndLanguage_ReturnsOk()
    {
        // Arrange
        var resource = "ValidationMessages";
        var language = "hi";
        _mockLocalizationService.Setup(s => s.RefreshAsync(resource, language, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Refresh(resource, language);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.RefreshAsync(resource, language, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithAllParameters_ReturnsOk()
    {
        // Arrange
        var resource = "ValidationMessages";
        var language = "hi";
        var key = "FloorID_Required";
        _mockLocalizationService.Setup(s => s.RefreshAsync(resource, language, key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Refresh(resource, language, key);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockLocalizationService.Verify(s => s.RefreshAsync(resource, language, key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenCancelled_ReturnsStatusCode499()
    {
        // Arrange
        _mockLocalizationService.Setup(s => s.RefreshAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _controller.Refresh();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(499, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Refresh_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockLocalizationService.Setup(s => s.RefreshAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.Refresh();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
}
