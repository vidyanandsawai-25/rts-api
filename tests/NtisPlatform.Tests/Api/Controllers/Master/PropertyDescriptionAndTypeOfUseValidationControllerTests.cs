using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class PropertyDescriptionAndTypeOfUseValidationControllerTests
{
    private static PropertyDescriptionAndTypeOfUseValidationController Create(
        out Mock<IPropertyDescriptionAndTypeOfUseValidationService> service,
        out Mock<IHardDeleteCleanupService> cleanup)
    {
        service = new Mock<IPropertyDescriptionAndTypeOfUseValidationService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        var logger = new Mock<ILogger<PropertyDescriptionAndTypeOfUseValidationController>>();
        return new PropertyDescriptionAndTypeOfUseValidationController(service.Object, cleanup.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var query = new PropertyDescriptionAndTypeOfUseValidationQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PropertyDescriptionAndTypeOfUseValidationDto>(
                new List<PropertyDescriptionAndTypeOfUseValidationDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
