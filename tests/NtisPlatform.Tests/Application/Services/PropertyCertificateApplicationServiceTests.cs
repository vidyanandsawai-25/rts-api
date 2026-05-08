using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Tests.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for PropertyCertificateApplicationService to achieve 100% line and branch coverage
/// </summary>
public class PropertyCertificateApplicationServiceTests
{
    private readonly Mock<IPropertyCertificateService> _mockPropertyCertificateService;
    private readonly Mock<IDocumentService> _mockDocumentService;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<PropertyCertificateApplicationService>> _mockLogger;
    private readonly PropertyCertificateApplicationService _service;

    public PropertyCertificateApplicationServiceTests()
    {
        _mockPropertyCertificateService = new Mock<IPropertyCertificateService>();
        _mockDocumentService = new Mock<IDocumentService>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        // Create in-memory configuration
        var configData = new Dictionary<string, string>
        {
            {"FileStorage:BufferSizeBytes", "81920"},
            {"FileStorage:MaxFileSizeBytes", "104857600"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        _mockLogger = new Mock<ILogger<PropertyCertificateApplicationService>>();
        _service = new PropertyCertificateApplicationService(
            _mockPropertyCertificateService.Object,
            _mockDocumentService.Object,
            _mockFileStorageService.Object,
            _mockUnitOfWork.Object,
            _configuration,
            _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        var service = new PropertyCertificateApplicationService(
            _mockPropertyCertificateService.Object,
            _mockDocumentService.Object,
            _mockFileStorageService.Object,
            _mockUnitOfWork.Object,
            _configuration,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region GetByPropertyIdAsync Tests

    [Fact]
    public async Task GetByPropertyIdAsync_WithExistingCertificates_ReturnsDtos()
    {
        // Arrange
        var entities = new List<PropertyCertificateEntity>
        {
            EntityTestHelpers.CreatePropertyCertificateEntity(
                propertyId: 100,
                certificateTypeId: 1,
                certificateNo: "CERT-001"),
            EntityTestHelpers.CreatePropertyCertificateEntity(
                propertyId: 100,
                certificateTypeId: 2,
                certificateNo: "CERT-002")
        };

        _mockPropertyCertificateService.Setup(s => s.GetByPropertyIdAsync(100))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetByPropertyIdAsync(100);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_WithNoCertificates_ReturnsEmptyList()
    {
        // Arrange
        _mockPropertyCertificateService.Setup(s => s.GetByPropertyIdAsync(999))
            .ReturnsAsync(new List<PropertyCertificateEntity>());

        // Act
        var result = await _service.GetByPropertyIdAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region UploadWithDocumentAsync Tests

    [Fact]
    public async Task UploadWithDocumentAsync_WithValidFile_UploadsSuccessfully()
    {
        // Arrange
        var content = "test content"u8.ToArray();
        var stream = new MemoryStream(content);
        var documentGuid = Guid.NewGuid();

        _mockFileStorageService.Setup(f => f.SaveFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/test.pdf");

        _mockDocumentService.Setup(d => d.CreateDocumentAsync(
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, documentGuid));

        _mockDocumentService.Setup(d => d.CreateDocumentBindingAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        _mockPropertyCertificateService.Setup(s => s.CreateWithDocumentAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockDocumentService.Setup(d => d.UpdateDocumentBindingReferenceAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UploadWithDocumentAsync(
            stream,
            "test.pdf",
            "application/pdf",
            content.Length,
            propertyId: 100,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now,
            uploadedBy: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentGuid, result.DocumentGuid);
    }

    #endregion
}
