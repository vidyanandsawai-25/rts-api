using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Master.AgeFactorCVMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class AgeFactorCVMasterControllerTests
{
    private static AgeFactorCVMasterController Create(
        out Mock<IAgeFactorCVMasterService> service,
        out Mock<IHardDeleteCleanupService> cleanupService,
        out Mock<IReferenceValidationService> referenceValidationService)
    {
        service = new Mock<IAgeFactorCVMasterService>();
        cleanupService = new Mock<IHardDeleteCleanupService>();
        referenceValidationService = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<AgeFactorCVMasterController>>();
        return new AgeFactorCVMasterController(service.Object, cleanupService.Object, referenceValidationService.Object, logger.Object);
    }

    private static AgeFactorCVMasterController Create(out Mock<IAgeFactorCVMasterService> service)
    {
        return Create(out service, out _, out _);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var query = new AgeFactorCVMasterQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AgeFactorCVMasterDto>(new List<AgeFactorCVMasterDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new AgeFactorCVMasterDto { Id = 1 });

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new CreateAgeFactorCVMasterDto();
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new AgeFactorCVMasterDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new UpdateAgeFactorCVMasterDto();
        service.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>())).ReturnsAsync(new AgeFactorCVMasterDto { Id = 1 });

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

    [Fact]
    public async Task BulkCreate_ReturnsOk()
    {
        var controller = Create(out var service);
        var items = new[] { new CreateAgeFactorCVMasterDto() };
        service.Setup(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkResult<AgeFactorCVMasterDto>(1, 0, new List<AgeFactorCVMasterDto>()));

        var result = await controller.BulkCreate(items, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task BulkUpdate_ReturnsOk()
    {
        var controller = Create(out var service);
        var items = new[] { new BulkUpdateItem<int, UpdateAgeFactorCVMasterDto>(1, new UpdateAgeFactorCVMasterDto()) };
        service.Setup(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkResult<AgeFactorCVMasterDto>(1, 0, new List<AgeFactorCVMasterDto>()));

        var result = await controller.BulkUpdate(items, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task BulkDelete_ReturnsOk()
    {
        var controller = Create(out var service);
        var ids = new[] { 1, 2 };
        service.Setup(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkResult<int>(2, 0, new List<int>()));

        var result = await controller.BulkDelete(ids, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_ReturnsOk()
    {
        var controller = Create(out _, out var cleanupService, out _);
        cleanupService.Setup(s => s.ForceHardDeleteAsync<AgeFactorCVMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task BulkPurge_ReturnsOk()
    {
        var controller = Create(out _, out var cleanupService, out var referenceValidationService);
        var ids = new[] { 1, 2 };
        referenceValidationService.Setup(s => s.GetReferencingTablesWithDataAsync<AgeFactorCVMasterEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        cleanupService.Setup(s => s.BulkForceHardDeleteAsync<AgeFactorCVMasterEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkResult<int>(2, 0, new List<int> { 1, 2 }));

        var result = await controller.BulkPurge(ids, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
