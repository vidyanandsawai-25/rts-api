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

    #region Upload

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileMissing()
    {
        var controller = Create(out _);
        var dto = new PropertyPhotoUploadFormDto { File = null!, PropertyId = 1, PhotoTypeId = 1 };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileEmpty()
    {
        var controller = Create(out _);
        var dto = new PropertyPhotoUploadFormDto
        {
            File = MakeFile("a.jpg", "image/jpeg", Array.Empty<byte>()),
            PropertyId = 1,
            PhotoTypeId = 1
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileTypeInvalid()
    {
        var controller = Create(out _);
        var dto = new PropertyPhotoUploadFormDto
        {
            File = MakeFile("a.exe", "application/x-msdownload", new byte[] { 1, 2, 3 }),
            PropertyId = 1,
            PhotoTypeId = 1
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenPropertyIdInvalid()
    {
        var controller = Create(out _);
        var dto = new PropertyPhotoUploadFormDto
        {
            File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }),
            PropertyId = 0,
            PhotoTypeId = 1
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenPhotoTypeIdInvalid()
    {
        var controller = Create(out _);
        var dto = new PropertyPhotoUploadFormDto
        {
            File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }),
            PropertyId = 1,
            PhotoTypeId = 0
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsOk_OnValidUpload()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UploadPhotoAsync(
            It.IsAny<Stream>(),
            "a.jpg",
            "image/jpeg",
            3,
            1,
            1,
            null,
            null,
            42,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyPhotoUploadResponseDto());

        var dto = new PropertyPhotoUploadFormDto
        {
            File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }),
            PropertyId = 1,
            PhotoTypeId = 1
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsUnauthorized_OnUnauthorizedAccessException()
    {
        var controller = Create(out _, userId: null);
        var dto = new PropertyPhotoUploadFormDto
        {
            File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }),
            PropertyId = 1,
            PhotoTypeId = 1
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_OnArgumentException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UploadPhotoAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("invalid"));

        var dto = new PropertyPhotoUploadFormDto
        {
            File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2 }),
            PropertyId = 1,
            PhotoTypeId = 1
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Replace

    [Fact]
    public async Task Replace_ReturnsBadRequest_WhenPropertyPhotoIdInvalid()
    {
        var controller = Create(out _);
        var dto = new ReplacePropertyPhotoFormDto { File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }) };

        var result = await controller.Replace(0, dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Replace_ReturnsBadRequest_WhenFileMissing()
    {
        var controller = Create(out _);
        var dto = new ReplacePropertyPhotoFormDto { File = null! };

        var result = await controller.Replace(123, dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Replace_ReturnsBadRequest_WhenFileTypeInvalid()
    {
        var controller = Create(out _);
        var dto = new ReplacePropertyPhotoFormDto { File = MakeFile("a.exe", "application/x-msdownload", new byte[] { 1, 2, 3 }) };

        var result = await controller.Replace(123, dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Replace_ReturnsUnauthorized_OnUnauthorizedAccessException()
    {
        var controller = Create(out _, userId: null);
        var dto = new ReplacePropertyPhotoFormDto { File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }) };

        var result = await controller.Replace(123, dto, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Replace_ReturnsNotFound_OnPropertyPhotoNotFoundException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.ReplacePhotoAsync(
            It.IsAny<int>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PropertyPhotoNotFoundException(123));

        var dto = new ReplacePropertyPhotoFormDto { File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }) };

        var result = await controller.Replace(123, dto, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Replace_ReturnsBadRequest_OnArgumentException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.ReplacePhotoAsync(
            It.IsAny<int>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("superseded version cannot be replaced"));

        var dto = new ReplacePropertyPhotoFormDto { File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }) };

        var result = await controller.Replace(123, dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Replace_Returns500_OnInvalidOperationException()
    {
        // A configuration/operational error (e.g., missing Department/Module) must NOT be
        // reported as 404 - it should surface as 500.
        var controller = Create(out var service);
        service.Setup(s => s.ReplacePhotoAsync(
            It.IsAny<int>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No department with code PTIS or PROPERTY found"));

        var dto = new ReplacePropertyPhotoFormDto { File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }) };

        var result = await controller.Replace(123, dto, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    [Fact]
    public async Task Replace_ReturnsOk_OnValidReplace()
    {
        var controller = Create(out var service);
        service.Setup(s => s.ReplacePhotoAsync(
            123,
            It.IsAny<Stream>(),
            "a.jpg",
            "image/jpeg",
            3,
            null,
            42,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyPhotoUploadResponseDto());

        var dto = new ReplacePropertyPhotoFormDto { File = MakeFile("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }) };

        var result = await controller.Replace(123, dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenPropertyPhotoIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.Delete(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenPhotoDoesNotExist()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeletePhotoAsync(123, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.Delete(123, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenDeleted()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeletePhotoAsync(123, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Delete(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsUnauthorized_OnUnauthorizedAccessException()
    {
        var controller = Create(out _, userId: null);

        var result = await controller.Delete(123, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
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
