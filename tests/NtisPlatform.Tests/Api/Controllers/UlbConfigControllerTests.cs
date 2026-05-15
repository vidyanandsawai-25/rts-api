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
    private static UlbConfigController Create(out Mock<IUlbConfigService> service)
    {
        service = new Mock<IUlbConfigService>();
        var logger = new Mock<ILogger<UlbConfigController>>();
        return new UlbConfigController(service.Object, logger.Object);
    }

    [Fact]
    public async Task GetConfig_ReturnsOk_WhenConfigExists()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetUlbConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UlbConfigDto());

        var result = await controller.GetConfig(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetConfig_ReturnsNotFound_WhenConfigMissing()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetUlbConfigAsync(It.IsAny<CancellationToken>())).ReturnsAsync((UlbConfigDto?)null);

        var result = await controller.GetConfig(CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetConfig_PropagatesOperationCanceledException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetUlbConfigAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => controller.GetConfig(CancellationToken.None));
    }

    [Fact]
    public async Task GetConfig_Returns500_OnUnexpectedException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetUlbConfigAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetConfig(CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
