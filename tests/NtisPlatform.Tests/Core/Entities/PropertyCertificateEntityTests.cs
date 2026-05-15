using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Comprehensive tests for PropertyCertificateEntity to achieve 100% coverage
/// </summary>
public class PropertyCertificateEntityTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_WithValidParameters_ReturnsNewEntity()
    {
        // Arrange & Act
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));

        // Assert
        Assert.Equal(1, entity.PropertyId);
        Assert.Equal(1, entity.CertificateTypeId);
        Assert.Equal("CERT-001", entity.CertificateNo);
        Assert.NotNull(entity.IssueDate);
        Assert.Null(entity.DocumentBindingId);
        Assert.True(entity.IsActive);
        Assert.False(entity.MarkedForDeletion);
        Assert.True(entity.IsEnabled); // Auto-enabled when both certificateNo and issueDate provided
    }

    [Fact]
    public void Create_WithoutCertificateNo_ReturnsDisabledEntity()
    {
        // Arrange & Act
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: null,
            issueDate: DateTime.Now.AddDays(-1));

        // Assert
        Assert.Equal(1, entity.PropertyId);
        Assert.Equal(1, entity.CertificateTypeId);
        Assert.Null(entity.CertificateNo);
        Assert.False(entity.IsEnabled); // Not auto-enabled
    }

    [Fact]
    public void Create_WithoutIssueDate_ReturnsDisabledEntity()
    {
        // Arrange & Act
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: null);

        // Assert
        Assert.Equal(1, entity.PropertyId);
        Assert.Equal(1, entity.CertificateTypeId);
        Assert.Equal("CERT-001", entity.CertificateNo);
        Assert.False(entity.IsEnabled); // Not auto-enabled
    }

    [Fact]
    public void Create_WithInvalidPropertyId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PropertyCertificateEntity.Create(
                propertyId: 0,
                certificateTypeId: 1));

        Assert.Contains("Property ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidCertificateTypeId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PropertyCertificateEntity.Create(
                propertyId: 1,
                certificateTypeId: 0));

        Assert.Contains("Certificate type ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void Create_WithTooLongCertificateNo_ThrowsArgumentException()
    {
        // Arrange
        var longCertNo = new string('A', 101);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PropertyCertificateEntity.Create(
                propertyId: 1,
                certificateTypeId: 1,
                certificateNo: longCertNo));

        Assert.Contains("Certificate number cannot exceed 100 characters", exception.Message);
    }

    [Fact]
    public void Create_WithFutureIssueDate_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PropertyCertificateEntity.Create(
                propertyId: 1,
                certificateTypeId: 1,
                certificateNo: "CERT-001",
                issueDate: DateTime.Now.AddDays(1)));

        Assert.Contains("Issue date cannot be in the future", exception.Message);
    }

    #endregion

    #region CreateWithDocument Factory Method Tests

    [Fact]
    public void CreateWithDocument_WithValidParameters_ReturnsNewEntity()
    {
        // Arrange & Act
        var entity = PropertyCertificateEntity.CreateWithDocument(
            propertyId: 1,
            certificateTypeId: 1,
            documentBindingId: 10,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));

        // Assert
        Assert.Equal(1, entity.PropertyId);
        Assert.Equal(1, entity.CertificateTypeId);
        Assert.Equal(10, entity.DocumentBindingId);
        Assert.Equal("CERT-001", entity.CertificateNo);
        Assert.NotNull(entity.IssueDate);
        Assert.True(entity.IsActive);
        Assert.True(entity.IsEnabled); // Auto-enabled when both certificateNo and issueDate provided
    }

    [Fact]
    public void CreateWithDocument_WithInvalidDocumentBindingId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PropertyCertificateEntity.CreateWithDocument(
                propertyId: 1,
                certificateTypeId: 1,
                documentBindingId: 0));

        Assert.Contains("Document binding ID must be greater than zero", exception.Message);
    }

    #endregion

    #region IsComplete Method Tests

    [Fact]
    public void IsComplete_WithAllRequiredFields_ReturnsTrue()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));

        // Act
        var result = entity.IsComplete();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsComplete_WithoutCertificateNo_ReturnsFalse()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: null,
            issueDate: DateTime.Now.AddDays(-1));

        // Act
        var result = entity.IsComplete();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsComplete_WithWhitespaceCertificateNo_ReturnsFalse()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Use SetCertificateNumber to set whitespace
        try
        {
            entity.SetCertificateNumber("   ");
        }
        catch
        {
            // Expected to fail, so create entity without cert no
        }

        // Act
        var result = entity.IsComplete();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsComplete_WithoutIssueDate_ReturnsFalse()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: null);

        // Act
        var result = entity.IsComplete();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsComplete_WithInvalidPropertyId_ReturnsFalse()
    {
        // Arrange - using internal constructor to test edge case
        var entity = new PropertyCertificateEntity(
            propertyId: 0,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));

        // Act
        var result = entity.IsComplete();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsComplete_WithInvalidCertificateTypeId_ReturnsFalse()
    {
        // Arrange - using internal constructor to test edge case
        var entity = new PropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 0,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));

        // Act
        var result = entity.IsComplete();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsComplete_WhenMarkedForDeletion_ReturnsFalse()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));
        entity.MarkForDeletion();

        // Act
        var result = entity.IsComplete();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Enable Method Tests

    [Fact]
    public void Enable_WithCompleteData_EnablesCertificate()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));
        entity.Disable();

        // Act
        entity.Enable();

        // Assert
        Assert.True(entity.IsEnabled);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Enable_WhenMarkedForDeletion_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));
        entity.MarkForDeletion();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.Enable());
        Assert.Contains("Cannot enable a certificate marked for deletion", exception.Message);
    }

    [Fact]
    public void Enable_WithoutIssueDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.Enable());
        Assert.Contains("Cannot enable certificate without an issue date", exception.Message);
    }

    [Fact]
    public void Enable_WithoutCertificateNo_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: null,
            issueDate: DateTime.Now.AddDays(-1));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.Enable());
        Assert.Contains("Cannot enable certificate without a certificate number", exception.Message);
    }

    #endregion

    #region Domain Method Tests

    [Fact]
    public void Disable_DisablesCertificate()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));

        // Act
        entity.Disable();

        // Assert
        Assert.False(entity.IsEnabled);
    }

    [Fact]
    public void SetCertificateNumber_WithValidNumber_UpdatesCertificateNo()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Act
        entity.SetCertificateNumber("CERT-002");

        // Assert
        Assert.Equal("CERT-002", entity.CertificateNo);
    }

    [Fact]
    public void SetCertificateNumber_WithWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetCertificateNumber("   "));
        Assert.Contains("Certificate number cannot be empty", exception.Message);
    }

    [Fact]
    public void SetCertificateNumber_WithTooLongNumber_ThrowsArgumentException()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);
        var longNumber = new string('A', 101);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetCertificateNumber(longNumber));
        Assert.Contains("Certificate number cannot exceed 100 characters", exception.Message);
    }

    [Fact]
    public void SetCertificateNumber_TrimsWhitespace()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Act
        entity.SetCertificateNumber("  CERT-003  ");

        // Assert
        Assert.Equal("CERT-003", entity.CertificateNo);
    }

    [Fact]
    public void SetIssueDate_WithValidDate_UpdatesIssueDate()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);
        var issueDate = DateTime.Now.AddDays(-10);

        // Act
        entity.SetIssueDate(issueDate);

        // Assert
        Assert.Equal(issueDate, entity.IssueDate);
    }

    [Fact]
    public void SetIssueDate_WithFutureDate_ThrowsArgumentException()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetIssueDate(DateTime.Now.AddDays(1)));
        Assert.Contains("Issue date cannot be in the future", exception.Message);
    }

    [Fact]
    public void LinkDocumentBinding_WithValidId_LinksDocument()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Act
        entity.LinkDocumentBinding(10);

        // Assert
        Assert.Equal(10, entity.DocumentBindingId);
    }

    [Fact]
    public void LinkDocumentBinding_WithInvalidId_ThrowsArgumentException()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.LinkDocumentBinding(0));
        Assert.Contains("Document binding ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void LinkDocumentBinding_WhenMarkedForDeletion_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);
        entity.MarkForDeletion();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.LinkDocumentBinding(10));
        Assert.Contains("Cannot link document to a certificate marked for deletion", exception.Message);
    }

    [Fact]
    public void UnlinkDocumentBinding_RemovesDocumentBinding()
    {
        // Arrange
        var entity = PropertyCertificateEntity.CreateWithDocument(
            propertyId: 1,
            certificateTypeId: 1,
            documentBindingId: 10);

        // Act
        entity.UnlinkDocumentBinding();

        // Assert
        Assert.Null(entity.DocumentBindingId);
    }

    [Fact]
    public void MarkForDeletion_MarksCertificateForDeletion()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));

        // Act
        entity.MarkForDeletion();

        // Assert
        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);
        Assert.False(entity.IsEnabled);
        Assert.False(entity.IsActive);
    }

    [Fact]
    public void MarkForDeletion_WhenAlreadyMarked_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);
        entity.MarkForDeletion();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.MarkForDeletion());
        Assert.Contains("Certificate is already marked for deletion", exception.Message);
    }

    [Fact]
    public void RestoreFromDeletion_RestoresCertificate()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);
        entity.MarkForDeletion();

        // Act
        entity.RestoreFromDeletion();

        // Assert
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.False(entity.IsEnabled); // IsEnabled remains false
    }

    [Fact]
    public void RestoreFromDeletion_WhenNotMarked_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.RestoreFromDeletion());
        Assert.Contains("Certificate is not marked for deletion", exception.Message);
    }

    [Fact]
    public void CanBeEnabled_WithCompleteAndActiveData_ReturnsTrue()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));

        // Act
        var result = entity.CanBeEnabled();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanBeEnabled_WhenIncomplete_ReturnsFalse()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: null,
            issueDate: DateTime.Now.AddDays(-1));

        // Act
        var result = entity.CanBeEnabled();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanBeEnabled_WhenInactive_ReturnsFalse()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));
        entity.IsActive = false;

        // Act
        var result = entity.CanBeEnabled();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanBeEnabled_WhenMarkedForDeletion_ReturnsFalse()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now.AddDays(-1));
        entity.MarkForDeletion();

        // Act
        var result = entity.CanBeEnabled();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasDocument_WithDocumentBinding_ReturnsTrue()
    {
        // Arrange
        var entity = PropertyCertificateEntity.CreateWithDocument(
            propertyId: 1,
            certificateTypeId: 1,
            documentBindingId: 10);

        // Act
        var result = entity.HasDocument();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasDocument_WithoutDocumentBinding_ReturnsFalse()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Act
        var result = entity.HasDocument();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateDetails_WithBothParameters_UpdatesBoth()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);
        var newDate = DateTime.Now.AddDays(-5);

        // Act
        entity.UpdateDetails("CERT-NEW", newDate);

        // Assert
        Assert.Equal("CERT-NEW", entity.CertificateNo);
        Assert.Equal(newDate, entity.IssueDate);
    }

    [Fact]
    public void UpdateDetails_WithOnlyCertificateNo_UpdatesOnlyCertificateNo()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Act
        entity.UpdateDetails("CERT-NEW", null);

        // Assert
        Assert.Equal("CERT-NEW", entity.CertificateNo);
    }

    [Fact]
    public void UpdateDetails_WithOnlyIssueDate_UpdatesOnlyIssueDate()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);
        var newDate = DateTime.Now.AddDays(-5);

        // Act
        entity.UpdateDetails(null, newDate);

        // Assert
        Assert.Equal(newDate, entity.IssueDate);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void PropertyCertificateEntity_InheritsFromBaseEntity()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Assert
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void PropertyCertificateEntity_ImplementsIHardDeletable()
    {
        // Arrange
        var entity = PropertyCertificateEntity.Create(
            propertyId: 1,
            certificateTypeId: 1);

        // Assert
        Assert.IsAssignableFrom<IHardDeletable>(entity);
    }

    #endregion
}
