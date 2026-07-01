using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using System.Text.Json;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class FieldRegistryControllerTests
{
    private static FieldRegistryController Create(out Mock<IFieldRegistryService> service)
    {
        service = new Mock<IFieldRegistryService>();
        var logger = new Mock<ILogger<FieldRegistryController>>();
        return new FieldRegistryController(service.Object, logger.Object);
    }

    // ============== SetFieldRegistryStatus Tests ==============

    [Fact]
    public async Task SetFieldRegistryStatus_ReturnsOk_WithActivatedMessage_WhenIsActiveTrue()
    {
        var controller = Create(out var service);
        service.Setup(s => s.SetActiveStatusAsync("Update_ContactNo", true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.SetFieldRegistryStatus("Update_ContactNo", isActive: true, updatedBy: null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("activated", doc.RootElement.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetFieldRegistryStatus_ReturnsOk_WithDeactivatedMessage_WhenIsActiveFalse()
    {
        var controller = Create(out var service);
        service.Setup(s => s.SetActiveStatusAsync("Update_ContactNo", false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.SetFieldRegistryStatus("Update_ContactNo", isActive: false, updatedBy: null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("deactivated", doc.RootElement.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetFieldRegistryStatus_Returns404_WhenUpdateCodeNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.SetActiveStatusAsync("Unknown_Code", It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.SetFieldRegistryStatus("Unknown_Code", isActive: false, updatedBy: null, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var json = JsonSerializer.Serialize(notFound.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task SetFieldRegistryStatus_PassesUpdatedBy_ToService()
    {
        var controller = Create(out var service);
        service.Setup(s => s.SetActiveStatusAsync("Update_ContactNo", false, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.SetFieldRegistryStatus("Update_ContactNo", isActive: false, updatedBy: 7, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.SetActiveStatusAsync("Update_ContactNo", false, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetFieldRegistryStatus_Returns500_WhenServiceThrowsException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.SetActiveStatusAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.SetFieldRegistryStatus("Update_ContactNo", isActive: true, updatedBy: null, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var json = JsonSerializer.Serialize(serverError.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }
}
