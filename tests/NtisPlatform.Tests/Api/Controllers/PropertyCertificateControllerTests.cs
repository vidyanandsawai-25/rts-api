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

        var controller = new PropertyCertificateController(service.Object, logger.Object, env.Object);

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

        var result = await controller.GetCertificateTypesWithStatus(0, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetCertificateTypesWithStatus_ReturnsOk_WhenPropertyIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetCertificateTypesWithStatusAsync(123, It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<PropertyCertificateWithStatusDto>());

        var result = await controller.GetCertificateTypesWithStatus(123, null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCertificateTypesWithStatus_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetCertificateTypesWithStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.GetCertificateTypesWithStatus(123, null, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetCertificateTypesWithStatus_PassesPropertyDetailsIdThrough_WhenProvided()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetCertificateTypesWithStatusAsync(123, It.IsAny<CancellationToken>(), 1702274))
            .ReturnsAsync(new List<PropertyCertificateWithStatusDto>());

        var result = await controller.GetCertificateTypesWithStatus(123, 1702274, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetCertificateTypesWithStatusAsync(123, It.IsAny<CancellationToken>(), 1702274), Times.Once);
    }

    #endregion

    #region GetSocietyWingCertificateTypesWithStatus

    [Fact]
    public async Task GetSocietyWingCertificateTypesWithStatus_ReturnsBadRequest_WhenBothParametersNull()
    {
        var controller = Create(out _);

        var result = await controller.GetSocietyWingCertificateTypesWithStatus(null, null, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task GetSocietyWingCertificateTypesWithStatus_ReturnsOk_WhenSocietyDetailIdProvided()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetSocietyOrWingCertificateTypesWithStatusAsync(101, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateWithStatusDto>());

        var result = await controller.GetSocietyWingCertificateTypesWithStatus(101, null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetSocietyOrWingCertificateTypesWithStatusAsync(101, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSocietyWingCertificateTypesWithStatus_ReturnsOk_WhenWingDetailIdProvided()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetSocietyOrWingCertificateTypesWithStatusAsync(null, 202, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateWithStatusDto>());

        var result = await controller.GetSocietyWingCertificateTypesWithStatus(null, 202, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetSocietyOrWingCertificateTypesWithStatusAsync(null, 202, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSocietyWingCertificateTypesWithStatus_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetSocietyOrWingCertificateTypesWithStatusAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.GetSocietyWingCertificateTypesWithStatus(101, null, CancellationToken.None);

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
            .ThrowsAsync(new Exception("error"));

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

    #region New Endpoints: GetByPropertyId, ReplaceDocument, DeleteByDocumentId

    [Fact]
    public async Task GetByPropertyId_ReturnsOk_WithListOfCertificates()
    {
        var controller = Create(out var service);
        var certificates = new List<PropertyCertificateDto>
        {
            new() { Id = 1, PropertyId = 100, CertificateTypeId = 2, CertificateNo = "CERT-100" }
        };

        service.Setup(s => s.GetByPropertyIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificates);

        var result = await controller.GetByPropertyId(100, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<List<PropertyCertificateDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Single(apiResponse.Items!);
        Assert.Equal("CERT-100", apiResponse.Items![0].CertificateNo);
    }

    [Fact]
    public async Task ReplaceDocument_ReturnsBadRequest_WhenFileNullOrEmpty()
    {
        var controller = Create(out _);

        var result = await controller.ReplaceDocument(1, null!, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("File is required", apiResponse.Message);
    }

    [Fact]
    public async Task ReplaceDocument_ReturnsOk_WhenFileValid()
    {
        var controller = Create(out var service);
        var file = MakeFile("test.pdf", "application/pdf", new byte[] { 1, 2, 3 });

        var uploadResponse = new PropertyCertificateUploadResponseDto
        {
            PropertyCertificateId = 1,
            DocumentId = 10,
            FileName = "test.pdf"
        };

        service.Setup(s => s.ReplaceDocumentAsync(
            1,
            It.IsAny<Stream>(),
            "test.pdf",
            "application/pdf",
            3,
            42,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResponse);

        var result = await controller.ReplaceDocument(1, file, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyCertificateUploadResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Document replaced successfully", apiResponse.Message);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(10, apiResponse.Items!.DocumentId);
    }

    [Fact]
    public async Task DeleteByDocumentId_ReturnsBadRequest_WhenDocumentIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.DeleteByDocumentId(0, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("A valid DocumentId is required", apiResponse.Message);
    }

    [Fact]
    public async Task DeleteByDocumentId_ReturnsNotFound_WhenServiceReturnsFalse()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteByDocumentIdAsync(55, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.DeleteByDocumentId(55, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Active PropertyCertificate document was not found", apiResponse.Message);
    }

    [Fact]
    public async Task DeleteByDocumentId_ReturnsOk_WhenDeleteSucceeds()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteByDocumentIdAsync(55, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.DeleteByDocumentId(55, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("PropertyCertificate document deleted successfully", apiResponse.Message);
    }

    #endregion
}

