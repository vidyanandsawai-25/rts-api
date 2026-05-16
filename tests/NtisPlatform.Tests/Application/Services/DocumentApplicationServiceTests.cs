using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Tests.Helpers;
using System.Text;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for DocumentApplicationService to achieve 100% line and branch coverage
/// </summary>
public class DocumentApplicationServiceTests
{
    private readonly Mock<IDocumentService> _mockDocumentService;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<DocumentApplicationService>> _mockLogger;
    private readonly DocumentApplicationService _service;

    public DocumentApplicationServiceTests()
    {
        _mockDocumentService = new Mock<IDocumentService>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        // Setup transaction methods to return completed tasks
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var configData = new Dictionary<string, string>
        {
            {"FileStorage:BufferSizeBytes", "81920"},
            {"FileStorage:MaxFileSizeBytes", "104857600"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        _mockLogger = new Mock<ILogger<DocumentApplicationService>>();
        _service = new DocumentApplicationService(
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
        var service = new DocumentApplicationService(
            _mockDocumentService.Object,
            _mockFileStorageService.Object,
            _mockUnitOfWork.Object,
            _configuration,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithMissingConfiguration_UsesDefaultValues()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();

        // Act
        var service = new DocumentApplicationService(
            _mockDocumentService.Object,
            _mockFileStorageService.Object,
            _mockUnitOfWork.Object,
            emptyConfig,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region UploadDocumentAsync Tests - Guard Clause Validation

    [Fact]
    public async Task UploadDocumentAsync_WithNullStream_ThrowsArgumentException()
    {
        // Arrange
        var uploadDto = new DocumentUploadDto
        {
            OwnerUserId = 1,
            ModuleCode = "PROPERTY",
            ReferenceTableName = "PropertyCertificate",
            ReferenceTableId = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadDocumentAsync(null!, "test.pdf", "application/pdf", 1024, uploadDto, 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_WithNullFileName_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream();
        var uploadDto = new DocumentUploadDto { OwnerUserId = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadDocumentAsync(stream, null!, "application/pdf", 1024, uploadDto, 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_WithEmptyFileName_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream();
        var uploadDto = new DocumentUploadDto { OwnerUserId = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadDocumentAsync(stream, "", "application/pdf", 1024, uploadDto, 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_WithFileNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream();
        var uploadDto = new DocumentUploadDto { OwnerUserId = 1 };
        var longFileName = new string('a', 256) + ".pdf";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadDocumentAsync(stream, longFileName, "application/pdf", 1024, uploadDto, 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_WithNullMimeType_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream();
        var uploadDto = new DocumentUploadDto { OwnerUserId = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadDocumentAsync(stream, "test.pdf", null!, 1024, uploadDto, 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_WithZeroFileSize_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream();
        var uploadDto = new DocumentUploadDto { OwnerUserId = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadDocumentAsync(stream, "test.pdf", "application/pdf", 0, uploadDto, 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_WithNegativeFileSize_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream();
        var uploadDto = new DocumentUploadDto { OwnerUserId = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadDocumentAsync(stream, "test.pdf", "application/pdf", -1, uploadDto, 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_WithFileSizeExceedingLimit_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream();
        var uploadDto = new DocumentUploadDto { OwnerUserId = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.UploadDocumentAsync(stream, "test.pdf", "application/pdf", 200000000, uploadDto, 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_WithNullUploadDto_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.UploadDocumentAsync(stream, "test.pdf", "application/pdf", 1024, null!, 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_WithZeroUploadedBy_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream();
        var uploadDto = new DocumentUploadDto { OwnerUserId = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadDocumentAsync(stream, "test.pdf", "application/pdf", 1024, uploadDto, 0));
    }

    #endregion

    #region UploadDocumentAsync Tests - Successful Upload

    [Fact]
    public async Task UploadDocumentAsync_WithValidParameters_UploadsSuccessfully()
    {
        // Arrange
        var content = "Test file content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var uploadDto = new DocumentUploadDto
        {
            OwnerUserId = 1,
            ModuleCode = "PROPERTY",
            ReferenceTableName = "PropertyCertificate",
            ReferenceTableId = 1
        };

        _mockFileStorageService.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/test.pdf");

        _mockDocumentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((123, Guid.NewGuid()));

        _mockDocumentService.Setup(s => s.CreateDocumentBindingAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(456);

        // Act
        var result = await _service.UploadDocumentAsync(
            stream, "test.pdf", "application/pdf", content.Length, uploadDto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.DocumentId);
        Assert.Equal(456, result.DocumentBindingId);
        Assert.Equal("test.pdf", result.FileName);
        Assert.Equal("/uploads/test.pdf", result.StoragePath);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_WithoutBindingInfo_DoesNotCreateBinding()
    {
        // Arrange
        var content = "Test file content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var uploadDto = new DocumentUploadDto
        {
            OwnerUserId = 1
        };

        _mockFileStorageService.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/test.pdf");

        _mockDocumentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((123, Guid.NewGuid()));

        // Act
        var result = await _service.UploadDocumentAsync(
            stream, "test.pdf", "application/pdf", content.Length, uploadDto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.DocumentBindingId);
        _mockDocumentService.Verify(s => s.CreateDocumentBindingAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadDocumentAsync_WithInvalidModuleCode_DoesNotCreateBinding()
    {
        // Arrange
        var content = "Test file content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var uploadDto = new DocumentUploadDto
        {
            OwnerUserId = 1,
            ModuleCode = "invalid",
            ReferenceTableName = "PropertyCertificate",
            ReferenceTableId = 1
        };

        _mockFileStorageService.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/test.pdf");

        _mockDocumentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((123, Guid.NewGuid()));

        // Act
        var result = await _service.UploadDocumentAsync(
            stream, "test.pdf", "application/pdf", content.Length, uploadDto, 1);

        // Assert
        Assert.Null(result.DocumentBindingId);
        _mockDocumentService.Verify(s => s.CreateDocumentBindingAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadDocumentAsync_WithInvalidReferenceTableName_DoesNotCreateBinding()
    {
        // Arrange
        var content = "Test file content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var uploadDto = new DocumentUploadDto
        {
            OwnerUserId = 1,
            ModuleCode = "PROPERTY",
            ReferenceTableName = "1InvalidTable",
            ReferenceTableId = 1
        };

        _mockFileStorageService.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/test.pdf");

        _mockDocumentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((123, Guid.NewGuid()));

        // Act
        var result = await _service.UploadDocumentAsync(
            stream, "test.pdf", "application/pdf", content.Length, uploadDto, 1);

        // Assert
        Assert.Null(result.DocumentBindingId);
    }

    [Fact]
    public async Task UploadDocumentAsync_WithBothIntAndGuidId_DoesNotCreateBinding()
    {
        // Arrange
        var content = "Test file content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var uploadDto = new DocumentUploadDto
        {
            OwnerUserId = 1,
            ModuleCode = "PROPERTY",
            ReferenceTableName = "PropertyCertificate",
            ReferenceTableId = 1,
            ReferenceTableIdGuid = Guid.NewGuid()
        };

        _mockFileStorageService.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/test.pdf");

        _mockDocumentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((123, Guid.NewGuid()));

        // Act
        var result = await _service.UploadDocumentAsync(
            stream, "test.pdf", "application/pdf", content.Length, uploadDto, 1);

        // Assert
        Assert.Null(result.DocumentBindingId);
    }

    [Fact]
    public async Task UploadDocumentAsync_WithGuidIdOnly_CreatesBinding()
    {
        // Arrange
        var content = "Test file content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var uploadDto = new DocumentUploadDto
        {
            OwnerUserId = 1,
            ModuleCode = "PROPERTY",
            ReferenceTableName = "PropertyCertificate",
            ReferenceTableIdGuid = Guid.NewGuid()
        };

        _mockFileStorageService.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/test.pdf");

        _mockDocumentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((123, Guid.NewGuid()));

        _mockDocumentService.Setup(s => s.CreateDocumentBindingAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(456);

        // Act
        var result = await _service.UploadDocumentAsync(
            stream, "test.pdf", "application/pdf", content.Length, uploadDto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(456, result.DocumentBindingId);
    }

    #endregion

    #region UploadDocumentAsync Tests - Error Handling

    [Fact]
    public async Task UploadDocumentAsync_WhenDatabaseFails_RollsBackAndDeletesFile()
    {
        // Arrange
        var content = "Test file content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var uploadDto = new DocumentUploadDto { OwnerUserId = 1 };

        _mockFileStorageService.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/test.pdf");

        _mockDocumentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        _mockFileStorageService.Setup(s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.UploadDocumentAsync(stream, "test.pdf", "application/pdf", content.Length, uploadDto, 1));

        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.Verify(s => s.DeleteFileAsync("/uploads/test.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenDatabaseFailsAndFileDeleteFails_LogsWarning()
    {
        // Arrange
        var content = "Test file content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var uploadDto = new DocumentUploadDto { OwnerUserId = 1 };

        _mockFileStorageService.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/test.pdf");

        _mockDocumentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        _mockFileStorageService.Setup(s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Delete failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.UploadDocumentAsync(stream, "test.pdf", "application/pdf", content.Length, uploadDto, 1));

        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetDocumentAsync Tests

    [Fact]
    public async Task GetDocumentAsync_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetDocumentAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetDocumentAsync_WithExistingDocument_ReturnsDto()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();
        var document = EntityTestHelpers.CreateDocumentEntity(documentGuid: documentGuid);

        _mockDocumentService.Setup(s => s.GetDocumentByGuidAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _service.GetDocumentAsync(documentGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentGuid, result.DocumentGuid);
        Assert.Equal(document.FileName, result.FileName);
    }

    [Fact]
    public async Task GetDocumentAsync_WithNonExistentDocument_ReturnsNull()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();

        _mockDocumentService.Setup(s => s.GetDocumentByGuidAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NtisPlatform.Core.Entities.DocumentEntity?)null);

        // Act
        var result = await _service.GetDocumentAsync(documentGuid);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DownloadDocumentAsync Tests

    [Fact]
    public async Task DownloadDocumentAsync_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.DownloadDocumentAsync(Guid.Empty, 1));
    }

    [Fact]
    public async Task DownloadDocumentAsync_WithZeroUserId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.DownloadDocumentAsync(Guid.NewGuid(), 0));
    }

    [Fact]
    public async Task DownloadDocumentAsync_WithExistingDocument_ReturnsStreamAndIncrementsCounter()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();
        var document = EntityTestHelpers.CreateDocumentEntity(documentGuid: documentGuid);
        var fileStream = new MemoryStream();

        _mockDocumentService.Setup(s => s.GetDocumentByGuidAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockFileStorageService.Setup(s => s.ReadFileAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        _mockDocumentService.Setup(s => s.IncrementDownloadCountAsync(documentGuid, 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DownloadDocumentAsync(documentGuid, 1);

        // Assert
        Assert.NotNull(result.FileStream);
        Assert.Equal(document.OriginalFileName, result.FileName);
        Assert.Equal(document.MimeType, result.MimeType);
        _mockDocumentService.Verify(s => s.IncrementDownloadCountAsync(documentGuid, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadDocumentAsync_WithNonExistentDocument_ReturnsEmpty()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();

        _mockDocumentService.Setup(s => s.GetDocumentByGuidAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NtisPlatform.Core.Entities.DocumentEntity?)null);

        // Act
        var result = await _service.DownloadDocumentAsync(documentGuid, 1);

        // Assert
        Assert.Null(result.FileStream);
        Assert.Equal(string.Empty, result.FileName);
        Assert.Equal(string.Empty, result.MimeType);
    }

    [Fact]
    public async Task DownloadDocumentAsync_WhenFileNotInStorage_ReturnsNullStream()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();
        var document = EntityTestHelpers.CreateDocumentEntity(documentGuid: documentGuid);

        _mockDocumentService.Setup(s => s.GetDocumentByGuidAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockFileStorageService.Setup(s => s.ReadFileAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await _service.DownloadDocumentAsync(documentGuid, 1);

        // Assert
        Assert.Null(result.FileStream);
        _mockDocumentService.Verify(s => s.IncrementDownloadCountAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ViewDocumentAsync Tests

    [Fact]
    public async Task ViewDocumentAsync_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ViewDocumentAsync(Guid.Empty));
    }

    [Fact]
    public async Task ViewDocumentAsync_WithExistingDocument_ReturnsStream()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();
        var document = EntityTestHelpers.CreateDocumentEntity(documentGuid: documentGuid);
        var fileStream = new MemoryStream();

        _mockDocumentService.Setup(s => s.GetDocumentByGuidAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockFileStorageService.Setup(s => s.ReadFileAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        // Act
        var result = await _service.ViewDocumentAsync(documentGuid);

        // Assert
        Assert.NotNull(result.FileStream);
        Assert.Equal(document.OriginalFileName, result.FileName);
        Assert.Equal(document.MimeType, result.MimeType);
    }

    [Fact]
    public async Task ViewDocumentAsync_WithNonExistentDocument_ReturnsEmpty()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();

        _mockDocumentService.Setup(s => s.GetDocumentByGuidAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NtisPlatform.Core.Entities.DocumentEntity?)null);

        // Act
        var result = await _service.ViewDocumentAsync(documentGuid);

        // Assert
        Assert.Null(result.FileStream);
        Assert.Equal(string.Empty, result.FileName);
        Assert.Equal(string.Empty, result.MimeType);
    }

    [Fact]
    public async Task ViewDocumentAsync_WhenFileNotInStorage_ReturnsNullStream()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();
        var document = EntityTestHelpers.CreateDocumentEntity(documentGuid: documentGuid);

        _mockDocumentService.Setup(s => s.GetDocumentByGuidAsync(documentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockFileStorageService.Setup(s => s.ReadFileAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await _service.ViewDocumentAsync(documentGuid);

        // Assert
        Assert.Null(result.FileStream);
    }

    #endregion

    #region DeleteDocumentAsync Tests

    [Fact]
    public async Task DeleteDocumentAsync_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.DeleteDocumentAsync(Guid.Empty, 1));
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithZeroDeletedBy_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.DeleteDocumentAsync(Guid.NewGuid(), 0));
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithExistingDocument_DeletesSuccessfully()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();

        _mockDocumentService.Setup(s => s.DeleteDocumentAsync(documentGuid, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteDocumentAsync(documentGuid, 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithNonExistentDocument_ReturnsFalse()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();

        _mockDocumentService.Setup(s => s.DeleteDocumentAsync(documentGuid, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteDocumentAsync(documentGuid, 1);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region UpdateDocumentBindingReferenceAsync Tests

    [Fact]
    public async Task UpdateDocumentBindingReferenceAsync_WithZeroBindingId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateDocumentBindingReferenceAsync(0, 1, 1));
    }

    [Fact]
    public async Task UpdateDocumentBindingReferenceAsync_WithZeroReferenceId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateDocumentBindingReferenceAsync(1, 0, 1));
    }

    [Fact]
    public async Task UpdateDocumentBindingReferenceAsync_WithZeroUpdatedBy_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateDocumentBindingReferenceAsync(1, 1, 0));
    }

    [Fact]
    public async Task UpdateDocumentBindingReferenceAsync_WithValidParameters_UpdatesSuccessfully()
    {
        // Arrange
        _mockDocumentService.Setup(s => s.UpdateDocumentBindingReferenceAsync(1, 2, 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateDocumentBindingReferenceAsync(1, 2, 1);

        // Assert
        _mockDocumentService.Verify(s => s.UpdateDocumentBindingReferenceAsync(1, 2, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
