using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Interfaces;
using System.Security.Claims;
using Xunit;
namespace NtisPlatform.Tests.Api.Controllers;
/// <summary>
/// Comprehensive tests for DocumentController to achieve 100% line coverage
/// Focuses on Upload and GetUserId methods
/// </summary>
public class DocumentControllerComprehensiveTests
{
    private readonly Mock<IDocumentApplicationService> _documentServiceMock;
    private readonly Mock<IDocumentAuthorizationService> _authServiceMock;
    private readonly Mock<ILogger<DocumentController>> _loggerMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly FileValidationHelper _fileValidationHelper;
    private readonly DocumentController _controller;
    public DocumentControllerComprehensiveTests()
    {
        _documentServiceMock = new Mock<IDocumentApplicationService>();
        _authServiceMock = new Mock<IDocumentAuthorizationService>();
        _loggerMock = new Mock<ILogger<DocumentController>>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        // Create a real configuration using InMemory provider
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FileValidation:AllowedMimeTypes:0"] = "application/pdf",
            ["FileValidation:AllowedMimeTypes:1"] = "image/jpeg",
            ["FileValidation:AllowedMimeTypes:2"] = "image/png",
            ["FileValidation:AllowedExtensions:0"] = ".pdf",
            ["FileValidation:AllowedExtensions:1"] = ".jpg",
            ["FileValidation:AllowedExtensions:2"] = ".jpeg",
            ["FileValidation:AllowedExtensions:3"] = ".png"
        });
        var configuration = configurationBuilder.Build();

        _fileValidationHelper = new FileValidationHelper(configuration);
        _controller = new DocumentController(
            _documentServiceMock.Object,
            _authServiceMock.Object,
            _loggerMock.Object,
            _environmentMock.Object,
            _fileValidationHelper);
    }
    #region GetUserId Tests
    [Fact]
    public async Task Upload_WithValidFile_CallsGetUserId()
    {
        // Arrange
        SetupUserClaim("123");
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = CreateFormDto(file);
        _documentServiceMock.Setup(x => x.UploadDocumentAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DocumentUploadDto>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentUploadResponseDto());
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        Assert.IsType<OkObjectResult>(result);
        _documentServiceMock.Verify(x => x.UploadDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<DocumentUploadDto>(),
            123, // Verify userId is extracted correctly
            It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task Upload_WithInvalidUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange - null claim
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = CreateFormDto(file);
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Valid user identification is required", response.Message);
    }
    [Fact]
    public async Task Upload_WithNonNumericUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange - non-numeric claim
        SetupUserClaim("abc");
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = CreateFormDto(file);
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Valid user identification is required", response.Message);
    }
    [Fact]
    public async Task Upload_WithZeroUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange - zero user id
        SetupUserClaim("0");
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = CreateFormDto(file);
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
    }
    [Fact]
    public async Task Upload_WithNegativeUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange - negative user id
        SetupUserClaim("-1");
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = CreateFormDto(file);
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
    }
    #endregion
    #region Upload Method Tests
    [Fact]
    public async Task Upload_WithNullFile_ReturnsBadRequest()
    {
        // Arrange
        SetupUserClaim("123");
        var formDto = new DocumentUploadFormDto
        {
            File = null!
        };
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("File is required", response.Message);
    }
    [Fact]
    public async Task Upload_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        SetupUserClaim("123");
        var file = CreateMockFile("test.pdf", "application/pdf", 0);
        var formDto = CreateFormDto(file);
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("File is required", response.Message);
    }
    [Fact]
    public async Task Upload_WithInvalidFileType_ReturnsBadRequest()
    {
        // Arrange
        SetupUserClaim("123");
        var file = CreateMockFile("test.exe", "application/x-msdownload", 1024);
        var formDto = CreateFormDto(file);
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Invalid file type", response.Message);
        Assert.Contains("Allowed extensions", response.Message);
    }
    [Fact]
    public async Task Upload_WithValidFile_ReturnsOk()
    {
        // Arrange
        SetupUserClaim("123");
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = CreateFormDto(file);
        var expectedResponse = new DocumentUploadResponseDto
        {
            DocumentId = 1,
            DocumentGuid = Guid.NewGuid(),
            FileName = "test.pdf"
        };
        _documentServiceMock.Setup(x => x.UploadDocumentAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DocumentUploadDto>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DocumentUploadResponseDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Document uploaded successfully", response.Message);
        Assert.NotNull(response.Items);
    }
    [Fact]
    public async Task Upload_WhenServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        SetupUserClaim("123");
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = CreateFormDto(file);
        _documentServiceMock.Setup(x => x.UploadDocumentAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DocumentUploadDto>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid argument"));
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Invalid argument", response.Message);
        Assert.NotNull(response.CorrelationId);
    }
    [Fact]
    public async Task Upload_WhenServiceThrowsUnauthorizedException_ReturnsUnauthorized()
    {
        // Arrange
        SetupUserClaim("123");
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = CreateFormDto(file);
        _documentServiceMock.Setup(x => x.UploadDocumentAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DocumentUploadDto>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Unauthorized"));
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.CorrelationId);
    }
    [Fact]
    public async Task Upload_WhenServiceThrowsGeneralException_InDevelopment_ReturnsDetailedError()
    {
        // Arrange
        SetupUserClaim("123");
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = CreateFormDto(file);
        _environmentMock.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        _documentServiceMock.Setup(x => x.UploadDocumentAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DocumentUploadDto>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Internal error"));
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(statusCodeResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Internal error", response.Message);
        Assert.NotNull(response.CorrelationId);
    }
    [Fact]
    public async Task Upload_WhenServiceThrowsGeneralException_InProduction_ReturnsGenericError()
    {
        // Arrange
        SetupUserClaim("123");
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = CreateFormDto(file);
        _environmentMock.Setup(x => x.EnvironmentName).Returns(Environments.Production);
        _documentServiceMock.Setup(x => x.UploadDocumentAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DocumentUploadDto>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Internal error"));
        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(statusCodeResult.Value);
        Assert.False(response.Success);
        Assert.Equal("An error occurred", response.Message);
        Assert.NotNull(response.CorrelationId);
    }
    [Fact]
    public async Task Upload_PassesCorrectParametersToService()
    {
        // Arrange
        SetupUserClaim("123");
        var file = CreateMockFile("test.pdf", "application/pdf", 1024);
        var formDto = new DocumentUploadFormDto
        {
            File = file,
            OwnerUserId = 456,
            DocumentType = "Certificate",
            ModuleCode = "PROPERTY",
            ReferenceTableName = "PropertyCertificate",
            ReferenceTableId = 789,
            BindingPurpose = "MainCertificate",
            IsPrimaryDocument = true,
            AuthModuleCode = "AUTH",
            AuthReferenceId = 999
        };
        _documentServiceMock.Setup(x => x.UploadDocumentAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<DocumentUploadDto>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentUploadResponseDto());
        // Act
        await _controller.Upload(formDto, CancellationToken.None);
        // Assert
        _documentServiceMock.Verify(x => x.UploadDocumentAsync(
            It.IsAny<Stream>(),
            "test.pdf",
            "application/pdf",
            1024,
            It.Is<DocumentUploadDto>(dto =>
                dto.OwnerUserId == 456 &&
                dto.DocumentType == "Certificate" &&
                dto.ModuleCode == "PROPERTY" &&
                dto.ReferenceTableName == "PropertyCertificate" &&
                dto.ReferenceTableId == 789 &&
                dto.BindingPurpose == "MainCertificate" &&
                dto.IsPrimaryDocument == true &&
                dto.AuthModuleCode == "AUTH" &&
                dto.AuthReferenceId == 999),
            123,
            It.IsAny<CancellationToken>()), Times.Once);
    }
    #endregion
    #region Helper Methods
    private void SetupUserClaim(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }
    private IFormFile CreateMockFile(string fileName, string contentType, long length)
    {
        var fileMock = new Mock<IFormFile>();
        var content = new byte[length];
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.ContentType).Returns(contentType);
        fileMock.Setup(f => f.Length).Returns(length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        return fileMock.Object;
    }
    private DocumentUploadFormDto CreateFormDto(IFormFile file)
    {
        return new DocumentUploadFormDto
        {
            File = file,
            ModuleCode = "PROPERTY",
            ReferenceTableName = "PropertyCertificate"
        };
    }
    #endregion

    #region Get Method Tests

    [Fact]
    public async Task Get_WithAuthorizedUser_ReturnsOk()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();
        var expectedDto = new DocumentDto { DocumentGuid = documentGuid, FileName = "test.pdf" };

        _authServiceMock.Setup(x => x.CanAccessDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _documentServiceMock.Setup(x => x.GetDocumentAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.Get(documentGuid, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DocumentDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
    }

    [Fact]
    public async Task Get_WithUnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();

        _authServiceMock.Setup(x => x.CanAccessDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Get(documentGuid, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Get_WithNonExistingDocument_ReturnsNotFound()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();

        _authServiceMock.Setup(x => x.CanAccessDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _documentServiceMock.Setup(x => x.GetDocumentAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentDto?)null);

        // Act
        var result = await _controller.Get(documentGuid, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Document not found", response.Message);
    }

    #endregion

    #region View Method Tests

    [Fact]
    public async Task View_WithAuthorizedUser_ReturnsFile()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();
        var fileStream = new MemoryStream(new byte[100]);

        _authServiceMock.Setup(x => x.CanAccessDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _documentServiceMock.Setup(x => x.ViewDocumentAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((fileStream, "test.pdf", "application/pdf"));

        // Act
        var result = await _controller.View(documentGuid, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
    }

    [Fact]
    public async Task View_WithUnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();

        _authServiceMock.Setup(x => x.CanAccessDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.View(documentGuid, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task View_WithNonExistingDocument_ReturnsNotFound()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();

        _authServiceMock.Setup(x => x.CanAccessDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _documentServiceMock.Setup(x => x.ViewDocumentAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null, null)!);

        // Act
        var result = await _controller.View(documentGuid, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region Download Method Tests

    [Fact]
    public async Task Download_WithAuthorizedUser_ReturnsFile()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();
        var fileStream = new MemoryStream(new byte[100]);

        _authServiceMock.Setup(x => x.CanAccessDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _documentServiceMock.Setup(x => x.DownloadDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((fileStream, "test.pdf", "application/pdf"));

        // Act
        var result = await _controller.Download(documentGuid, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("test.pdf", fileResult.FileDownloadName);
        Assert.True(fileResult.EnableRangeProcessing);
    }

    [Fact]
    public async Task Download_WithUnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();

        _authServiceMock.Setup(x => x.CanAccessDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Download(documentGuid, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Download_WithNonExistingDocument_ReturnsNotFound()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();

        _authServiceMock.Setup(x => x.CanAccessDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _documentServiceMock.Setup(x => x.DownloadDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null, null)!);

        // Act
        var result = await _controller.Download(documentGuid, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region Delete Method Tests

    [Fact]
    public async Task Delete_WithAuthorizedUser_ReturnsOk()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();

        _authServiceMock.Setup(x => x.CanModifyDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _documentServiceMock.Setup(x => x.DeleteDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(documentGuid, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Document deleted", response.Message);
    }

    [Fact]
    public async Task Delete_WithUnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();

        _authServiceMock.Setup(x => x.CanModifyDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(documentGuid, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_WithNonExistingDocument_ReturnsNotFound()
    {
        // Arrange
        SetupUserClaim("123");
        var documentGuid = Guid.NewGuid();

        _authServiceMock.Setup(x => x.CanModifyDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _documentServiceMock.Setup(x => x.DeleteDocumentAsync(documentGuid, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(documentGuid, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Document not found", response.Message);
    }

    #endregion

    #region UpdateBindingReference Method Tests

    [Fact]
    public async Task UpdateBindingReference_WithAuthorizedUser_ReturnsOk()
    {
        // Arrange
        SetupUserClaim("123");
        var documentBindingId = 1;
        var referenceTableId = 456;

        _authServiceMock.Setup(x => x.CanAccessDocumentBindingAsync(documentBindingId, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _documentServiceMock.Setup(x => x.UpdateDocumentBindingReferenceAsync(documentBindingId, referenceTableId, 123, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateBindingReference(documentBindingId, referenceTableId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Binding updated", response.Message);
    }

    [Fact]
    public async Task UpdateBindingReference_WithUnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        SetupUserClaim("123");
        var documentBindingId = 1;
        var referenceTableId = 456;

        _authServiceMock.Setup(x => x.CanAccessDocumentBindingAsync(documentBindingId, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateBindingReference(documentBindingId, referenceTableId, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    #endregion
}
