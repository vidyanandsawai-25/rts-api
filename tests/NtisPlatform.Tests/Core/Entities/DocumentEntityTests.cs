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

    #region UpdateMetadata Tests

    [Fact]
    public void UpdateMetadata_WithValidParameters_UpdatesMetadata()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateMetadata("New Title", "New Description", "NewCategory");

        // Assert
        Assert.Equal("New Title", entity.DocumentTitle);
        Assert.Equal("New Description", entity.Description);
        Assert.Equal("NewCategory", entity.DocumentCategory);
    }

    [Fact]
    public void UpdateMetadata_WithNullTitle_UpdatesOnlyDescriptionAndCategory()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateMetadata(null, "New Description", "NewCategory");

        // Assert
        Assert.Null(entity.DocumentTitle);
        Assert.Equal("New Description", entity.Description);
        Assert.Equal("NewCategory", entity.DocumentCategory);
    }

    [Fact]
    public void UpdateMetadata_WithNullDescription_UpdatesOnlyTitleAndCategory()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateMetadata("New Title", null, "NewCategory");

        // Assert
        Assert.Equal("New Title", entity.DocumentTitle);
        Assert.Null(entity.Description);
        Assert.Equal("NewCategory", entity.DocumentCategory);
    }

    [Fact]
    public void UpdateMetadata_WithNullCategory_UpdatesOnlyTitleAndDescription()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateMetadata("New Title", "New Description", null);

        // Assert
        Assert.Equal("New Title", entity.DocumentTitle);
        Assert.Equal("New Description", entity.Description);
    }

    [Fact]
    public void UpdateMetadata_WithEmptyCategory_DoesNotUpdateCategory()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateMetadata("New Title", "New Description", "");

        // Assert
        Assert.Equal("New Title", entity.DocumentTitle);
        Assert.Equal("New Description", entity.Description);
    }

    [Fact]
    public void UpdateMetadata_WithTooLongTitle_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var longTitle = new string('A', 501);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            entity.UpdateMetadata(longTitle, "Description", "Category"));
        Assert.Contains("Document title cannot exceed 500 characters", exception.Message);
    }

    [Fact]
    public void UpdateMetadata_WithTooLongDescription_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var longDescription = new string('A', 2001);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            entity.UpdateMetadata("Title", longDescription, "Category"));
        Assert.Contains("Description cannot exceed 2000 characters", exception.Message);
    }

    [Fact]
    public void UpdateMetadata_WithWhitespaceTitle_UpdatesTitle()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateMetadata("   ", "Description", "Category");

        // Assert - whitespace is considered as null
        Assert.Null(entity.DocumentTitle);
    }

    [Fact]
    public void UpdateMetadata_WithWhitespaceDescription_UpdatesDescription()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateMetadata("Title", "   ", "Category");

        // Assert - whitespace is considered as null
        Assert.Null(entity.Description);
    }

    #endregion

    #region SetConfidentialityLevel Tests

    [Fact]
    public void SetConfidentialityLevel_WithPublic_SetsLevelAndIsPublic()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.SetConfidentialityLevel("PUBLIC");

        // Assert
        Assert.Equal("PUBLIC", entity.ConfidentialityLevel);
        Assert.True(entity.IsPublic);
    }

    [Fact]
    public void SetConfidentialityLevel_WithInternal_SetsLevelAndIsNotPublic()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.SetConfidentialityLevel("INTERNAL");

        // Assert
        Assert.Equal("INTERNAL", entity.ConfidentialityLevel);
        Assert.False(entity.IsPublic);
    }

    [Fact]
    public void SetConfidentialityLevel_WithConfidential_SetsLevelAndIsNotPublic()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.SetConfidentialityLevel("CONFIDENTIAL");

        // Assert
        Assert.Equal("CONFIDENTIAL", entity.ConfidentialityLevel);
        Assert.False(entity.IsPublic);
    }

    [Fact]
    public void SetConfidentialityLevel_WithRestricted_SetsLevelAndIsNotPublic()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.SetConfidentialityLevel("RESTRICTED");

        // Assert
        Assert.Equal("RESTRICTED", entity.ConfidentialityLevel);
        Assert.False(entity.IsPublic);
    }

    [Fact]
    public void SetConfidentialityLevel_WithSecret_SetsLevelAndIsNotPublic()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.SetConfidentialityLevel("SECRET");

        // Assert
        Assert.Equal("SECRET", entity.ConfidentialityLevel);
        Assert.False(entity.IsPublic);
    }

    [Fact]
    public void SetConfidentialityLevel_WithLowercasePublic_NormalizesToUppercase()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.SetConfidentialityLevel("public");

        // Assert
        Assert.Equal("PUBLIC", entity.ConfidentialityLevel);
        Assert.True(entity.IsPublic);
    }

    [Fact]
    public void SetConfidentialityLevel_WithInvalidLevel_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            entity.SetConfidentialityLevel("INVALID"));
        Assert.Contains("Invalid confidentiality level", exception.Message);
        Assert.Contains("PUBLIC, INTERNAL, CONFIDENTIAL, RESTRICTED, SECRET", exception.Message);
    }

    [Fact]
    public void SetConfidentialityLevel_WithNull_SetsLevelToNullAndIsPublicToFalse()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.SetConfidentialityLevel("PUBLIC");
        Assert.True(entity.IsPublic);

        // Act
        entity.SetConfidentialityLevel(null);

        // Assert
        Assert.Null(entity.ConfidentialityLevel);
        Assert.False(entity.IsPublic);
    }

    [Fact]
    public void SetConfidentialityLevel_WithWhitespace_SetsLevelToNullAndIsPublicToFalse()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.SetConfidentialityLevel("PUBLIC");
        Assert.True(entity.IsPublic);

        // Act
        entity.SetConfidentialityLevel("   ");

        // Assert
        Assert.Null(entity.ConfidentialityLevel);
        Assert.False(entity.IsPublic);
    }

    #endregion

    #region ValidateIntegrity Tests

    [Fact]
    public void ValidateIntegrity_WithValidDocument_ReturnsTrue()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        var result = entity.ValidateIntegrity();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateIntegrity_WhenMarkedForDeletion_ReturnsFalse()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.MarkForDeletion(1);

        // Act
        var result = entity.ValidateIntegrity();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateIntegrity_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        // Set expiry date using reflection
        var expiryProperty = typeof(DocumentEntity).GetProperty("ExpiryDate");
        expiryProperty!.SetValue(entity, DateTime.Now.AddDays(-1));

        // Act
        var result = entity.ValidateIntegrity();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateIntegrity_WhenInfected_ReturnsFalse()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.UpdateScanStatus("INFECTED");

        // Act
        var result = entity.ValidateIntegrity();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateIntegrity_WhenInactive_ReturnsFalse()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.IsActive = false;

        // Act
        var result = entity.ValidateIntegrity();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateIntegrity_WithMultipleFailures_ReturnsFalse()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.MarkForDeletion(1);

        // Act
        var result = entity.ValidateIntegrity();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region RecordDownload Tests

    [Fact]
    public void RecordDownload_WithInvalidUserId_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.RecordDownload(0));
        Assert.Contains("User ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void RecordDownload_WithNegativeUserId_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.RecordDownload(-1));
        Assert.Contains("User ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void RecordDownload_WhenMarkedForDeletion_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.MarkForDeletion(1);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.RecordDownload(1));
        Assert.Contains("Cannot download a document marked for deletion", exception.Message);
    }

    #endregion

    #region MarkForDeletion Tests

    [Fact]
    public void MarkForDeletion_WithInvalidUserId_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.MarkForDeletion(0));
        Assert.Contains("Deleted by user ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void MarkForDeletion_WhenAlreadyMarked_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.MarkForDeletion(1);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.MarkForDeletion(2));
        Assert.Contains("Document is already marked for deletion", exception.Message);
    }

    [Fact]
    public void MarkForDeletion_SetsDeletedBy()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.MarkForDeletion(5);

        // Assert
        Assert.Equal(5, entity.DeletedBy);
    }

    #endregion

    #region RestoreFromDeletion Tests

    [Fact]
    public void RestoreFromDeletion_WhenNotMarked_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.RestoreFromDeletion());
        Assert.Contains("Document is not marked for deletion", exception.Message);
    }

    [Fact]
    public void RestoreFromDeletion_RestoresEntity()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.MarkForDeletion(1);

        // Act
        entity.RestoreFromDeletion();

        // Assert
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.Null(entity.DeletedBy);
        Assert.Null(entity.DeletedDate);
        Assert.True(entity.IsActive);
    }

    #endregion

    #region UpdateScanStatus Tests

    [Fact]
    public void UpdateScanStatus_WithPending_UpdatesStatus()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateScanStatus("PENDING");

        // Assert
        Assert.Equal("PENDING", entity.ScanStatusCode);
        Assert.NotNull(entity.ScanCompletedDate);
    }

    [Fact]
    public void UpdateScanStatus_WithClean_UpdatesStatus()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateScanStatus("CLEAN");

        // Assert
        Assert.Equal("CLEAN", entity.ScanStatusCode);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void UpdateScanStatus_WithInfected_MarksInactive()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateScanStatus("INFECTED");

        // Assert
        Assert.Equal("INFECTED", entity.ScanStatusCode);
        Assert.False(entity.IsActive);
    }

    [Fact]
    public void UpdateScanStatus_WithError_UpdatesStatus()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateScanStatus("ERROR");

        // Assert
        Assert.Equal("ERROR", entity.ScanStatusCode);
    }

    [Fact]
    public void UpdateScanStatus_WithScanDetails_UpdatesDetails()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.UpdateScanStatus("CLEAN", "No threats detected");

        // Assert
        Assert.Equal("CLEAN", entity.ScanStatusCode);
        Assert.Equal("No threats detected", entity.ScanDetails);
    }

    [Fact]
    public void UpdateScanStatus_WithInvalidStatus_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.UpdateScanStatus("INVALID"));
        Assert.Contains("Invalid scan status", exception.Message);
    }

    #endregion

    #region SetDocumentType Tests

    [Fact]
    public void SetDocumentType_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetDocumentType(""));
        Assert.Contains("Document type cannot be empty", exception.Message);
    }

    [Fact]
    public void SetDocumentType_WithWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetDocumentType("   "));
        Assert.Contains("Document type cannot be empty", exception.Message);
    }

    [Fact]
    public void SetDocumentType_WithNull_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetDocumentType(null!));
        Assert.Contains("Document type cannot be empty", exception.Message);
    }

    #endregion

    #region TransferOwnership Tests

    [Fact]
    public void TransferOwnership_WithValidUserId_UpdatesOwner()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.TransferOwnership(10);

        // Assert
        Assert.Equal(10, entity.OwnerUserId);
    }

    [Fact]
    public void TransferOwnership_WithInvalidUserId_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.TransferOwnership(0));
        Assert.Contains("New owner user ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void TransferOwnership_WhenMarkedForDeletion_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.MarkForDeletion(1);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.TransferOwnership(2));
        Assert.Contains("Cannot transfer ownership of a document marked for deletion", exception.Message);
    }

    #endregion

    #region SetChecksum Tests

    [Fact]
    public void SetChecksum_WithValidChecksum_SetsChecksum()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var checksum = new string('a', 64);

        // Act
        entity.SetChecksum(checksum);

        // Assert
        Assert.Equal(checksum, entity.ChecksumSha256);
    }

    [Fact]
    public void SetChecksum_ConvertsToLowercase()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var checksum = new string('A', 64);

        // Act
        entity.SetChecksum(checksum);

        // Assert
        Assert.Equal(checksum.ToLowerInvariant(), entity.ChecksumSha256);
    }

    [Fact]
    public void SetChecksum_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetChecksum(""));
        Assert.Contains("Checksum cannot be empty", exception.Message);
    }

    [Fact]
    public void SetChecksum_WithNull_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetChecksum(null!));
        Assert.Contains("Checksum cannot be empty", exception.Message);
    }

    [Fact]
    public void SetChecksum_WithIncorrectLength_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetChecksum("short"));
        Assert.Contains("SHA256 checksum must be 64 characters", exception.Message);
    }

    #endregion

    #region SetExpiryDate Tests

    [Fact]
    public void SetExpiryDate_WithFutureDate_SetsDate()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var futureDate = DateTime.Now.AddDays(30);

        // Act
        entity.SetExpiryDate(futureDate);

        // Assert
        Assert.Equal(futureDate, entity.ExpiryDate);
    }

    [Fact]
    public void SetExpiryDate_WithPastDate_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var pastDate = DateTime.Now.AddDays(-1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetExpiryDate(pastDate));
        Assert.Contains("Expiry date must be in the future", exception.Message);
    }

    [Fact]
    public void SetExpiryDate_WithCurrentDate_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var currentDate = DateTime.Now;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetExpiryDate(currentDate));
        Assert.Contains("Expiry date must be in the future", exception.Message);
    }

    #endregion

    #region IsExpired Tests

    [Fact]
    public void IsExpired_WithNoExpiryDate_ReturnsFalse()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        var result = entity.IsExpired();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpired_WithFutureExpiryDate_ReturnsFalse()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        entity.SetExpiryDate(DateTime.Now.AddDays(30));

        // Act
        var result = entity.IsExpired();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpired_WithPastExpiryDate_ReturnsTrue()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        // Use reflection to set past expiry date
        var expiryProperty = typeof(DocumentEntity).GetProperty("ExpiryDate");
        expiryProperty!.SetValue(entity, DateTime.Now.AddDays(-1));

        // Act
        var result = entity.IsExpired();

        // Assert
        Assert.True(result);
    }

    #endregion

    #region SetAsNewVersionOf Tests

    [Fact]
    public void SetAsNewVersionOf_WithValidParameters_SetsVersion()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.SetAsNewVersionOf(5, 2);

        // Assert
        Assert.Equal(5, entity.ParentDocumentId);
        Assert.Equal(2, entity.Version);
        Assert.True(entity.IsLatestVersion);
    }

    [Fact]
    public void SetAsNewVersionOf_WithInvalidParentId_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetAsNewVersionOf(0, 2));
        Assert.Contains("Parent document ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void SetAsNewVersionOf_WithInvalidVersionNumber_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetAsNewVersionOf(5, 0));
        Assert.Contains("Version number must be greater than zero", exception.Message);
    }

    #endregion

    #region MarkAsOutdated Tests

    [Fact]
    public void MarkAsOutdated_WithValidDocumentId_MarksAsOutdated()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act
        entity.MarkAsOutdated(10);

        // Assert
        Assert.Equal(10, entity.ReplacedByDocumentId);
        Assert.False(entity.IsLatestVersion);
    }

    [Fact]
    public void MarkAsOutdated_WithInvalidDocumentId_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.MarkAsOutdated(0));
        Assert.Contains("Replaced by document ID must be greater than zero", exception.Message);
    }

    #endregion

    #region Property Setter Tests

    [Fact]
    public void FileName_SetToNull_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("FileName");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => property!.SetValue(entity, null));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
    }

    [Fact]
    public void OriginalFileName_SetToNull_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("OriginalFileName");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => property!.SetValue(entity, null));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
    }

    [Fact]
    public void FileExtension_SetToNull_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("FileExtension");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => property!.SetValue(entity, null));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
    }

    [Fact]
    public void MimeType_SetToNull_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("MimeType");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => property!.SetValue(entity, null));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
    }

    [Fact]
    public void StoragePath_SetToNull_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("StoragePath");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => property!.SetValue(entity, null));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
    }

    [Fact]
    public void DownloadCount_SetToNegative_ThrowsArgumentException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("DownloadCount");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => property!.SetValue(entity, -1));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    #endregion

    #region IHardDeletable Explicit Interface Implementation Tests

    [Fact]
    public void IHardDeletable_MarkedForDeletion_Setter_CanBeSetThroughInterface()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var hardDeletable = (IHardDeletable)entity;

        // Act - Set through explicit interface (covers line 422)
        hardDeletable.MarkedForDeletion = true;

        // Assert
        Assert.True(entity.MarkedForDeletion);
        Assert.True(hardDeletable.MarkedForDeletion);
    }

    [Fact]
    public void IHardDeletable_MarkedForDeletion_Getter_ReturnsValue()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var hardDeletable = (IHardDeletable)entity;
        hardDeletable.MarkedForDeletion = true;

        // Act - Get through explicit interface (covers line 421)
        var value = hardDeletable.MarkedForDeletion;

        // Assert
        Assert.True(value);
    }

    [Fact]
    public void IHardDeletable_MarkedForDeletionDate_Setter_CanBeSetThroughInterface()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var hardDeletable = (IHardDeletable)entity;
        var date = DateTime.Now;

        // Act - Set through explicit interface (covers line 428)
        hardDeletable.MarkedForDeletionDate = date;

        // Assert
        Assert.Equal(date, entity.MarkedForDeletionDate);
        Assert.Equal(date, hardDeletable.MarkedForDeletionDate);
    }

    [Fact]
    public void IHardDeletable_MarkedForDeletionDate_Getter_ReturnsValue()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var hardDeletable = (IHardDeletable)entity;
        var date = DateTime.Now;
        hardDeletable.MarkedForDeletionDate = date;

        // Act - Get through explicit interface (covers line 427)
        var value = hardDeletable.MarkedForDeletionDate;

        // Assert
        Assert.Equal(date, value);
    }

    [Fact]
    public void IHardDeletable_MarkedForDeletion_SetToFalse_UpdatesValue()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var hardDeletable = (IHardDeletable)entity;
        hardDeletable.MarkedForDeletion = true;

        // Act
        hardDeletable.MarkedForDeletion = false;

        // Assert
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void IHardDeletable_MarkedForDeletionDate_SetToNull_ClearsValue()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var hardDeletable = (IHardDeletable)entity;
        hardDeletable.MarkedForDeletionDate = DateTime.Now;

        // Act
        hardDeletable.MarkedForDeletionDate = null;

        // Assert
        Assert.Null(entity.MarkedForDeletionDate);
    }

    #endregion

    #region IHardDeletable Interface Tests

    [Fact]
    public void IHardDeletable_MarkedForDeletion_CanBeSetThroughInterface()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var hardDeletable = (IHardDeletable)entity;

        // Act
        hardDeletable.MarkedForDeletion = true;

        // Assert
        Assert.True(entity.MarkedForDeletion);
    }

    [Fact]
    public void IHardDeletable_MarkedForDeletionDate_CanBeSetThroughInterface()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var hardDeletable = (IHardDeletable)entity;
        var date = DateTime.Now;

        // Act
        hardDeletable.MarkedForDeletionDate = date;

        // Assert
        Assert.Equal(date, entity.MarkedForDeletionDate);
    }

    #endregion

    #region Create Factory Method Additional Tests

    [Fact]
    public void Create_WithNullFileName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: null!,
                originalFileName: "test.pdf",
                fileExtension: ".pdf",
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("File name cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithWhitespaceFileName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "   ",
                originalFileName: "test.pdf",
                fileExtension: ".pdf",
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("File name cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithNullOriginalFileName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "stored.pdf",
                originalFileName: null!,
                fileExtension: ".pdf",
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("Original file name cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithWhitespaceOriginalFileName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "stored.pdf",
                originalFileName: "   ",
                fileExtension: ".pdf",
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("Original file name cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithNullFileExtension_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "test.pdf",
                originalFileName: "test.pdf",
                fileExtension: null!,
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("File extension cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithWhitespaceFileExtension_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "test.pdf",
                originalFileName: "test.pdf",
                fileExtension: "   ",
                mimeType: "application/pdf",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("File extension cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithNullMimeType_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "test.pdf",
                originalFileName: "test.pdf",
                fileExtension: ".pdf",
                mimeType: null!,
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("MIME type cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithWhitespaceMimeType_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "test.pdf",
                originalFileName: "test.pdf",
                fileExtension: ".pdf",
                mimeType: "   ",
                fileSizeBytes: 1024,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("MIME type cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithNullStoragePath_ThrowsArgumentException()
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
                storagePath: null!));

        Assert.Contains("Storage path cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithWhitespaceStoragePath_ThrowsArgumentException()
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
                storagePath: "   "));

        Assert.Contains("Storage path cannot be empty", exception.Message);
    }

    [Fact]
    public void Create_WithNegativeFileSizeBytes_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentEntity.Create(
                uploadedByUserId: 1,
                fileName: "test.pdf",
                originalFileName: "test.pdf",
                fileExtension: ".pdf",
                mimeType: "application/pdf",
                fileSizeBytes: -1,
                storagePath: "/uploads/test.pdf"));

        Assert.Contains("File size must be greater than zero", exception.Message);
    }

    [Fact]
    public void Create_WithNullDocumentType_CreatesSuccessfully()
    {
        // Act
        var entity = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf",
            documentType: null);

        // Assert
        Assert.Null(entity.DocumentType);
    }

    [Fact]
    public void Create_WithEmptyDocumentType_CreatesSuccessfully()
    {
        // Act
        var entity = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf",
            documentType: "");

        // Assert
        Assert.Null(entity.DocumentType);
    }

    [Fact]
    public void Create_FileExtension_IsConvertedToLowerCase()
    {
        // Act
        var entity = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".PDF",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        // Assert
        Assert.Equal(".pdf", entity.FileExtension);
    }

    [Fact]
    public void Create_DefaultValues_AreSetCorrectly()
    {
        // Act
        var entity = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        // Assert
        Assert.Equal("FOLDER", entity.StorageProvider);
        Assert.Equal("ACTIVE", entity.UploadStatusCode);
        Assert.Equal(1, entity.Version);
        Assert.True(entity.IsLatestVersion);
        Assert.False(entity.IsPublic);
        Assert.True(entity.InheritPermissions);
        Assert.False(entity.IsEncrypted);
        Assert.Equal(0, entity.DownloadCount);
        Assert.True(entity.IsActive);
        Assert.False(entity.MarkedForDeletion);
    }

    #endregion

    #region InternalConstructor Additional Tests

    [Fact]
    public void InternalConstructor_WithNullOwnerUserId_DefaultsToUploadedBy()
    {
        // Act
        var entity = new DocumentEntity(
            documentGuid: Guid.NewGuid(),
            uploadedByUserId: 5,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf",
            ownerUserId: null);

        // Assert
        Assert.Equal(5, entity.OwnerUserId);
    }

    [Fact]
    public void InternalConstructor_WithNullStorageProvider_DefaultsToFOLDER()
    {
        // Act
        var entity = new DocumentEntity(
            documentGuid: Guid.NewGuid(),
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf",
            storageProvider: null);

        // Assert
        Assert.Equal("FOLDER", entity.StorageProvider);
    }

    [Fact]
    public void InternalConstructor_WithNullUploadStatusCode_DefaultsToACTIVE()
    {
        // Act
        var entity = new DocumentEntity(
            documentGuid: Guid.NewGuid(),
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf",
            uploadStatusCode: null);

        // Assert
        Assert.Equal("ACTIVE", entity.UploadStatusCode);
    }

    [Fact]
    public void InternalConstructor_FileExtension_IsConvertedToLowerCase()
    {
        // Act
        var entity = new DocumentEntity(
            documentGuid: Guid.NewGuid(),
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".PDF",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        // Assert
        Assert.Equal(".pdf", entity.FileExtension);
    }

    #endregion

    #region Property Setter Null Exception Tests

    [Fact]
    public void FileName_SetToNull_ViaReflection_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("FileName");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => property!.SetValue(entity, null));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Contains("FileName", exception.InnerException.Message);
    }

    [Fact]
    public void OriginalFileName_SetToNull_ViaReflection_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("OriginalFileName");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => property!.SetValue(entity, null));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Contains("OriginalFileName", exception.InnerException.Message);
    }

    [Fact]
    public void FileExtension_SetToNull_ViaReflection_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("FileExtension");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => property!.SetValue(entity, null));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Contains("FileExtension", exception.InnerException.Message);
    }

    [Fact]
    public void MimeType_SetToNull_ViaReflection_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("MimeType");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => property!.SetValue(entity, null));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Contains("MimeType", exception.InnerException.Message);
    }

    [Fact]
    public void StoragePath_SetToNull_ViaReflection_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("StoragePath");

        // Act & Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => property!.SetValue(entity, null));
        Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Contains("StoragePath", exception.InnerException.Message);
    }

    [Fact]
    public void FileExtension_SetterWithNonNullValue_ConvertsToLowerInvariant()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("FileExtension");

        // Act
        property!.SetValue(entity, ".PDF");

        // Assert
        Assert.Equal(".pdf", entity.FileExtension);
    }

    #endregion

    #region Protected Constructor Tests

    [Fact]
    public void ProtectedConstructor_CanBeCalledViaReflection()
    {
        // Arrange
        var constructorInfo = typeof(DocumentEntity).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);

        // Act
        var entity = constructorInfo?.Invoke(null) as DocumentEntity;

        // Assert
        Assert.NotNull(entity);
        Assert.NotNull(constructorInfo);
    }

    [Fact]
    public void Create_WithEmptyStringDocumentType_DoesNotSetDocumentType()
    {
        // Act
        var entity = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf",
            documentType: "");

        // Assert
        Assert.Null(entity.DocumentType);
    }

    [Fact]
    public void Create_WithWhitespaceDocumentType_DoesNotSetDocumentType()
    {
        // Act
        var entity = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf",
            documentType: "   ");

        // Assert
        Assert.Null(entity.DocumentType);
    }

    [Fact]
    public void InternalConstructor_WithZeroDownloadCount_SetsDownloadCountToZero()
    {
        // Act
        var entity = new DocumentEntity(
            documentGuid: Guid.NewGuid(),
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf",
            downloadCount: 0);

        // Assert
        Assert.Equal(0, entity.DownloadCount);
    }

    [Fact]
    public void FileExtension_Setter_ConvertsToLowerInvariant()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("FileExtension");

        // Act
        property!.SetValue(entity, ".PDF");

        // Assert
        Assert.Equal(".pdf", entity.FileExtension);
    }

    [Fact]
    public void MimeType_Setter_PreservesValue()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();
        var property = typeof(DocumentEntity).GetProperty("MimeType");

        // Act
        property!.SetValue(entity, "application/pdf");

        // Assert
        Assert.Equal("application/pdf", entity.MimeType);
    }

    #endregion

    #region Navigation Properties Tests

    [Fact]
    public void DocumentBindings_InitializesAsEmptyCollection()
    {
        // Act
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Assert - Covers line 468
        Assert.NotNull(entity.DocumentBindings);
        Assert.Empty(entity.DocumentBindings);
        Assert.IsAssignableFrom<ICollection<DocumentBindingEntity>>(entity.DocumentBindings);
    }

    [Fact]
    public void DocumentBindings_IsNotNull_OnCreation()
    {
        // Arrange & Act
        var entity = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        // Assert
        Assert.NotNull(entity.DocumentBindings);
        Assert.Empty(entity.DocumentBindings);
    }

    [Fact]
    public void DocumentBindings_CanCheckCount()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Act & Assert
        Assert.Equal(0, entity.DocumentBindings.Count);
    }

    [Fact]
    public void ParentDocument_DefaultsToNull()
    {
        // Act
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Assert
        Assert.Null(entity.ParentDocument);
    }

    [Fact]
    public void ReplacedByDocument_DefaultsToNull()
    {
        // Act
        var entity = EntityTestHelpers.CreateDocumentEntity();

        // Assert
        Assert.Null(entity.ReplacedByDocument);
    }

    #endregion
}
