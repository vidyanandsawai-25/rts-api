using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class TaxZoningRangeControllerTests
{
    private static TaxZoningRangeController Create(out Mock<ITaxZoningRangeService> service)
    {
        service = new Mock<ITaxZoningRangeService>();
        var logger = new Mock<ILogger<TaxZoningRangeController>>();
        var ulbConfigService = new Mock<IUlbConfigService>();
        var controller = new TaxZoningRangeController(service.Object, logger.Object, ulbConfigService.Object);

        var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, "1")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext 
        { 
            HttpContext = new DefaultHttpContext { User = claimsPrincipal } 
        };
        return controller;
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResult()
    {
        var controller = Create(out var service);
        var query = new TaxZoningRangeQueryParameters();
        var paged = new PagedResult<TaxZoningRangeDto>(new List<TaxZoningRangeDto>(), 0, 1, 10);
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((TaxZoningRangeDto?)null);

        var result = await controller.GetById(5, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new TaxZoningRangeDto { Id = 5 });

        var result = await controller.GetById(5, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk_WhenSuccessful()
    {
        var controller = Create(out var service);
        var dto = new CreateTaxZoningRangeDto { WardIds = new List<int> { 1 }, TaxZoneId = 1, ZoneDescription = "A valid description of 15+ chars" };
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaxZoningRangeDto> { new() { Id = 1 } });

        var result = await controller.Create(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<TaxZoningRangeDto>>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Create_OnArgumentException_ThrowsArgumentException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateAsync(It.IsAny<CreateTaxZoningRangeDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("gap detected"));

        await Assert.ThrowsAsync<ArgumentException>(() => controller.Create(new CreateTaxZoningRangeDto(), CancellationToken.None));
    }

    [Fact]
    public async Task Create_OnGenericException_ThrowsInvalidOperationException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateAsync(It.IsAny<CreateTaxZoningRangeDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Create(new CreateTaxZoningRangeDto(), CancellationToken.None));
    }

    [Fact]
    public async Task Update_ReturnsNotFoundMessage_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateTaxZoningRangeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxZoningRangeDto?)null);

        var result = await controller.Update(5, new UpdateTaxZoningRangeDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxZoningRangeDto>>(ok.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Update_OnArgumentException_ThrowsArgumentException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateTaxZoningRangeDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("gap detected"));

        await Assert.ThrowsAsync<ArgumentException>(() => controller.Update(5, new UpdateTaxZoningRangeDto(), CancellationToken.None));
    }

    [Fact]
    public async Task Bulk_ReturnsBadRequest_WhenNoItems()
    {
        var controller = Create(out _);

        var result = await controller.Bulk(new BulkTaxZoningRangeRequest { Items = new List<CreateTaxZoningRangeDto>() }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Bulk_ReturnsOk_WithPartialResult()
    {
        var controller = Create(out var service);
        var request = new BulkTaxZoningRangeRequest { Items = new List<CreateTaxZoningRangeDto> { new() { ZoneDescription = "A valid description of 15+ chars" } } };
        var rangeResult = new RangeResult<TaxZoningRangeDto>(1, 1, new List<TaxZoningRangeDto> { new() { Id = 1 } }, new List<string> { "Item 1: bad" });
        service.Setup(s => s.BulkUpsertAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(rangeResult);

        var result = await controller.Bulk(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.False(response.Success); // has failures
    }

    [Fact]
    public async Task Coverage_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetCoverageAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(new TaxZoningCoverageDto());

        var result = await controller.Coverage(null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task WardAbstract_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetWardAbstractAsync(It.IsAny<WardAbstractQueryParameters>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new PagedResult<WardZoningAbstractDto>());

        var result = await controller.WardAbstract(new WardAbstractQueryParameters(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
