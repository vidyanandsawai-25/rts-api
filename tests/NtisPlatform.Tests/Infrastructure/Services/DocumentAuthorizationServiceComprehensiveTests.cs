using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive tests for DocumentAuthorizationService to achieve 100% line coverage
/// Focuses on CanAccessDocumentBindingAsync method
/// </summary>
public class DocumentAuthorizationServiceComprehensiveTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    #region CanAccessDocumentBindingAsync Tests

    [Fact]
    public async Task CanAccessDocumentBindingAsync_WithOwnerUser_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CanAccessDocumentBindingAsync(binding.Id, 1, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_WithUploaderUser_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 2,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");
        document.TransferOwnership(1); // Owner is 1, uploader is 2

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CanAccessDocumentBindingAsync(binding.Id, 2, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_WithAuthorizedUser_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        binding.SetAuthorizationContext("AUTH_MODULE", 999); // User 999 is authorized

        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CanAccessDocumentBindingAsync(binding.Id, 999, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_WithUnauthorizedUser_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act - User 999 is not the owner, uploader, or authorized
        var result = await service.CanAccessDocumentBindingAsync(binding.Id, 999, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_WithNonExistentBinding_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        // Act
        var result = await service.CanAccessDocumentBindingAsync(9999, 1, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_WithInactiveBinding_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        binding.IsActive = false;

        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CanAccessDocumentBindingAsync(binding.Id, 1, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_WithNullAuthReferenceId_ReturnsTrueForOwner()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        // No SetAuthorizationContext called, so AuthReferenceId is null

        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CanAccessDocumentBindingAsync(binding.Id, 1, CancellationToken.None);

        // Assert
        Assert.True(result); // Owner should still have access
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_WithDifferentOwnerAndUploader_BothHaveAccess()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 2,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");
        document.TransferOwnership(3); // Owner is 3, uploader is 2

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act & Assert
        var ownerHasAccess = await service.CanAccessDocumentBindingAsync(binding.Id, 3, CancellationToken.None);
        Assert.True(ownerHasAccess);

        var uploaderHasAccess = await service.CanAccessDocumentBindingAsync(binding.Id, 2, CancellationToken.None);
        Assert.True(uploaderHasAccess);

        var otherUserHasAccess = await service.CanAccessDocumentBindingAsync(binding.Id, 999, CancellationToken.None);
        Assert.False(otherUserHasAccess);
    }

    #endregion

    #region CanAccessDocumentAsync Additional Tests

    [Fact]
    public async Task CanAccessDocumentAsync_WithOwnerUser_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CanAccessDocumentAsync(document.DocumentGuid, 1, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessDocumentAsync_WithBindingAuthorization_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: document.Id,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        binding.SetAuthorizationContext("AUTH_MODULE", 999);

        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CanAccessDocumentAsync(document.DocumentGuid, 999, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanModifyDocumentAsync_WithOwnerUser_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CanModifyDocumentAsync(document.DocumentGuid, 1, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanModifyDocumentAsync_WithUnauthorizedUser_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentAuthorizationService(context);

        var document = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "test.pdf",
            originalFileName: "test.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1024,
            storagePath: "/uploads/test.pdf");

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CanModifyDocumentAsync(document.DocumentGuid, 999, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion
}
