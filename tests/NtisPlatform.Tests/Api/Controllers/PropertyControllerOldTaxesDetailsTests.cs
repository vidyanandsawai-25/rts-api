using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerOldTaxesDetailsTests
{
    private static PropertyController Create(out Mock<IPropertyService> service)
    {
        service = new Mock<IPropertyService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return PropertyControllerTestHelper.CreateController(service, logger);
    }

    [Fact]
    public async Task GetOldTaxesDetails_ReturnsOk_WhenFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetOldTaxesDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyOldTaxesDetailsDto());

        var result = await controller.GetOldTaxesDetails(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetOldTaxesDetails_ReturnsNotFound_WhenMissing()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetOldTaxesDetailsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyOldTaxesDetailsDto?)null);

        var result = await controller.GetOldTaxesDetails(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetOldTaxesDetails_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetOldTaxesDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetOldTaxesDetails(1, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task UpdateOldTaxesDetails_ReturnsOk_WhenSuccess()
    {
        var controller = Create(out var service);
        var dto = new UpdatePropertyOldTaxesDetailsDto();
        service.Setup(s => s.UpdateOldTaxesDetailsAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyOldTaxesDetailsDto());

        var result = await controller.UpdateOldTaxesDetails(1, dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateOldTaxesDetails_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateOldTaxesDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyOldTaxesDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyOldTaxesDetailsDto?)null);

        var result = await controller.UpdateOldTaxesDetails(99, new UpdatePropertyOldTaxesDetailsDto(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateOldTaxesDetails_ReturnsBadRequest_OnInvalidOperation()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateOldTaxesDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyOldTaxesDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("invalid"));

        var result = await controller.UpdateOldTaxesDetails(1, new UpdatePropertyOldTaxesDetailsDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateOldTaxesDetails_Returns500_OnGenericException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateOldTaxesDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyOldTaxesDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await controller.UpdateOldTaxesDetails(1, new UpdatePropertyOldTaxesDetailsDto(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task CreateOldTaxesDetails_ReturnsCreated_WhenSuccess()
    {
        var controller = Create(out var service);
        var dto = new UpdatePropertyOldTaxesDetailsDto();
        service.Setup(s => s.CreateOldTaxesDetailsAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyOldTaxesDetailsDto { PropertyId = 1 });

        var result = await controller.CreateOldTaxesDetails(1, dto, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        var createdResult = result as CreatedAtActionResult;
        Assert.Equal(nameof(PropertyController.GetOldTaxesDetails), createdResult!.ActionName);
    }

    [Fact]
    public async Task CreateOldTaxesDetails_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateOldTaxesDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyOldTaxesDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyOldTaxesDetailsDto?)null);

        var result = await controller.CreateOldTaxesDetails(99, new UpdatePropertyOldTaxesDetailsDto(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateOldTaxesDetails_ReturnsConflict_WhenRecordsAlreadyExist()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateOldTaxesDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyOldTaxesDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot create records - the following year-tax combinations already exist: 2022-23 - Property Tax"));

        var result = await controller.CreateOldTaxesDetails(1, new UpdatePropertyOldTaxesDetailsDto(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CreateOldTaxesDetails_ReturnsBadRequest_OnValidationError()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateOldTaxesDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyOldTaxesDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate finance years are not allowed"));

        var result = await controller.CreateOldTaxesDetails(1, new UpdatePropertyOldTaxesDetailsDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateOldTaxesDetails_Returns500_OnGenericException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateOldTaxesDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyOldTaxesDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await controller.CreateOldTaxesDetails(1, new UpdatePropertyOldTaxesDetailsDto(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
