using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class TaxZoningControllerTests
{
    private static TaxZoningController Create(out Mock<ITaxZoningService> service)
    {
        service = new Mock<ITaxZoningService>();
        var logger = new Mock<ILogger<TaxZoningController>>();
        var controller = new TaxZoningController(service.Object, logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    [Fact]
    public async Task Get_WithGroupByWard_CallsGetFromToPropertyNo()
    {
        var controller = Create(out var service);
        var query = new TaxZoningQueryParameters { GroupBy = "ward" };
        var paged = new PagedResult<TaxZoningDto>(new List<TaxZoningDto>(), 0, 1, 10);
        service.Setup(s => s.GetFromToPropertyNo(query, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var result = await controller.Get(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetFromToPropertyNo(query, It.IsAny<CancellationToken>()), Times.Once);
        service.Verify(s => s.GetAllPropertyNo(It.IsAny<TaxZoningQueryParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Get_WithoutGroupByWard_CallsGetAllPropertyNo()
    {
        var controller = Create(out var service);
        var query = new TaxZoningQueryParameters();
        var paged = new PagedResult<TaxZoningDto>(new List<TaxZoningDto>(), 0, 1, 10);
        service.Setup(s => s.GetAllPropertyNo(query, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var result = await controller.Get(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetAllPropertyNo(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_OnArgumentException_ReturnsBadRequest()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetAllPropertyNo(It.IsAny<TaxZoningQueryParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("invalid"));

        var result = await controller.Get(new TaxZoningQueryParameters(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_OnGenericException_Returns500()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetAllPropertyNo(It.IsAny<TaxZoningQueryParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.Get(new TaxZoningQueryParameters(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenSuccessful()
    {
        var controller = Create(out var service);
        var dto = new UpdateTaxZoningDto();
        service.Setup(s => s.UpdateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new TaxZoningDto());

        var result = await controller.Update(dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(It.IsAny<UpdateTaxZoningDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxZoningDto?)null);

        var result = await controller.Update(new UpdateTaxZoningDto(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_OnArgumentException_ReturnsBadRequest()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(It.IsAny<UpdateTaxZoningDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("invalid"));

        var result = await controller.Update(new UpdateTaxZoningDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_OnGenericException_Returns500()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(It.IsAny<UpdateTaxZoningDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.Update(new UpdateTaxZoningDto(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
