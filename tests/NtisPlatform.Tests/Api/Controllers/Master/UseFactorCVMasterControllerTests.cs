using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.UseFactorCVMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class UseFactorCVMasterControllerTests
{
    private static UseFactorCVMasterController Create(
        out Mock<IUseFactorCVMasterService> service,
        out Mock<IHardDeleteCleanupService> cleanupService,
        out Mock<IReferenceValidationService> referenceValidationService)
    {
        service = new Mock<IUseFactorCVMasterService>();
        cleanupService = new Mock<IHardDeleteCleanupService>();
        referenceValidationService = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<UseFactorCVMasterController>>();
        return new UseFactorCVMasterController(service.Object, cleanupService.Object, referenceValidationService.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var query = new UseFactorCVMasterQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<UseFactorCVMasterDto>(new List<UseFactorCVMasterDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var dto = new CreateUseFactorCVMasterDto();
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new UseFactorCVMasterDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_ReturnsOk()
    {
        var controller = Create(out _, out var cleanupService, out _);
        cleanupService.Setup(s => s.ForceHardDeleteAsync<UseFactorCVMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task BulkPurge_ReturnsOk()
    {
        var controller = Create(out _, out var cleanupService, out var referenceValidationService);
        var ids = new[] { 1, 2 };
        referenceValidationService.Setup(s => s.GetReferencingTablesWithDataAsync<UseFactorCVMasterEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        cleanupService.Setup(s => s.BulkForceHardDeleteAsync<UseFactorCVMasterEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NtisPlatform.Application.DTOs.Bulk.BulkResult<int>(2, 0, new List<int> { 1, 2 }));

        var result = await controller.BulkPurge(ids, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
