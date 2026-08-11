using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class InventoryItemModelControllerTests
{
    private static InventoryItemModelController Create(
        out Mock<IInventoryItemModelService> service,
        Mock<IHardDeleteCleanupService>? cleanupService = null,
        Mock<IReferenceValidationService>? referenceValidationService = null)
    {
        service = new Mock<IInventoryItemModelService>();
        var cleanup = cleanupService ?? new Mock<IHardDeleteCleanupService>();
        var refVal = referenceValidationService ?? new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<InventoryItemModelController>>();
        return new InventoryItemModelController(logger.Object, service.Object, cleanup.Object, refVal.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var qp = new InventoryItemModelQueryParameters();
        service.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<InventoryItemModelDto>(new List<InventoryItemModelDto>(), 0, 1, 10));

        var result = await controller.GetAll(qp, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemModelDto { Id = 1, ModelName = "Model X" });

        var result = await controller.GetById(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<InventoryItemModelDto>(ok.Value);
        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemModelDto?)null);

        var result = await controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateAsync(It.IsAny<CreateInventoryItemModelDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemModelDto { Id = 1, ModelName = "Model X" });

        var result = await controller.Create(
            new CreateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "Model X", DisplayOrder = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryItemModelDto>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Create_DuplicateModelName_PropagatesValidationException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateAsync(It.IsAny<CreateInventoryItemModelDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NtisPlatform.Application.Exceptions.ValidationException(
                "ModelName", "InventoryItemModel_ModelName_Duplicate", NtisPlatform.Application.Enums.OperationType.Create));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => controller.Create(
                new CreateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "Model X", DisplayOrder = 1 },
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateInventoryItemModelDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemModelDto { Id = 1, ModelName = "Model Y" });

        var result = await controller.Update(
            1,
            new UpdateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "Model Y", DisplayOrder = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryItemModelDto>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Update_NonExisting_ReturnsOkWithFailure()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateInventoryItemModelDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemModelDto?)null);

        var result = await controller.Update(
            999,
            new UpdateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "Test", DisplayOrder = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryItemModelDto>>(ok.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryItemModelDto>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Delete_NonExisting_ReturnsOkWithFailure()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Delete(999, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryItemModelDto>>(ok.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Purge_WithValidId_ReturnsOk()
    {
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        cleanupMock.Setup(s => s.ForceHardDeleteAsync<InventoryItemModelEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = Create(out _, cleanupMock);

        var result = await controller.Purge(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_WithInvalidId_ReturnsOk()
    {
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        cleanupMock.Setup(s => s.ForceHardDeleteAsync<InventoryItemModelEntity, int>(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = Create(out _, cleanupMock);

        var result = await controller.Purge(999, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
