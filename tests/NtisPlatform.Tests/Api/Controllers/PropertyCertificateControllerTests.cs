using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Security.Claims;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Comprehensive tests for PropertyCertificateController to achieve 100% code coverage
/// </summary>
public class PropertyCertificateControllerTests
{
    private readonly Mock<IPropertyCertificateApplicationService> _mockService;
    private readonly Mock<ILogger<PropertyCertificateController>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly FileValidationHelper _fileValidationHelper;
    private readonly PropertyCertificateController _controller;

    public PropertyCertificateControllerTests()
    {
        _mockService = new Mock<IPropertyCertificateApplicationService>();
        _mockLogger = new Mock<ILogger<PropertyCertificateController>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _fileValidationHelper = CreateFileValidationHelper();
        _controller = new PropertyCertificateController(
            _mockService.Object,
            _mockLogger.Object,
            _mockEnvironment.Object,
            _fileValidationHelper);
        SetupControllerContext();
    }

    private static FileValidationHelper CreateFileValidationHelper()
    {
        var mockConfiguration = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns((string?)null);
        mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockSection.Object);
        return new FileValidationHelper(mockConfiguration.Object);
    }

    private void SetupControllerContext(int userId = 1)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetupControllerContextWithoutUserId()
    {
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetupControllerContextWithInvalidUserId()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-number")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region Upload Tests

    [Fact]
    public async Task Upload_WithValidData_ReturnsOk()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Certificate content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1,
            CertificateNo = "CERT-001",
            IssueDate = DateTime.Now
        };

        var expectedResponse = new PropertyCertificateUploadResponseDto
        {
            PropertyCertificateId = 1,
            DocumentGuid = Guid.NewGuid(),
            DocumentId = 1,
            DocumentBindingId = 1,
            PropertyId = 100,
            CertificateTypeId = 1,
            CertificateNo = "CERT-001",
            IssueDate = formDto.IssueDate,
            FileName = "certificate.pdf",
            FileSizeBytes = content.Length,
            StoragePath = "/certificates/certificate.pdf"
        };

        _mockService.Setup(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyCertificateUploadResponseDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("PropertyCertificate uploaded successfully", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(expectedResponse.PropertyCertificateId, response.Items.PropertyCertificateId);
        Assert.Equal(expectedResponse.PropertyId, response.Items.PropertyId);
    }

    [Fact]
    public async Task Upload_WithNullFile_ReturnsBadRequest()
    {
        // Arrange
        var formDto = new PropertyCertificateUploadFormDto
        {
            File = null!,
            PropertyId = 100,
            CertificateTypeId = 1
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
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
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
    public async Task Upload_WithInvalidFileType_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.FileName).Returns("malicious.exe");
        fileMock.Setup(f => f.ContentType).Returns("application/x-msdownload");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Invalid file type", response.Message);
    }

    [Fact]
    public async Task Upload_WithValidPdfFile_Succeeds()
    {
        // Arrange - Test PDF is allowed
        var fileMock = new Mock<IFormFile>();
        var content = "Test PDF content"u8.ToArray();
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(content));
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        var expectedResponse = new PropertyCertificateUploadResponseDto();
        _mockService.Setup(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Upload_WithValidImageFile_Succeeds()
    {
        // Arrange - Test image is allowed
        var fileMock = new Mock<IFormFile>();
        var content = "Test image content"u8.ToArray();
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(content));
        fileMock.Setup(f => f.FileName).Returns("photo.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.Length).Returns(content.Length);

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        var expectedResponse = new PropertyCertificateUploadResponseDto();
        _mockService.Setup(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Upload_WithZeroPropertyId_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 0,
            CertificateTypeId = 1
        };

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("PropertyId is required", response.Message);
    }

    [Fact]
    public async Task Upload_WithNegativePropertyId_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = -1,
            CertificateTypeId = 1
        };

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("PropertyId is required", response.Message);
    }

    [Fact]
    public async Task Upload_WithZeroCertificateTypeId_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 0
        };

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("CertificateTypeId is required", response.Message);
    }

    [Fact]
    public async Task Upload_WithNegativeCertificateTypeId_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = -5
        };

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("CertificateTypeId is required", response.Message);
    }

    [Fact]
    public async Task Upload_WhenServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        _mockService.Setup(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid certificate type"));

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Invalid certificate type", response.Message);
    }

    [Fact]
    public async Task Upload_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        _mockService.Setup(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(statusCodeResult.Value);
        Assert.False(response.Success);
        Assert.Equal("An error occurred", response.Message);
    }

    [Fact]
    public async Task Upload_WithNullCertificateNo_ReturnsOk()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Certificate content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1,
            CertificateNo = null,
            IssueDate = null
        };

        var expectedResponse = new PropertyCertificateUploadResponseDto
        {
            PropertyCertificateId = 1,
            DocumentGuid = Guid.NewGuid(),
            PropertyId = 100,
            CertificateTypeId = 1
        };

        _mockService.Setup(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            100,
            1,
            null,
            null,
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyCertificateUploadResponseDto>>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Upload_VerifiesCorrectParametersPassed()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Certificate content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var issueDate = new DateTime(2024, 1, 15);
        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 200,
            CertificateTypeId = 5,
            CertificateNo = "CERT-XYZ",
            IssueDate = issueDate
        };

        var expectedResponse = new PropertyCertificateUploadResponseDto();

        _mockService.Setup(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(),
            "certificate.pdf",
            "application/pdf",
            content.Length,
            200,
            5,
            "CERT-XYZ",
            issueDate,
            1,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(),
            "certificate.pdf",
            "application/pdf",
            content.Length,
            200,
            5,
            "CERT-XYZ",
            issueDate,
            1,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetByPropertyId Tests

    [Fact]
    public async Task GetByPropertyId_WithExistingCertificates_ReturnsOk()
    {
        // Arrange
        var propertyId = 100;
        var expectedCertificates = new List<PropertyCertificateDto>
        {
            new PropertyCertificateDto
            {
                Id = 1,
                PropertyId = propertyId,
                CertificateTypeId = 1,
                CertificateTypeName = "Ownership Certificate",
                CertificateNo = "CERT-001",
                IssueDate = DateTime.Now.AddDays(-30),
                DocumentBindingId = 1,
                DocumentGuid = Guid.NewGuid(),
                IsEnabled = true
            },
            new PropertyCertificateDto
            {
                Id = 2,
                PropertyId = propertyId,
                CertificateTypeId = 2,
                CertificateTypeName = "Tax Certificate",
                CertificateNo = "CERT-002",
                IssueDate = DateTime.Now.AddDays(-15),
                DocumentBindingId = 2,
                DocumentGuid = Guid.NewGuid(),
                IsEnabled = true
            }
        };

        _mockService.Setup(s => s.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCertificates);

        // Act
        var result = await _controller.GetByPropertyId(propertyId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<PropertyCertificateDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(2, response.Items.Count);
        Assert.All(response.Items, cert => Assert.Equal(propertyId, cert.PropertyId));
    }

    [Fact]
    public async Task GetByPropertyId_WithNoCertificates_ReturnsEmptyList()
    {
        // Arrange
        var propertyId = 999;
        var expectedCertificates = new List<PropertyCertificateDto>();

        _mockService.Setup(s => s.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCertificates);

        // Act
        var result = await _controller.GetByPropertyId(propertyId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<PropertyCertificateDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Empty(response.Items);
    }

    [Fact]
    public async Task GetByPropertyId_VerifiesServiceCalledWithCorrectPropertyId()
    {
        // Arrange
        var propertyId = 500;
        _mockService.Setup(s => s.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateDto>());

        // Act
        await _controller.GetByPropertyId(propertyId, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByPropertyId_WithZeroPropertyId_ReturnsEmptyList()
    {
        // Arrange
        var propertyId = 0;
        _mockService.Setup(s => s.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateDto>());

        // Act
        var result = await _controller.GetByPropertyId(propertyId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<PropertyCertificateDto>>>(okResult.Value);
        Assert.True(response.Success);
    }

    #endregion

    #region GetUserId Tests

    [Fact]
    public async Task Upload_WithNoUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContextWithoutUserId();

        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Valid user identification is required", response.Message);
    }

    [Fact]
    public async Task Upload_WithInvalidUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContextWithInvalidUserId();

        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Upload_WithZeroUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContext(userId: 0);

        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Upload_WithNegativeUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContext(userId: -1);

        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        // Act
        var result = await _controller.Upload(formDto, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        var controller = new PropertyCertificateController(
            _mockService.Object,
            _mockLogger.Object,
            _mockEnvironment.Object,
            _fileValidationHelper);

        // Assert
        Assert.NotNull(controller);
    }

    #endregion

    #region PropertyCertificateUploadFormDto Tests

    [Fact]
    public void PropertyCertificateUploadFormDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PropertyCertificateUploadFormDto();

        // Assert
        Assert.Null(dto.File);
        Assert.Equal(0, dto.PropertyId);
        Assert.Equal(0, dto.CertificateTypeId);
        Assert.Null(dto.CertificateNo);
        Assert.Null(dto.IssueDate);
    }

    [Fact]
    public void PropertyCertificateUploadFormDto_CanSetAllProperties()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var issueDate = DateTime.Now;

        // Act
        var dto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 123,
            CertificateTypeId = 5,
            CertificateNo = "CERT-TEST",
            IssueDate = issueDate
        };

        // Assert
        Assert.Equal(fileMock.Object, dto.File);
        Assert.Equal(123, dto.PropertyId);
        Assert.Equal(5, dto.CertificateTypeId);
        Assert.Equal("CERT-TEST", dto.CertificateNo);
        Assert.Equal(issueDate, dto.IssueDate);
    }

    #endregion

    #region Integration-like Tests

    [Fact]
    public async Task Upload_ThenGetByPropertyId_WorksTogether()
    {
        // Arrange
        var propertyId = 100;
        var fileMock = new Mock<IFormFile>();
        var content = "Certificate content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = propertyId,
            CertificateTypeId = 1,
            CertificateNo = "CERT-001"
        };

        var uploadResponse = new PropertyCertificateUploadResponseDto
        {
            PropertyCertificateId = 1,
            DocumentGuid = Guid.NewGuid(),
            PropertyId = propertyId
        };

        var certificates = new List<PropertyCertificateDto>
        {
            new PropertyCertificateDto
            {
                Id = 1,
                PropertyId = propertyId,
                CertificateNo = "CERT-001"
            }
        };

        _mockService.Setup(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            propertyId,
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResponse);

        _mockService.Setup(s => s.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificates);

        // Act - Upload
        var uploadResult = await _controller.Upload(formDto, CancellationToken.None);
        var uploadOkResult = Assert.IsType<OkObjectResult>(uploadResult);
        var uploadApiResponse = Assert.IsType<ApiResponse<PropertyCertificateUploadResponseDto>>(uploadOkResult.Value);
        Assert.True(uploadApiResponse.Success);

        // Act - Get
        var getResult = await _controller.GetByPropertyId(propertyId, CancellationToken.None);
        var getOkResult = Assert.IsType<OkObjectResult>(getResult);
        var getApiResponse = Assert.IsType<ApiResponse<List<PropertyCertificateDto>>>(getOkResult.Value);

        // Assert
        Assert.True(getApiResponse.Success);
        Assert.Single(getApiResponse.Items!);
        Assert.Equal("CERT-001", getApiResponse.Items![0].CertificateNo);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task Upload_WhenArgumentExceptionThrown_LogsWarning()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        var expectedException = new ArgumentException("Validation failed");
        _mockService.Setup(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        await _controller.Upload(formDto, CancellationToken.None);

        // Assert - Verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Upload_WhenExceptionThrown_LogsError()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "Test content"u8.ToArray();
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("certificate.pdf");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var formDto = new PropertyCertificateUploadFormDto
        {
            File = fileMock.Object,
            PropertyId = 100,
            CertificateTypeId = 1
        };

        var expectedException = new Exception("Database error");
        _mockService.Setup(s => s.UploadWithDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        await _controller.Upload(formDto, CancellationToken.None);

        // Assert - Verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
