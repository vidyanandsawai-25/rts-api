using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.UserScreenAccess;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class UserControllerUserAccessTests
{
    private static UserController Create(
        out Mock<IUserScreenAccessService> screenAccessService)
    {
        var userService = new Mock<IUserService>();
        var twoFactorService = new Mock<ITwoFactorAuthenticationService>();
        var logger = new Mock<ILogger<UserController>>();
        screenAccessService = new Mock<IUserScreenAccessService>();
        return new UserController(userService.Object, twoFactorService.Object, logger.Object, screenAccessService.Object);
    }

    [Fact]
    public async Task GetUserScreenAccess_ReturnsOk()
    {
        var controller = Create(out var screenService);
        var query = new UserScreenAccessQueryParameters();
        screenService.Setup(s => s.GetUserScreenAccessAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<UserScreenAccessDto>(new List<UserScreenAccessDto>(), 0, 1, 10));

        var result = await controller.GetUserScreenAccess(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetUserScreenAccess_ReturnsBadRequest_OnException()
    {
        var controller = Create(out var screenService);
        screenService.Setup(s => s.GetUserScreenAccessAsync(It.IsAny<UserScreenAccessQueryParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetUserScreenAccess(new UserScreenAccessQueryParameters(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetUserScreens_ReturnsOk_WhenScreensFound()
    {
        var controller = Create(out var screenService);
        screenService.Setup(s => s.GetUserScreensByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new UserScreenAccessDto() });

        var result = await controller.GetUserScreens(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetUserScreens_ReturnsNotFound_WhenEmpty()
    {
        var controller = Create(out var screenService);
        screenService.Setup(s => s.GetUserScreensByUserIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserScreenAccessDto>());

        var result = await controller.GetUserScreens(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetUserScreens_ReturnsBadRequest_OnException()
    {
        var controller = Create(out var screenService);
        screenService.Setup(s => s.GetUserScreensByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetUserScreens(1, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetScreensByRole_ReturnsOk()
    {
        var controller = Create(out var screenService);
        screenService.Setup(s => s.GetUserScreenAccessAsync(It.IsAny<UserScreenAccessQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<UserScreenAccessDto>(new List<UserScreenAccessDto>(), 0, 1, 1000));

        var result = await controller.GetScreensByRole(5, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetScreensByRole_ReturnsBadRequest_OnException()
    {
        var controller = Create(out var screenService);
        screenService.Setup(s => s.GetUserScreenAccessAsync(It.IsAny<UserScreenAccessQueryParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetScreensByRole(5, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
