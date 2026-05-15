using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class LocalizationCacheControllerTests
{
    private static LocalizationCacheController Create(out Mock<ILocalizationService> service)
    {
        service = new Mock<ILocalizationService>();
        var logger = new Mock<ILogger<LocalizationCacheController>>();
        return new LocalizationCacheController(service.Object, logger.Object);
    }

    [Fact]
    public void Invalidate_ReturnsOk()
    {
        var controller = Create(out var service);

        var result = controller.Invalidate("ValidationMessages", "hi", "FloorID_Required");

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.Invalidate("ValidationMessages", "hi", "FloorID_Required"), Times.Once);
    }

    [Fact]
    public void Invalidate_WhenServiceThrows_Returns500()
    {
        var controller = Create(out var service);
        service.Setup(s => s.Invalidate(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Throws(new InvalidOperationException("boom"));

        var result = controller.Invalidate();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task Reload_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.ReloadAsync(null, null, null, false, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await controller.Reload(null, null, null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Reload_WhenCancelled_Returns499()
    {
        var controller = Create(out var service);
        service.Setup(s => s.ReloadAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await controller.Reload(ct: CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(499, status.StatusCode);
    }

    [Fact]
    public async Task Reload_WhenServiceThrows_Returns500()
    {
        var controller = Create(out var service);
        service.Setup(s => s.ReloadAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("oops"));

        var result = await controller.Reload(ct: CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.RefreshAsync(null, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await controller.Refresh(null, null, null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.RefreshAsync(null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenCancelled_Returns499()
    {
        var controller = Create(out var service);
        service.Setup(s => s.RefreshAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await controller.Refresh(ct: CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(499, status.StatusCode);
    }

    [Fact]
    public async Task Refresh_WhenServiceThrows_Returns500()
    {
        var controller = Create(out var service);
        service.Setup(s => s.RefreshAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("oops"));

        var result = await controller.Refresh(ct: CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
