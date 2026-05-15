using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.RoleWiseScreenAccessMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class RoleWiseScreenAccessMasterControllerTests
{
    private static RoleWiseScreenAccessMasterController Create(out Mock<IRoleWiseScreenAccessMasterService> service)
    {
        service = new Mock<IRoleWiseScreenAccessMasterService>();
        var logger = new Mock<ILogger<RoleWiseScreenAccessMasterController>>();
        return new RoleWiseScreenAccessMasterController(service.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var query = new RoleWiseScreenAccessQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<RoleWiseScreenAccessMasterDTO>(new List<RoleWiseScreenAccessMasterDTO>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
