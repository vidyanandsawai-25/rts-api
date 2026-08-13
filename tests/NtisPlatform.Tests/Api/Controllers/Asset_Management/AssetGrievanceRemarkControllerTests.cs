using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

public class AssetGrievanceRemarkControllerTests
{
    private static AssetGrievanceRemarkController Create(
        out Mock<IAssetGrievanceRemarkService> service,
        out Mock<IHardDeleteCleanupService> cleanupService,
        out Mock<IReferenceValidationService> referenceValidationService)
    {
        service = new Mock<IAssetGrievanceRemarkService>();
        cleanupService = new Mock<IHardDeleteCleanupService>();
        referenceValidationService = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<AssetGrievanceRemarkController>>();
        return new AssetGrievanceRemarkController(service.Object, cleanupService.Object, referenceValidationService.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetGrievanceRemarkQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetGrievanceRemarkDto>(new List<AssetGrievanceRemarkDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetGrievanceRemarkDto { Id = 1, Remark = "Test Remark" });

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetGrievanceRemarkDto?)null);

        var result = await controller.GetById(999, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var dto = new CreateAssetGrievanceRemarkDto { GrievanceCategoryId = 1, Remark = "New Remark" };
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetGrievanceRemarkDto { Id = 1, GrievanceCategoryId = 1, Remark = "New Remark" });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ExistingId_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var dto = new UpdateAssetGrievanceRemarkDto { GrievanceCategoryId = 1, Remark = "Updated Remark" };
        service.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetGrievanceRemarkDto { Id = 1, GrievanceCategoryId = 1, Remark = "Updated Remark" });

        var result = await controller.Update(1, dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsOkWithSuccessFalse()
    {
        var controller = Create(out var service, out _, out _);
        var dto = new UpdateAssetGrievanceRemarkDto { GrievanceCategoryId = 1, Remark = "Updated Remark" };
        service.Setup(s => s.UpdateAsync(999, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetGrievanceRemarkDto?)null);

        var result = await controller.Update(999, dto, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<AssetGrievanceRemarkDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsOkWithSuccessFalse()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.Delete(999, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<AssetGrievanceRemarkDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Purge_ReturnsOk()
    {
        var controller = Create(out _, out var cleanupService, out var refValService);
        refValService.Setup(r => r.ValidateReferencesAsync<AssetGrievanceRemarkMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        cleanupService.Setup(c => c.ForceHardDeleteAsync<AssetGrievanceRemarkMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
