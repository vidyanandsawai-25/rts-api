using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Core.Constants;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Document;

/// <summary>
/// Comprehensive tests for Document DTOs to achieve 100% line and branch coverage
/// </summary>
public class DocumentDtoTests
{
    #region DocumentDto Tests

    [Fact]
    public void DocumentDto_Properties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new DocumentDto
        {
            Id = 1,
            DocumentGuid = Guid.NewGuid(),
            UploadedBy = 10,
            OwnerUserId = 20,
            FileName = "test.pdf",
            OriginalFileName = "original.pdf",
            FileExtension = ".pdf",
            MimeType = "application/pdf",
            FileSizeBytes = 1024,
            StorageProvider = "Local",
            StoragePath = "/uploads/test.pdf",
            DocumentType = "Certificate",
            UploadStatusCode = "ACTIVE",
            DownloadCount = 5,
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.NotEqual(Guid.Empty, dto.DocumentGuid);
        Assert.Equal(10, dto.UploadedBy);
        Assert.Equal(20, dto.OwnerUserId);
        Assert.Equal("test.pdf", dto.FileName);
        Assert.Equal("original.pdf", dto.OriginalFileName);
        Assert.Equal(".pdf", dto.FileExtension);
        Assert.Equal("application/pdf", dto.MimeType);
        Assert.Equal(1024, dto.FileSizeBytes);
        Assert.Equal("Local", dto.StorageProvider);
        Assert.Equal("/uploads/test.pdf", dto.StoragePath);
        Assert.Equal("Certificate", dto.DocumentType);
        Assert.Equal("ACTIVE", dto.UploadStatusCode);
        Assert.Equal(5, dto.DownloadCount);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void DocumentDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new DocumentDto();

        // Assert
        Assert.Equal(0, dto.Id);
        Assert.Equal(Guid.Empty, dto.DocumentGuid);
        Assert.Equal(0, dto.UploadedBy);
        Assert.Null(dto.FileName);
        Assert.Equal(0, dto.FileSizeBytes);
        Assert.Null(dto.StoragePath);
        Assert.False(dto.IsActive);
    }

    #endregion

    #region DocumentUploadDto Tests

    [Fact]
    public void DocumentUploadDto_Properties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new DocumentUploadDto
        {
            OwnerUserId = 100,
            ModuleCode = "PROPERTY",
            ReferenceTableName = "PropertyCertificate",
            ReferenceTableId = 1,
            ReferenceTableIdGuid = Guid.NewGuid(),
            BindingPurpose = "MainDocument",
            IsPrimaryDocument = true,
            AuthModuleCode = "AUTH_MOD",
            AuthReferenceId = 50,
            DocumentType = "Certificate"
        };

        // Assert
        Assert.Equal(100, dto.OwnerUserId);
        Assert.Equal("PROPERTY", dto.ModuleCode);
        Assert.Equal("PropertyCertificate", dto.ReferenceTableName);
        Assert.Equal(1, dto.ReferenceTableId);
        Assert.NotEqual(Guid.Empty, dto.ReferenceTableIdGuid);
        Assert.Equal("MainDocument", dto.BindingPurpose);
        Assert.True(dto.IsPrimaryDocument);
        Assert.Equal("AUTH_MOD", dto.AuthModuleCode);
        Assert.Equal(50, dto.AuthReferenceId);
        Assert.Equal("Certificate", dto.DocumentType);
    }

    [Fact]
    public void DocumentUploadDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new DocumentUploadDto();

        // Assert
        Assert.Null(dto.OwnerUserId);
        Assert.Null(dto.ModuleCode);
        Assert.Null(dto.ReferenceTableName);
        Assert.Null(dto.ReferenceTableId);
        Assert.Null(dto.ReferenceTableIdGuid);
        Assert.Null(dto.BindingPurpose);
        Assert.False(dto.IsPrimaryDocument);
        Assert.Null(dto.AuthModuleCode);
        Assert.Null(dto.AuthReferenceId);
        Assert.Null(dto.DocumentType);
    }

    [Fact]
    public void DocumentUploadDto_WithIntReferenceId_SetsCorrectly()
    {
        // Arrange & Act
        var dto = new DocumentUploadDto
        {
            ReferenceTableId = 123,
            ReferenceTableIdGuid = null
        };

        // Assert
        Assert.Equal(123, dto.ReferenceTableId);
        Assert.Null(dto.ReferenceTableIdGuid);
    }

    [Fact]
    public void DocumentUploadDto_WithGuidReferenceId_SetsCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var dto = new DocumentUploadDto
        {
            ReferenceTableId = null,
            ReferenceTableIdGuid = guid
        };

        // Assert
        Assert.Null(dto.ReferenceTableId);
        Assert.Equal(guid, dto.ReferenceTableIdGuid);
    }

    #endregion

    #region DocumentUploadResponseDto Tests

    [Fact]
    public void DocumentUploadResponseDto_Properties_GetSet_WorksCorrectly()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();

        // Act
        var dto = new DocumentUploadResponseDto
        {
            DocumentGuid = documentGuid,
            DocumentId = 1,
            DocumentBindingId = 2,
            FileName = "uploaded.pdf",
            FileSizeBytes = 2048,
            StoragePath = "/uploads/uploaded.pdf"
        };

        // Assert
        Assert.Equal(documentGuid, dto.DocumentGuid);
        Assert.Equal(1, dto.DocumentId);
        Assert.Equal(2, dto.DocumentBindingId);
        Assert.Equal("uploaded.pdf", dto.FileName);
        Assert.Equal(2048, dto.FileSizeBytes);
        Assert.Equal("/uploads/uploaded.pdf", dto.StoragePath);
    }

    [Fact]
    public void DocumentUploadResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new DocumentUploadResponseDto();

        // Assert
        Assert.Equal(Guid.Empty, dto.DocumentGuid);
        Assert.Equal(0, dto.DocumentId);
        Assert.Null(dto.DocumentBindingId);
        Assert.Null(dto.FileName);
        Assert.Equal(0, dto.FileSizeBytes);
        Assert.Null(dto.StoragePath);
    }

    [Fact]
    public void DocumentUploadResponseDto_WithoutBinding_SetsCorrectly()
    {
        // Arrange & Act
        var dto = new DocumentUploadResponseDto
        {
            DocumentGuid = Guid.NewGuid(),
            DocumentId = 100,
            DocumentBindingId = null,
            FileName = "no-binding.pdf",
            FileSizeBytes = 512,
            StoragePath = "/uploads/no-binding.pdf"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, dto.DocumentGuid);
        Assert.Equal(100, dto.DocumentId);
        Assert.Null(dto.DocumentBindingId);
    }

    [Fact]
    public void DocumentUploadResponseDto_WithBinding_SetsCorrectly()
    {
        // Arrange & Act
        var dto = new DocumentUploadResponseDto
        {
            DocumentGuid = Guid.NewGuid(),
            DocumentId = 100,
            DocumentBindingId = 50,
            FileName = "with-binding.pdf",
            FileSizeBytes = 1024,
            StoragePath = "/uploads/with-binding.pdf"
        };

        // Assert
        Assert.Equal(50, dto.DocumentBindingId);
        Assert.True(dto.DocumentBindingId.HasValue);
    }

    #endregion
}
