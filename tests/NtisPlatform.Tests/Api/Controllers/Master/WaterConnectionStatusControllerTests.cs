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

public class WaterConnectionStatusControllerTests
{
    private static WaterConnectionStatusController Create(
        out Mock<IWaterConnectionStatusService> service,
        out Mock<IHardDeleteCleanupService> cleanup)
    {
        service = new Mock<IWaterConnectionStatusService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        var logger = new Mock<ILogger<WaterConnectionStatusController>>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();
        return new WaterConnectionStatusController(service.Object, cleanup.Object, mockReferenceValidationService.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var query = new WaterConnectionStatusQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<WaterConnectionStatusDto>(new List<WaterConnectionStatusDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var dto = new CreateWaterConnectionStatusDto();
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new WaterConnectionStatusDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
