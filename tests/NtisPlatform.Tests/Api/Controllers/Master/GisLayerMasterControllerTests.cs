using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class GisLayerMasterControllerTests
{
    private static GisLayerMasterController Create(out Mock<IGisLayerMasterService> service)
    {
        service = new Mock<IGisLayerMasterService>();
        var cleanupService = new Mock<IHardDeleteCleanupService>();
        var referenceValidationService = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<GisLayerMasterController>>();
        return new GisLayerMasterController(service.Object, cleanupService.Object, referenceValidationService.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var query = new GisLayerMasterQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<GisLayerMasterDto>(new List<GisLayerMasterDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new CreateGisLayerMasterDto { LayerCode = "TAX_BOUNDARY", LayerName = "Tax Boundary Layer" };
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new GisLayerMasterDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
