using NtisPlatform.Application.DTOs.PropertyCertificate;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.PropertyCertificate;

/// <summary>
/// Tests for PropertyCertificate DTOs to achieve 100% code coverage
/// </summary>
public class PropertyCertificateDtosTests
{
    #region PropertyCertificateUploadResponseDto Tests

    [Fact]
    public void PropertyCertificateUploadResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PropertyCertificateUploadResponseDto();

        // Assert
        Assert.Equal(0, dto.PropertyCertificateId);
        Assert.Equal(Guid.Empty, dto.DocumentGuid);
        Assert.Equal(0, dto.DocumentId);
        Assert.Equal(0, dto.DocumentBindingId);
        Assert.Equal(0, dto.PropertyId);
        Assert.Equal(0, dto.CertificateTypeId);
        Assert.Null(dto.CertificateNo);
        Assert.Null(dto.IssueDate);
        Assert.Equal(string.Empty, dto.FileName);
        Assert.Equal(0, dto.FileSizeBytes);
        Assert.Equal(string.Empty, dto.StoragePath);
    }

    [Fact]
    public void PropertyCertificateUploadResponseDto_CanSetAllProperties()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();
        var issueDate = new DateTime(2024, 6, 15);

        // Act
        var dto = new PropertyCertificateUploadResponseDto
        {
            PropertyCertificateId = 1,
            DocumentGuid = documentGuid,
            DocumentId = 100,
            DocumentBindingId = 50,
            PropertyId = 200,
            CertificateTypeId = 5,
            CertificateNo = "CERT-2024-001",
            IssueDate = issueDate,
            FileName = "certificate.pdf",
            FileSizeBytes = 4096,
            StoragePath = "/certificates/certificate.pdf"
        };

        // Assert
        Assert.Equal(1, dto.PropertyCertificateId);
        Assert.Equal(documentGuid, dto.DocumentGuid);
        Assert.Equal(100, dto.DocumentId);
        Assert.Equal(50, dto.DocumentBindingId);
        Assert.Equal(200, dto.PropertyId);
        Assert.Equal(5, dto.CertificateTypeId);
        Assert.Equal("CERT-2024-001", dto.CertificateNo);
        Assert.Equal(issueDate, dto.IssueDate);
        Assert.Equal("certificate.pdf", dto.FileName);
        Assert.Equal(4096, dto.FileSizeBytes);
        Assert.Equal("/certificates/certificate.pdf", dto.StoragePath);
    }

    [Fact]
    public void PropertyCertificateUploadResponseDto_NullableProperties_CanBeSetToNull()
    {
        // Arrange
        var dto = new PropertyCertificateUploadResponseDto
        {
            CertificateNo = "CERT-001",
            IssueDate = DateTime.Now
        };

        // Act
        dto.CertificateNo = null;
        dto.IssueDate = null;

        // Assert
        Assert.Null(dto.CertificateNo);
        Assert.Null(dto.IssueDate);
    }

    #endregion

    #region PropertyCertificateDto Tests

    [Fact]
    public void PropertyCertificateDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PropertyCertificateDto();

        // Assert
        Assert.Equal(0, dto.Id);
        Assert.Equal(0, dto.PropertyId);
        Assert.Equal(0, dto.CertificateTypeId);
        Assert.Null(dto.CertificateTypeName);
        Assert.Null(dto.CertificateNo);
        Assert.Null(dto.IssueDate);
        Assert.Null(dto.DocumentBindingId);
        Assert.Null(dto.DocumentGuid);
        Assert.False(dto.IsEnabled);
    }

    [Fact]
    public void PropertyCertificateDto_CanSetAllProperties()
    {
        // Arrange
        var documentGuid = Guid.NewGuid();
        var issueDate = new DateTime(2024, 3, 20);

        // Act
        var dto = new PropertyCertificateDto
        {
            Id = 10,
            PropertyId = 500,
            CertificateTypeId = 3,
            CertificateTypeName = "Ownership Certificate",
            CertificateNo = "OWN-2024-500",
            IssueDate = issueDate,
            DocumentBindingId = 75,
            DocumentGuid = documentGuid,
            IsEnabled = true
        };

        // Assert
        Assert.Equal(10, dto.Id);
        Assert.Equal(500, dto.PropertyId);
        Assert.Equal(3, dto.CertificateTypeId);
        Assert.Equal("Ownership Certificate", dto.CertificateTypeName);
        Assert.Equal("OWN-2024-500", dto.CertificateNo);
        Assert.Equal(issueDate, dto.IssueDate);
        Assert.Equal(75, dto.DocumentBindingId);
        Assert.Equal(documentGuid, dto.DocumentGuid);
        Assert.True(dto.IsEnabled);
    }

    [Fact]
    public void PropertyCertificateDto_NullableProperties_CanBeSetToNull()
    {
        // Arrange
        var dto = new PropertyCertificateDto
        {
            CertificateTypeName = "Test",
            CertificateNo = "TEST-001",
            IssueDate = DateTime.Now,
            DocumentBindingId = 1,
            DocumentGuid = Guid.NewGuid()
        };

        // Act
        dto.CertificateTypeName = null;
        dto.CertificateNo = null;
        dto.IssueDate = null;
        dto.DocumentBindingId = null;
        dto.DocumentGuid = null;

        // Assert
        Assert.Null(dto.CertificateTypeName);
        Assert.Null(dto.CertificateNo);
        Assert.Null(dto.IssueDate);
        Assert.Null(dto.DocumentBindingId);
        Assert.Null(dto.DocumentGuid);
    }

    [Fact]
    public void PropertyCertificateDto_IsEnabled_CanBeSetToFalse()
    {
        // Arrange & Act
        var dto = new PropertyCertificateDto
        {
            IsEnabled = false
        };

        // Assert
        Assert.False(dto.IsEnabled);
    }

    [Fact]
    public void PropertyCertificateDto_IsEnabled_CanBeSetToTrue()
    {
        // Arrange & Act
        var dto = new PropertyCertificateDto
        {
            IsEnabled = true
        };

        // Assert
        Assert.True(dto.IsEnabled);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void PropertyCertificateUploadResponseDto_WithEmptyStrings_HandlesCorrectly()
    {
        // Arrange & Act
        var dto = new PropertyCertificateUploadResponseDto
        {
            CertificateNo = "",
            FileName = "",
            StoragePath = ""
        };

        // Assert
        Assert.Equal("", dto.CertificateNo);
        Assert.Equal("", dto.FileName);
        Assert.Equal("", dto.StoragePath);
    }

    [Fact]
    public void PropertyCertificateDto_WithMinDate_HandlesCorrectly()
    {
        // Arrange & Act
        var dto = new PropertyCertificateDto
        {
            IssueDate = DateTime.MinValue
        };

        // Assert
        Assert.Equal(DateTime.MinValue, dto.IssueDate);
    }

    [Fact]
    public void PropertyCertificateDto_WithMaxDate_HandlesCorrectly()
    {
        // Arrange & Act
        var dto = new PropertyCertificateDto
        {
            IssueDate = DateTime.MaxValue
        };

        // Assert
        Assert.Equal(DateTime.MaxValue, dto.IssueDate);
    }

    [Fact]
    public void PropertyCertificateUploadResponseDto_WithLargeFileSizeBytes_HandlesCorrectly()
    {
        // Arrange & Act
        var dto = new PropertyCertificateUploadResponseDto
        {
            FileSizeBytes = long.MaxValue
        };

        // Assert
        Assert.Equal(long.MaxValue, dto.FileSizeBytes);
    }

    [Fact]
    public void PropertyCertificateDto_WithNegativeIds_HandlesCorrectly()
    {
        // Arrange & Act
        var dto = new PropertyCertificateDto
        {
            Id = -1,
            PropertyId = -100,
            CertificateTypeId = -5,
            DocumentBindingId = -50
        };

        // Assert
        Assert.Equal(-1, dto.Id);
        Assert.Equal(-100, dto.PropertyId);
        Assert.Equal(-5, dto.CertificateTypeId);
        Assert.Equal(-50, dto.DocumentBindingId);
    }

    #endregion
}
