using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class DepreciationControllerTests
{
    private static DepreciationController Create(
        out Mock<IDepreciationService> service,
        out Mock<IHardDeleteCleanupService> cleanup)
    {
        service = new Mock<IDepreciationService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        var referenceValidation = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<DepreciationController>>();
        return new DepreciationController(service.Object, cleanup.Object, referenceValidation.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var query = new DepreciationQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<DepreciationDtos>(new List<DepreciationDtos>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new DepreciationDtos { Id = 1 });

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
