using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class SocietyDetailsControllerTests
{
    private static SocietyDetailsController Create(
        out Mock<ISocietyDetailsService> service,
        out Mock<IHardDeleteCleanupService> cleanup)
    {
        service = new Mock<ISocietyDetailsService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        var logger = new Mock<ILogger<SocietyDetailsController>>();
        return new SocietyDetailsController(service.Object, cleanup.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var query = new SocietyDetailsQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SocietyDetailsDto>(new List<SocietyDetailsDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
