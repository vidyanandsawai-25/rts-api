using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyPhoto;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyPhotoControllerTests
{
    private static PropertyPhotoController Create(
        out Mock<IPropertyPhotoApplicationService> service,
        bool isDevelopment = false,
        int? userId = 42)
    {
        service = new Mock<IPropertyPhotoApplicationService>();
        var logger = new Mock<ILogger<PropertyPhotoController>>();
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(isDevelopment ? "Development" : "Production");

        var config = new Mock<IConfiguration>();
        var section = new Mock<IConfigurationSection>();
        section.Setup(s => s.Value).Returns((string?)null);
        config.Setup(c => c.GetSection(It.IsAny<string>())).Returns(section.Object);
        var fileHelper = new FileValidationHelper(config.Object);

        var controller = new PropertyPhotoController(service.Object, logger.Object, env.Object, fileHelper);

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
        return new FormFile(stream, 0, content.Length, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    #region GetPhotosByProperty

    [Fact]
    public async Task GetPhotosByProperty_ReturnsBadRequest_WhenPropertyIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetPhotosByProperty(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetPhotosByProperty_ReturnsOk_WhenPropertyIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetPhotosByPropertyAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyPhotoDto>());

        var result = await controller.GetPhotosByProperty(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPhotosByProperty_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetPhotosByPropertyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.GetPhotosByProperty(123, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion

    #region GetPhotoTypesWithStatus

    [Fact]
    public async Task GetPhotoTypesWithStatus_ReturnsBadRequest_WhenPropertyIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetPhotoTypesWithStatus(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetPhotoTypesWithStatus_ReturnsOk_WhenPropertyIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetPhotoTypesWithStatusAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyPhotoTypeWithStatusDto>());

        var result = await controller.GetPhotoTypesWithStatus(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region GetGroupedPhotosByProperty

    [Fact]
    public async Task GetGroupedPhotosByProperty_ReturnsBadRequest_WhenPropertyIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetGroupedPhotosByProperty(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetGroupedPhotosByProperty_ReturnsOk_WhenPropertyIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetGroupedPhotosByPropertyAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyPhotoGalleryDto { PropertyId = 123 });

        var result = await controller.GetGroupedPhotosByProperty(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion
}
