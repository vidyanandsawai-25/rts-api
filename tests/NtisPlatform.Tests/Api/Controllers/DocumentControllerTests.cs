using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class DocumentControllerTests
{
    private static DocumentController Create(
        out Mock<IDocumentApplicationService> docService,
        out Mock<IDocumentAuthorizationService> authService,
        bool isDevelopment = false,
        int? userId = 42)
    {
        docService = new Mock<IDocumentApplicationService>();
        authService = new Mock<IDocumentAuthorizationService>();
        var logger = new Mock<ILogger<DocumentController>>();
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(isDevelopment ? "Development" : "Production");

        var config = new Mock<IConfiguration>();
        var section = new Mock<IConfigurationSection>();
        section.Setup(s => s.Value).Returns((string?)null);
        config.Setup(c => c.GetSection(It.IsAny<string>())).Returns(section.Object);
        var fileHelper = new FileValidationHelper(config.Object);

        var controller = new DocumentController(docService.Object, authService.Object, logger.Object, env.Object, fileHelper);

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

    #region Upload

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileMissing()
    {
        var controller = Create(out _, out _);
        var dto = new DocumentUploadFormDto { File = null! };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileEmpty()
    {
        var controller = Create(out _, out _);
        var dto = new DocumentUploadFormDto { File = MakeFile("a.pdf", "application/pdf", Array.Empty<byte>()) };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileTypeInvalid()
    {
        var controller = Create(out _, out _);
        var dto = new DocumentUploadFormDto { File = MakeFile("a.exe", "application/x-msdownload", new byte[] { 1, 2, 3 }) };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsOk_OnValidFile()
    {
        var controller = Create(out var docService, out _);
        docService.Setup(s => s.UploadDocumentAsync(It.IsAny<Stream>(), "a.pdf", "application/pdf", 3, It.IsAny<DocumentUploadDto>(), 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentUploadResponseDto());
        var dto = new DocumentUploadFormDto
        {
            File = MakeFile("a.pdf", "application/pdf", new byte[] { 1, 2, 3 }),
            OwnerUserId = 1,
            DocumentType = "Other",
            IsPrimaryDocument = true,
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsUnauthorized_OnUnauthorizedAccessException()
    {
        var controller = Create(out var docService, out _, userId: null);
        var dto = new DocumentUploadFormDto
        {
            File = MakeFile("a.pdf", "application/pdf", new byte[] { 1, 2, 3 })
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_OnArgumentException()
    {
        var controller = Create(out var docService, out _);
        docService.Setup(s => s.UploadDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<DocumentUploadDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("invalid"));
        var dto = new DocumentUploadFormDto { File = MakeFile("a.pdf", "application/pdf", new byte[] { 1, 2 }) };

        var result = await controller.Upload(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_Returns500_OnGenericException_InProduction()
    {
        var controller = Create(out var docService, out _, isDevelopment: false);
        docService.Setup(s => s.UploadDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<DocumentUploadDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var dto = new DocumentUploadFormDto { File = MakeFile("a.pdf", "application/pdf", new byte[] { 1, 2 }) };

        var result = await controller.Upload(dto, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task Upload_Returns500_OnGenericException_InDevelopment()
    {
        var controller = Create(out var docService, out _, isDevelopment: true);
        docService.Setup(s => s.UploadDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<DocumentUploadDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var dto = new DocumentUploadFormDto { File = MakeFile("a.pdf", "application/pdf", new byte[] { 1, 2 }) };

        var result = await controller.Upload(dto, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    #endregion

    #region Get

    [Fact]
    public async Task Get_ReturnsForbid_WhenUnauthorized()
    {
        var controller = Create(out _, out var authService);
        authService.Setup(a => a.CanAccessDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var controller = Create(out var docService, out var authService);
        authService.Setup(a => a.CanAccessDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        docService.Setup(s => s.GetDocumentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DocumentDto?)null);

        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsOk_WhenDocumentFound()
    {
        var controller = Create(out var docService, out var authService);
        authService.Setup(a => a.CanAccessDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        docService.Setup(s => s.GetDocumentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DocumentDto());

        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region View

    [Fact]
    public async Task View_ReturnsForbid_WhenUnauthorized()
    {
        var controller = Create(out _, out var authService);
        authService.Setup(a => a.CanAccessDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.View(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task View_ReturnsNotFound_WhenStreamNull()
    {
        var controller = Create(out var docService, out var authService);
        authService.Setup(a => a.CanAccessDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        docService.Setup(s => s.ViewDocumentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream?)null, "", ""));

        var result = await controller.View(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task View_ReturnsFile_WhenStreamReturned()
    {
        var controller = Create(out var docService, out var authService);
        authService.Setup(a => a.CanAccessDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        docService.Setup(s => s.ViewDocumentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new MemoryStream(Encoding.ASCII.GetBytes("pdf")), "a.pdf", "application/pdf"));

        var result = await controller.View(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<FileStreamResult>(result);
    }

    #endregion

    #region Download

    [Fact]
    public async Task Download_ReturnsForbid_WhenUnauthorized()
    {
        var controller = Create(out _, out var authService);
        authService.Setup(a => a.CanAccessDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Download(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Download_ReturnsNotFound_WhenStreamNull()
    {
        var controller = Create(out var docService, out var authService);
        authService.Setup(a => a.CanAccessDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        docService.Setup(s => s.DownloadDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream?)null, "", ""));

        var result = await controller.Download(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Download_ReturnsFile_WhenStreamReturned()
    {
        var controller = Create(out var docService, out var authService);
        authService.Setup(a => a.CanAccessDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        docService.Setup(s => s.DownloadDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new MemoryStream(Encoding.ASCII.GetBytes("pdf")), "a.pdf", "application/pdf"));

        var result = await controller.Download(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<FileStreamResult>(result);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ReturnsForbid_WhenUnauthorized()
    {
        var controller = Create(out _, out var authService);
        authService.Setup(a => a.CanModifyDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenServiceReturnsFalse()
    {
        var controller = Create(out var docService, out var authService);
        authService.Setup(a => a.CanModifyDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        docService.Setup(s => s.DeleteDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenSuccessful()
    {
        var controller = Create(out var docService, out var authService);
        authService.Setup(a => a.CanModifyDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        docService.Setup(s => s.DeleteDocumentAsync(It.IsAny<Guid>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region UpdateBindingReference

    [Fact]
    public async Task UpdateBindingReference_ReturnsForbid_WhenUnauthorized()
    {
        var controller = Create(out _, out var authService);
        authService.Setup(a => a.CanAccessDocumentBindingAsync(5, 42, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.UpdateBindingReference(5, 9, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateBindingReference_ReturnsOk_WhenAuthorized()
    {
        var controller = Create(out var docService, out var authService);
        authService.Setup(a => a.CanAccessDocumentBindingAsync(5, 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        docService.Setup(s => s.UpdateDocumentBindingReferenceAsync(5, 9, 42, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await controller.UpdateBindingReference(5, 9, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        docService.Verify(s => s.UpdateDocumentBindingReferenceAsync(5, 9, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
