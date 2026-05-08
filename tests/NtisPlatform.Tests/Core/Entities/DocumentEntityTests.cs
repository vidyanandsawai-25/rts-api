using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Tests.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Comprehensive tests for DocumentEntity to achieve 100% coverage
/// </summary>
public class DocumentEntityTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_WithValidParameters_ReturnsNewEntity()
    {
        // Arrange & Act
        var entity = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "stored.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/stored.pdf",
            documentType: "Certificate");

        // Assert
        Assert.NotEqual(Guid.Empty, entity.DocumentGuid);
        Assert.Equal(1, entity.UploadedByUserId);
        Assert.Equal(1, entity.OwnerUserId); // Defaults to uploader
        Assert.Equal("stored.pdf", entity.FileName);
        Assert.Equal("test.pdf", entity.OriginalFileName);
        Assert.Equal(".pdf", entity.FileExtension);
        Assert.Equal("application/pdf", entity.MimeType);
        Assert.Equal(1024, entity.FileSizeBytes);
        Assert.Equal("/uploads/stored.pdf", entity.StoragePath);
        Assert.Equal("Certificate", entity.DocumentType);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Create_WithInvalidUploadedByUserId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 0,
                fileName: "test.pdf",
                originalFileName: "test.pdf",
                fileExtension: ".pdf",
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("Uploaded by user ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void Create_WithEmptyFileName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "",
                originalFileName: "test.pdf",
                fileExtension: ".pdf",
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("File name cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithEmptyOriginalFileName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "stored.pdf",
                originalFileName: "",
                fileExtension: ".pdf",
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("Original file name cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithEmptyFileExtension_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "test.pdf",
                originalFileName: "test.pdf",
                fileExtension: "",
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("File extension cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithEmptyMimeType_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "test.pdf",
                originalFileName: "test.pdf",
                fileExtension: ".pdf",
                mimeType: "",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("MIME type cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidFileSizeBytes_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "test.pdf",
                originalFileName: "test.pdf",
                fileExtension: ".pdf",
                mimeType: "application/pdf",
                fileSizeBytes: 0,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("File size must be greater than zero", exception.Message);
    }

    [Fact]
    public void Create_WithEmptyStoragePath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "test.pdf",
                originalFileName: "test.pdf",
                fileExtension: ".pdf",
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: ""));

        Assert.Contains("Storage path cannot be empty", exception.Message);
    }

    #endregion

    #region Internal Constructor Tests

    [Fact]
    public void InternalConstructor_CreatesEntityWithAllProperties()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();

        // Act
        var entity = new DocumentEntity(
            documentGuid: documentGuid,
            uploadedByUserId: 1,
            fileName: "stored.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf",
            storageProvider: "AZURE",
            ownerUserId: 2,
            documentType: "Certificate",
            uploadStatusCode: "ACTIVE",
            downloadCount: 5);

        // Assert
        Assert.Equal(documentGuid, entity.DocumentGuid);
        Assert.Equal(1, entity.UploadedByUserId);
        Assert.Equal(2, entity.OwnerUserId);
        Assert.Equal("stored.pdf", entity.FileName);
        Assert.Equal("test.pdf", entity.OriginalFileName);
        Assert.Equal(".pdf", entity.FileExtension);
        Assert.Equal("application/pdf", entity.MimeType);
        Assert.Equal(1024, entity.FileSizeBytes);
        Assert.Equal("/uploads/test.pdf", entity.StoragePath);
        Assert.Equal("AZURE", entity.StorageProvider);
        Assert.Equal("Certificate", entity.DocumentType);
        Assert.Equal("ACTIVE", entity.UploadStatusCode);
        Assert.Equal(5, entity.DownloadCount);
    }

    #endregion

    #region Domain Method Tests

    [Fact]
    public void RecordDownload_IncrementsDownloadCount()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity(downloadCount: 5);

        // Act
        entity.RecordDownload(userId: 1);

        // Assert
        Assert.Equal(6, entity.DownloadCount);
        Assert.NotNull(entity.LastAccessedDate);
        Assert.Equal(1, entity.LastAccessedBy);
    }

    [Fact]
    public void MarkForDeletion_MarksEntityForDeletion()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.MarkForDeletion(deletedByUserId: 1);

        // Assert
        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);
        Assert.False(entity.IsActive);
    }

    [Fact]
    public void SetDocumentType_UpdatesDocumentType()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.SetDocumentType("Invoice");

        // Assert
        Assert.Equal("Invoice", entity.DocumentType);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void DocumentEntity_InheritsFromBaseEntity()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Assert
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void DocumentEntity_ImplementsIHardDeletable()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Assert
        Assert.IsAssignableFrom<IHardDeletable>(entity);
    }

    #endregion
}
