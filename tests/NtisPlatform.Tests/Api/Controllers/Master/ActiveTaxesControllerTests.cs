using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class ActiveTaxesControllerTests
{
    private static ActiveTaxesController Create(out Mock<IActiveTaxesService> service)
    {
        service = new Mock<IActiveTaxesService>();
        var logger = new Mock<ILogger<ActiveTaxesController>>();
        return new ActiveTaxesController(service.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var query = new ActiveTaxesQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ActiveTaxesDto>(new List<ActiveTaxesDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveTaxesDto { Id = 1 });

        var result = await controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new CreateActiveTaxesDto();
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveTaxesDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new UpdateActiveTaxesDto();
        service.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveTaxesDto { Id = 1 });

        var result = await controller.Update(1, dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
