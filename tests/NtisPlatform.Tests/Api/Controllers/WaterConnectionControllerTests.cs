using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class WaterConnectionControllerTests
{
    private static WaterConnectionController Create(
        out Mock<IWaterConnectionService> service,
        out Mock<IHardDeleteCleanupService> cleanup)
    {
        service = new Mock<IWaterConnectionService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        var logger = new Mock<ILogger<WaterConnectionController>>();
        return new WaterConnectionController(service.Object, cleanup.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var query = new WaterConnectionQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<WaterConnectionDto>(new List<WaterConnectionDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetByIdWithFinanceYearAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaterConnectionDto { Id = 1 });

        var result = await controller.GetById(1, null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetByIdWithFinanceYearAsync(99, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaterConnectionDto?)null);

        var result = await controller.GetById(99, 2024, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_Returns500_OnException()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetByIdWithFinanceYearAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("oops"));

        var result = await controller.GetById(1, null, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var dto = new CreateWaterConnectionDto();
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new WaterConnectionDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var dto = new UpdateWaterConnectionDto();
        service.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>())).ReturnsAsync(new WaterConnectionDto { Id = 1 });

        var result = await controller.Update(1, dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
