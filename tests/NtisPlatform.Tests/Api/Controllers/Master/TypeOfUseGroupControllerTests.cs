using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class TypeOfUseGroupControllerTests
{
    private static TypeOfUseGroupController Create(
        out Mock<ITypeOfUseGroupService> service,
        Mock<IHardDeleteCleanupService>? cleanupService = null,
        Mock<IReferenceValidationService>? referenceValidationService = null)
    {
        service = new Mock<ITypeOfUseGroupService>();
        var cleanup = cleanupService ?? new Mock<IHardDeleteCleanupService>();
        var refVal = referenceValidationService ?? new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<TypeOfUseGroupController>>();
        return new TypeOfUseGroupController(service.Object, cleanup.Object, refVal.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var qp = new TypeOfUseGroupQueryParameters();
        service.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<TypeOfUseGroupDto>(new List<TypeOfUseGroupDto>(), 0, 1, 10));

        var result = await controller.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TypeOfUseGroupDto { Id = 1 });

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupDto?)null);

        var result = await controller.GetById(99, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateAsync(It.IsAny<CreateTypeOfUseGroupDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TypeOfUseGroupDto { Id = 1 });

        var result = await controller.Create(new CreateTypeOfUseGroupDto { TypeOfUseGroupCode = "R", GroupName = "Residential", GroupIcon = "Home" }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateTypeOfUseGroupDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TypeOfUseGroupDto { Id = 1 });

        var result = await controller.Update(1, new UpdateTypeOfUseGroupDto { TypeOfUseGroupCode = "R", GroupName = "Residential", GroupIcon = "Home" }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsOkWithFailureResponse()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(99, It.IsAny<UpdateTypeOfUseGroupDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupDto?)null);

        var result = await controller.Update(99, new UpdateTypeOfUseGroupDto { TypeOfUseGroupCode = "R", GroupName = "Residential" }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TypeOfUseGroupDto>>(okResult.Value);
        Assert.False(response.Success);
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
    public async Task Delete_NotFound_ReturnsOkWithFailureResponse()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Delete(99, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TypeOfUseGroupDto>>(okResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Purge_WithValidId_ReturnsOk()
    {
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        cleanupMock.Setup(s => s.ForceHardDeleteAsync<TypeOfUseGroupEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = Create(out _, cleanupMock);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_WithInvalidId_ReturnsOk()
    {
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        cleanupMock.Setup(s => s.ForceHardDeleteAsync<TypeOfUseGroupEntity, int>(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = Create(out _, cleanupMock);

        var result = await controller.Purge(999, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
