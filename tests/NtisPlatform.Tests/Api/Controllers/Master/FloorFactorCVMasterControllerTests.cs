using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.FloorFactorCVMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class FloorFactorCVMasterControllerTests
{
    private static FloorFactorCVMasterController Create(
        out Mock<IFloorFactorCVMasterService> service,
        out Mock<IHardDeleteCleanupService> cleanupService,
        out Mock<IReferenceValidationService> referenceValidationService)
    {
        service = new Mock<IFloorFactorCVMasterService>();
        cleanupService = new Mock<IHardDeleteCleanupService>();
        referenceValidationService = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<FloorFactorCVMasterController>>();
        return new FloorFactorCVMasterController(service.Object, cleanupService.Object, referenceValidationService.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var query = new FloorFactorCVMasterQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<FloorFactorCVMasterDto>(new List<FloorFactorCVMasterDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var dto = new CreateFloorFactorCVMasterDto();
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new FloorFactorCVMasterDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_ReturnsOk()
    {
        var controller = Create(out _, out var cleanupService, out _);
        cleanupService.Setup(s => s.ForceHardDeleteAsync<FloorFactorCVMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task BulkPurge_ReturnsOk()
    {
        var controller = Create(out _, out var cleanupService, out var referenceValidationService);
        var ids = new[] { 1, 2 };
        referenceValidationService.Setup(s => s.GetReferencingTablesWithDataAsync<FloorFactorCVMasterEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        cleanupService.Setup(s => s.BulkForceHardDeleteAsync<FloorFactorCVMasterEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NtisPlatform.Application.DTOs.Bulk.BulkResult<int>(2, 0, new List<int> { 1, 2 }));

        var result = await controller.BulkPurge(ids, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
