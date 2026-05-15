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

public class WaterConnectionDetailsControllerTests
{
    private static WaterConnectionDetailsController Create(
        out Mock<IWaterConnectionDetailsService> service,
        out Mock<IHardDeleteCleanupService> cleanup)
    {
        service = new Mock<IWaterConnectionDetailsService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        var logger = new Mock<ILogger<WaterConnectionDetailsController>>();
        return new WaterConnectionDetailsController(service.Object, cleanup.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var query = new WaterConnectionDetailsQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<WaterConnectionDetailsDto>(new List<WaterConnectionDetailsDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var dto = new CreateWaterConnectionDetailsDto();
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new WaterConnectionDetailsDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var dto = new UpdateWaterConnectionDetailsDto();
        service.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>())).ReturnsAsync(new WaterConnectionDetailsDto { Id = 1 });

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

    [Fact]
    public async Task GenerateBill_ReturnsOk_WhenBillReturned()
    {
        var controller = Create(out var service, out _);
        var request = new GenerateBillRequest { WaterConnectionId = 1, FinanceYearId = 2024 };
        service.Setup(s => s.GenerateBillAsync(1, 2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaterConnectionDetailsDto { Id = 9 });

        var result = await controller.GenerateBill(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GenerateBill_ReturnsNoContent_WhenNullResult()
    {
        var controller = Create(out var service, out _);
        var request = new GenerateBillRequest { WaterConnectionId = 1, FinanceYearId = 2024 };
        service.Setup(s => s.GenerateBillAsync(1, 2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaterConnectionDetailsDto?)null);

        var result = await controller.GenerateBill(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GenerateBill_ReturnsBadRequest_OnInvalidOperation()
    {
        var controller = Create(out var service, out _);
        var request = new GenerateBillRequest { WaterConnectionId = 1, FinanceYearId = 2024 };
        service.Setup(s => s.GenerateBillAsync(1, 2024, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bad"));

        var result = await controller.GenerateBill(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GenerateBill_Returns500_OnException()
    {
        var controller = Create(out var service, out _);
        var request = new GenerateBillRequest { WaterConnectionId = 1, FinanceYearId = 2024 };
        service.Setup(s => s.GenerateBillAsync(1, 2024, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("unexpected"));

        var result = await controller.GenerateBill(request, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
