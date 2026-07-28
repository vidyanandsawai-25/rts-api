using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

public class AssetDocumentDefinitionControllerTests
{
    private static AssetDocumentDefinitionController Create(
        out Mock<IAssetDocumentDefinitionService> service,
        Mock<IHardDeleteCleanupService>? cleanupService = null,
        Mock<IReferenceValidationService>? referenceValidationService = null)
    {
        service = new Mock<IAssetDocumentDefinitionService>();
        var cleanup = cleanupService ?? new Mock<IHardDeleteCleanupService>();
        var refVal = referenceValidationService ?? new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<AssetDocumentDefinitionController>>();
        return new AssetDocumentDefinitionController(service.Object, cleanup.Object, refVal.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var qp = new AssetDocumentDefinitionQueryParameters { DocumentCode = "TAX", DocumentName = "Tax", AssetCategoryId = 1, AssetTypeId = 1, SearchTerm = "Receipt", IsActive = true };
        service.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetDocumentDefinitionDto>(new List<AssetDocumentDefinitionDto>(), 0, 1, 10));

        var result = await controller.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDocumentDefinitionDto { Id = 1, DocumentCode = "TAX_RECEIPT" });

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetDocumentDefinitionDto?)null);

        var result = await controller.GetById(99, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new CreateAssetDocumentDefinitionDto { DocumentCode = "TAX_RECEIPT", DocumentName = "Tax Receipt" };
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDocumentDefinitionDto { Id = 1, DocumentCode = "TAX_RECEIPT" });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new UpdateAssetDocumentDefinitionDto { DocumentCode = "TAX_RECEIPT", DocumentName = "Updated Tax Receipt" };
        service.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDocumentDefinitionDto { Id = 1, DocumentCode = "TAX_RECEIPT" });

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
    public async Task Purge_WithValidId_ReturnsOk()
    {
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        cleanupMock.Setup(s => s.ForceHardDeleteAsync<AssetDocumentDefinitionEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = Create(out _, cleanupMock);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
