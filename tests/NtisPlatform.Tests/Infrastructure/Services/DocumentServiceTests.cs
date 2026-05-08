using Moq;
using MockQueryable.Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Tests.Helpers;
using System.Linq.Expressions;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive tests for DocumentService to achieve 100% line and branch coverage
/// </summary>
public class DocumentServiceTests
{
    private readonly Mock<IRepository<DocumentEntity, int>> _mockDocumentRepository;
    private readonly Mock<IRepository<DocumentBindingEntity, int>> _mockBindingRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _mockDocumentRepository = new Mock<IRepository<DocumentEntity, int>>();
        _mockBindingRepository = new Mock<IRepository<DocumentBindingEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _service = new DocumentService(
            _mockDocumentRepository.Object,
            _mockBindingRepository.Object,
            _mockUnitOfWork.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        var service = new DocumentService(
            _mockDocumentRepository.Object,
            _mockBindingRepository.Object,
            _mockUnitOfWork.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region CreateDocumentAsync Tests

    [Fact]
    public async Task CreateDocumentAsync_WithValidParameters_CreatesDocument()
    {
        // Arrange
        _mockDocumentRepository.Setup(r => r.AddAsync(It.IsAny<DocumentEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentEntity, CancellationToken>((entity, ct) => entity.Id = 123); // Simulate ID assignment

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateDocumentAsync(
            uploadedByUserId: 1,
            ownerUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf",
            thumbnailPath: null,
            checksumSha256: null,
            documentType: "Certificate");

        // Assert
        Assert.NotEqual(Guid.Empty, result.DocumentGuid);
        Assert.True(result.DocumentId > 0);
        _mockDocumentRepository.Verify(r => r.AddAsync(It.IsAny<DocumentEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetDocumentByGuidAsync Tests

    [Fact]
    public async Task GetDocumentByGuidAsync_WithExistingDocument_ReturnsDocument()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();
        var document = EntityTestHelpers.CreateDocumentEntity(documentGuid: documentGuid);

        _mockDocumentRepository.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<DocumentEntity, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentEntity> { document });

        // Act
        var result = await _service.GetDocumentByGuidAsync(documentGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentGuid, result.DocumentGuid);
    }

    [Fact]
    public async Task GetDocumentByGuidAsync_WithNonExistingDocument_ReturnsNull()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();

        _mockDocumentRepository.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<DocumentEntity, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentEntity>());

        // Act
        var result = await _service.GetDocumentByGuidAsync(documentGuid);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetDocumentByIdAsync Tests

    [Fact]
    public async Task GetDocumentByIdAsync_WithExistingDocument_ReturnsDocument()
    {
        // Arrange
        var documentId = 1;
        var document = EntityTestHelpers.CreateDocumentEntity();

        _mockDocumentRepository.Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _service.GetDocumentByIdAsync(documentId);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetDocumentByIdAsync_WithNonExistingDocument_ReturnsNull()
    {
        // Arrange
        var documentId = 999;

        _mockDocumentRepository.Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentEntity?)null);

        // Act
        var result = await _service.GetDocumentByIdAsync(documentId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteDocumentAsync Tests

    [Fact]
    public async Task DeleteDocumentAsync_WithExistingDocument_MarksForDeletion()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();
        var document = EntityTestHelpers.CreateDocumentEntity(documentGuid: documentGuid);

        _mockDocumentRepository.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<DocumentEntity, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentEntity> { document });

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteDocumentAsync(documentGuid, deletedBy: 1);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithNonExistingDocument_ReturnsFalse()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();

        _mockDocumentRepository.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<DocumentEntity, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentEntity>());

        // Act
        var result = await _service.DeleteDocumentAsync(documentGuid, deletedBy: 1);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IncrementDownloadCountAsync Tests

    [Fact]
    public async Task IncrementDownloadCountAsync_WithExistingDocument_IncrementsCount()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();
        var document = EntityTestHelpers.CreateDocumentEntity(documentGuid: documentGuid);

        _mockDocumentRepository.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<DocumentEntity, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentEntity> { document });

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.IncrementDownloadCountAsync(documentGuid, userId: 1);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IncrementDownloadCountAsync_WithNonExistingDocument_DoesNothing()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();

        _mockDocumentRepository.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<DocumentEntity, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentEntity>());

        // Act
        await _service.IncrementDownloadCountAsync(documentGuid, userId: 1);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region CreateDocumentBindingAsync Tests

    [Fact]
    public async Task CreateDocumentBindingAsync_WithIntReference_CreatesBinding()
    {
        // Arrange
        _mockBindingRepository.Setup(r => r.AddAsync(It.IsAny<DocumentBindingEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentBindingEntity, CancellationToken>((entity, ct) => entity.Id = 456); // Simulate ID assignment

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateDocumentBindingAsync(
            documentId: 1,
            moduleCode: "TEST",
            referenceTableName: "TestTable",
            referenceTableId: 1,
            referenceTableIdGuid: null,
            bindingPurpose: "Test",
            isPrimaryDocument: false,
            authModuleCode: null,
            authReferenceId: null,
            createdBy: 1);

        // Assert
        Assert.True(result > 0);
        _mockBindingRepository.Verify(r => r.AddAsync(It.IsAny<DocumentBindingEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDocumentBindingAsync_WithGuidReference_CreatesBinding()
    {
        // Arrange
        _mockBindingRepository.Setup(r => r.AddAsync(It.IsAny<DocumentBindingEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentBindingEntity, CancellationToken>((entity, ct) => entity.Id = 789); // Simulate ID assignment

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateDocumentBindingAsync(
            documentId: 1,
            moduleCode: "TEST",
            referenceTableName: "TestTable",
            referenceTableId: null,
            referenceTableIdGuid: Guid.NewGuid(),
            bindingPurpose: "Test",
            isPrimaryDocument: false,
            authModuleCode: null,
            authReferenceId: null,
            createdBy: 1);

        // Assert
        Assert.True(result > 0);
        _mockBindingRepository.Verify(r => r.AddAsync(It.IsAny<DocumentBindingEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetDocumentsByReferenceAsync Tests

    [Fact]
    public async Task GetDocumentsByReferenceAsync_WithIntReference_ReturnsDocuments()
    {
        // Arrange
        var document = EntityTestHelpers.CreateDocumentEntity();
        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 123,
            bindingPurpose: "MainDocument");

        // Set the Document navigation property
        var bindingProperty = typeof(DocumentBindingEntity).GetProperty("Document");
        bindingProperty?.SetValue(binding, document);

        var bindings = new List<DocumentBindingEntity> { binding };
        var mockQueryable = bindings.BuildMockDbSet();

        _mockBindingRepository.Setup(r => r.GetQueryable())
            .Returns(mockQueryable.Object);

        // Act
        var result = await _service.GetDocumentsByReferenceAsync(
            referenceTableName: "PropertyCertificate",
            referenceTableId: 123,
            referenceTableIdGuid: null);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(document.DocumentGuid, result[0].DocumentGuid);
    }

    [Fact]
    public async Task GetDocumentsByReferenceAsync_WithGuidReference_ReturnsDocuments()
    {
        // Arrange
        var document = EntityTestHelpers.CreateDocumentEntity();
        var guid = Guid.NewGuid();
        var binding = DocumentBindingEntity.CreateWithGuidReference(
            documentId: document.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableIdGuid: guid,
            bindingPurpose: "MainDocument");

        // Set the Document navigation property
        var bindingProperty = typeof(DocumentBindingEntity).GetProperty("Document");
        bindingProperty?.SetValue(binding, document);

        var bindings = new List<DocumentBindingEntity> { binding };
        var mockQueryable = bindings.BuildMockDbSet();

        _mockBindingRepository.Setup(r => r.GetQueryable())
            .Returns(mockQueryable.Object);

        // Act
        var result = await _service.GetDocumentsByReferenceAsync(
            referenceTableName: "PropertyCertificate",
            referenceTableId: null,
            referenceTableIdGuid: guid);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(document.DocumentGuid, result[0].DocumentGuid);
    }

    [Fact]
    public async Task GetDocumentsByReferenceAsync_WithBothReferences_ThrowsArgumentException()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<XorValidationException>(() =>
            _service.GetDocumentsByReferenceAsync(
                referenceTableName: "PropertyCertificate",
                referenceTableId: 123,
                referenceTableIdGuid: guid));
    }

    [Fact]
    public async Task GetDocumentsByReferenceAsync_WithNeitherReference_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<XorValidationException>(() =>
            _service.GetDocumentsByReferenceAsync(
                referenceTableName: "PropertyCertificate",
                referenceTableId: null,
                referenceTableIdGuid: null));
    }

    [Fact]
    public async Task GetDocumentsByReferenceAsync_OrdersByPrimaryThenDisplayOrderThenCreatedDate()
    {
        // Arrange
        var document1 = EntityTestHelpers.CreateDocumentEntity(id: 1);
        var document2 = EntityTestHelpers.CreateDocumentEntity(id: 2);
        var document3 = EntityTestHelpers.CreateDocumentEntity(id: 3);

        // Create bindings with different priorities
        var primaryBinding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document1.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 123);
        primaryBinding.MarkAsPrimary();
        primaryBinding.CreatedDate = DateTime.Now.AddDays(-1);

        // Set the Document navigation property
        var binding1Property = typeof(DocumentBindingEntity).GetProperty("Document");
        binding1Property?.SetValue(primaryBinding, document1);

        var secondaryBinding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document2.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 123);
        secondaryBinding.SetDisplayOrder(1);
        secondaryBinding.CreatedDate = DateTime.Now.AddDays(-2);

        var binding2Property = typeof(DocumentBindingEntity).GetProperty("Document");
        binding2Property?.SetValue(secondaryBinding, document2);

        var tertiaryBinding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document3.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 123);
        tertiaryBinding.SetDisplayOrder(2);
        tertiaryBinding.CreatedDate = DateTime.Now.AddDays(-3);

        var binding3Property = typeof(DocumentBindingEntity).GetProperty("Document");
        binding3Property?.SetValue(tertiaryBinding, document3);

        var bindings = new List<DocumentBindingEntity> { secondaryBinding, tertiaryBinding, primaryBinding };
        var mockQueryable = bindings.BuildMockDbSet();

        _mockBindingRepository.Setup(r => r.GetQueryable())
            .Returns(mockQueryable.Object);

        // Act
        var result = await _service.GetDocumentsByReferenceAsync(
            referenceTableName: "PropertyCertificate",
            referenceTableId: 123,
            referenceTableIdGuid: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        // The service should order by IsPrimaryDocument DESC, DisplayOrder ASC, CreatedDate DESC
        // So primary should be first, then secondary (DisplayOrder 1), then tertiary (DisplayOrder 2)
        Assert.Equal(document1.DocumentGuid, result[0].DocumentGuid);
        Assert.Equal(document2.DocumentGuid, result[1].DocumentGuid);
        Assert.Equal(document3.DocumentGuid, result[2].DocumentGuid);
    }

    #endregion
}
