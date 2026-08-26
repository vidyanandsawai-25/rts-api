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

public class GisKpiMasterControllerTests
{
    private static GisKpiMasterController Create(out Mock<IGisKpiMasterService> service)
    {
        service = new Mock<IGisKpiMasterService>();
        var cleanupService = new Mock<IHardDeleteCleanupService>();
        var referenceValidationService = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<GisKpiMasterController>>();
        return new GisKpiMasterController(service.Object, cleanupService.Object, referenceValidationService.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var query = new GisKpiMasterQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<GisKpiMasterDto>(new List<GisKpiMasterDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new CreateGisKpiMasterDto { KpiCode = "TOTAL_PROPERTIES", DefaultTitle = "Total Properties" };
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new GisKpiMasterDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
