using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.CommonRemarkDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class CommonRemarkDetailsControllerTests
{
    private static CommonRemarkDetailsController Create(
        out Mock<ICommonRemarkDetailsService> service,
        Mock<IHardDeleteCleanupService>? cleanupService = null,
        Mock<IReferenceValidationService>? referenceValidationService = null)
    {
        service = new Mock<ICommonRemarkDetailsService>();
        var cleanup = cleanupService ?? new Mock<IHardDeleteCleanupService>();
        var refVal = referenceValidationService ?? new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<CommonRemarkDetailsController>>();
        return new CommonRemarkDetailsController(service.Object, cleanup.Object, refVal.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var query = new CommonRemarkDetailsQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CommonRemarkDetailsDtos>(new List<CommonRemarkDetailsDtos>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new CreateCommonRemarkDetailsDto();
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new CommonRemarkDetailsDtos { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_WithValidId_ReturnsOk()
    {
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        cleanupMock.Setup(s => s.ForceHardDeleteAsync<CommonRemarkDetailsEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = Create(out _, cleanupMock);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_WithInvalidId_ReturnsOk()
    {
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        cleanupMock.Setup(s => s.ForceHardDeleteAsync<CommonRemarkDetailsEntity, int>(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = Create(out _, cleanupMock);

        var result = await controller.Purge(999, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
