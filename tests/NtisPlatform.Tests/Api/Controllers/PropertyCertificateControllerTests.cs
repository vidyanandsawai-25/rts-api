using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyCertificateControllerTests
{
    private static PropertyCertificateController Create(
        out Mock<IPropertyCertificateApplicationService> service,
        bool isDevelopment = false,
        int? userId = 42)
    {
        service = new Mock<IPropertyCertificateApplicationService>();
        var logger = new Mock<ILogger<PropertyCertificateController>>();
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(isDevelopment ? "Development" : "Production");

        var config = new Mock<IConfiguration>();
        var section = new Mock<IConfigurationSection>();
        section.Setup(s => s.Value).Returns((string?)null);
        config.Setup(c => c.GetSection(It.IsAny<string>())).Returns(section.Object);
        var fileHelper = new FileValidationHelper(config.Object);

        var controller = new PropertyCertificateController(service.Object, logger.Object, env.Object, fileHelper);

        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            }, "TestAuth"));
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static IFormFile MakeFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        var formFile = new FormFile(stream, 0, content.Length, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        return formFile;
    }

    #region GetCertificateTypesWithStatus

    [Fact]
    public async Task GetCertificateTypesWithStatus_ReturnsBadRequest_WhenPropertyIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetCertificateTypesWithStatus(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetCertificateTypesWithStatus_ReturnsOk_WhenPropertyIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetCertificateTypesWithStatusAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateWithStatusDto>());

        var result = await controller.GetCertificateTypesWithStatus(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCertificateTypesWithStatus_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetCertificateTypesWithStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.GetCertificateTypesWithStatus(123, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion

    #region BulkSaveAll

    [Fact]
    public async Task BulkSaveAll_ReturnsBadRequest_WhenModelStateInvalid()
    {
        var controller = Create(out _);
        controller.ModelState.AddModelError("key", "error");
        var dto = new PropertyCertificateBulkSaveDto { PropertyId = 1, Certificates = new List<PropertyCertificateItemDto>() };

        var result = await controller.BulkSaveAll(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BulkSaveAll_ReturnsUnauthorized_OnUnauthorizedAccessException()
    {
        var controller = Create(out _, userId: null);
        var dto = new PropertyCertificateBulkSaveDto { PropertyId = 1, Certificates = new List<PropertyCertificateItemDto>() };

        var result = await controller.BulkSaveAll(dto, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task BulkSaveAll_ReturnsOk_OnSuccessfulSave()
    {
        var controller = Create(out var service);
        service.Setup(s => s.BulkSaveAllAsync(
            It.IsAny<PropertyCertificateBulkSaveDto>(),
            42,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyCertificateBulkSaveResponseDto
            {
                PropertyId = 1,
                TotalProcessed = 5,
                EnabledCount = 3,
                DisabledCount = 2,
                Errors = new List<string>()
            });

        var dto = new PropertyCertificateBulkSaveDto
        {
            PropertyId = 1,
            Certificates = new List<PropertyCertificateItemDto>()
        };

        var result = await controller.BulkSaveAll(dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task BulkSaveAll_Returns500_OnGenericException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.BulkSaveAllAsync(
            It.IsAny<PropertyCertificateBulkSaveDto>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var dto = new PropertyCertificateBulkSaveDto
        {
            PropertyId = 1,
            Certificates = new List<PropertyCertificateItemDto>()
        };

        var result = await controller.BulkSaveAll(dto, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion
}
