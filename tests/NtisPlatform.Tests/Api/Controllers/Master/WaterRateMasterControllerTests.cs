using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class WaterRateMasterControllerTests
{
    private static WaterRateMasterController Create(
        out Mock<IWaterRateMasterService> service,
        out Mock<IHardDeleteCleanupService> cleanup)
    {
        service = new Mock<IWaterRateMasterService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        var logger = new Mock<ILogger<WaterRateMasterController>>();
        return new WaterRateMasterController(service.Object, cleanup.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var query = new WaterRateMasterQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<WaterRateMasterDto>(new List<WaterRateMasterDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var dto = new CreateWaterRateMasterDto();
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new WaterRateMasterDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
