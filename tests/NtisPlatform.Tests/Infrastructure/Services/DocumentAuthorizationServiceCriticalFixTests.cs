using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Tests.Helpers;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Critical security regression tests for DocumentAuthorizationService:
/// Ensures AuthReferenceId is NOT treated as userId for authorization.
/// </summary>
public class DocumentAuthorizationServiceCriticalFixTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    #region Critical Security Fix: AuthReferenceId != userId

    [Fact]
    public async Task CanAccessDocumentBinding_DoesNotAuthorize_WhenAuthReferenceIdEqualsUserId()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<DocumentAuthorizationService>>();
        var service = new DocumentAuthorizationService(context, Enumerable.Empty<IDocumentAuthorizationHandler>(), loggerMock.Object);

        const int userId = 100;
        const int uploaderId = 200; // Different from userId

        // Create a document uploaded by someone else
        var document = EntityTestHelpers.CreateDocumentEntity(
            id: 1,
            uploadedByUserId: uploaderId,
            fileName: "test.pdf",
            originalFileName: "test.pdf");
        context.Documents.Add(document);

        // Create a binding with AuthReferenceId=100 and AuthDepartmentId=1
        var binding = EntityTestHelpers.CreateDocumentBindingEntity(
            documentId: document.Id,
            departmentId: 1,
            moduleId: 1,
            referenceTableName: "PropertyCertificates",
            referencePropertyName: "Id",
            referenceTableId: 1,
            bindingPurpose: "MainDocument",
            isPrimaryDocument: true);

        // Set auth context using reflection
        var authDeptProp = typeof(DocumentBindingEntity).GetProperty("AuthDepartmentId");
        authDeptProp?.SetValue(binding, 1);
        var authRefProp = typeof(DocumentBindingEntity).GetProperty("AuthReferenceId");
        authRefProp?.SetValue(binding, userId); // CRITICAL: This is PropertyId=100, NOT userId!

        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act: Try to access with userId that matches AuthReferenceId
        var canAccess = await service.CanAccessDocumentBindingAsync(
            binding.Id,
            userId,
            CancellationToken.None);

        // Assert: Should NOT grant access because:
        // 1. User is not the uploader
        // 2. AuthReferenceId is entity ID (PropertyId), not user ID
        canAccess.Should().BeFalse(
            "AuthReferenceId stores entity ID (e.g., PropertyId), not userId, so matching userId should NOT grant access");
    }

    [Fact]
    public async Task CanAccessDocumentBinding_Authorizes_WhenUserIsUploader()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<DocumentAuthorizationService>>();
        var service = new DocumentAuthorizationService(context, Enumerable.Empty<IDocumentAuthorizationHandler>(), loggerMock.Object);

        const int userId = 100;

        // Create a document uploaded by the current user
        var document = EntityTestHelpers.CreateDocumentEntity(
            id: 1,
            uploadedByUserId: userId);
        context.Documents.Add(document);

        var binding = EntityTestHelpers.CreateDocumentBindingEntity(
            documentId: document.Id,
            departmentId: 1,
            moduleId: 1,
            referenceTableName: "PropertyCertificates",
            referencePropertyName: "Id",
            referenceTableId: 1,
            bindingPurpose: "MainDocument",
            isPrimaryDocument: true);

        // Set auth context
        var authDeptProp = typeof(DocumentBindingEntity).GetProperty("AuthDepartmentId");
        authDeptProp?.SetValue(binding, 1);
        var authRefProp = typeof(DocumentBindingEntity).GetProperty("AuthReferenceId");
        authRefProp?.SetValue(binding, 999); // Different from userId

        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var canAccess = await service.CanAccessDocumentBindingAsync(
            binding.Id,
            userId,
            CancellationToken.None);

        // Assert: Should grant access because user is the uploader
        canAccess.Should().BeTrue("user should have access to documents they uploaded");
    }

    [Fact]
    public async Task CanAccessDocumentBinding_ReturnsFalse_WhenBindingNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<DocumentAuthorizationService>>();
        var service = new DocumentAuthorizationService(context, Enumerable.Empty<IDocumentAuthorizationHandler>(), loggerMock.Object);

        // Act
        var canAccess = await service.CanAccessDocumentBindingAsync(
            999,
            100,
            CancellationToken.None);

        // Assert
        canAccess.Should().BeFalse("should return false when binding does not exist");
    }

    [Fact]
    public async Task CanAccessDocumentBinding_ReturnsFalse_WhenBindingInactive()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<DocumentAuthorizationService>>();
        var service = new DocumentAuthorizationService(context, Enumerable.Empty<IDocumentAuthorizationHandler>(), loggerMock.Object);

        const int userId = 100;

        var document = EntityTestHelpers.CreateDocumentEntity(uploadedByUserId: userId);
        context.Documents.Add(document);

        var binding = EntityTestHelpers.CreateDocumentBindingEntity(
            documentId: document.Id,
            departmentId: 1,
            moduleId: 1,
            referenceTableName: "PropertyCertificates",
            referencePropertyName: "Id",
            referenceTableId: 1,
            bindingPurpose: "MainDocument",
            isPrimaryDocument: true);

        // Set auth context
        var authDeptProp = typeof(DocumentBindingEntity).GetProperty("AuthDepartmentId");
        authDeptProp?.SetValue(binding, 1);
        var authRefProp = typeof(DocumentBindingEntity).GetProperty("AuthReferenceId");
        authRefProp?.SetValue(binding, 100);

        binding.IsActive = false; // Mark inactive
        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        // Act
        var canAccess = await service.CanAccessDocumentBindingAsync(
            binding.Id,
            userId,
            CancellationToken.None);

        // Assert
        canAccess.Should().BeFalse("should return false for inactive bindings");
    }

    #endregion

    #region CanAccessDocumentAsync Tests

    [Fact]
    public async Task CanAccessDocument_Authorizes_WhenUserIsUploader()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<DocumentAuthorizationService>>();
        var service = new DocumentAuthorizationService(context, Enumerable.Empty<IDocumentAuthorizationHandler>(), loggerMock.Object);

        const int userId = 100;
        var documentGuid = Guid.NewGuid();

        var document = EntityTestHelpers.CreateDocumentEntity(
            documentGuid: documentGuid,
            uploadedByUserId: userId);
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        var canAccess = await service.CanAccessDocumentAsync(
            documentGuid,
            userId,
            CancellationToken.None);

        // Assert
        canAccess.Should().BeTrue("uploader should have access to their documents");
    }

    [Fact]
    public async Task CanAccessDocument_ReturnsFalse_WhenUserIsNotUploader()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<DocumentAuthorizationService>>();
        var service = new DocumentAuthorizationService(context, Enumerable.Empty<IDocumentAuthorizationHandler>(), loggerMock.Object);

        var documentGuid = Guid.NewGuid();

        var document = EntityTestHelpers.CreateDocumentEntity(
            documentGuid: documentGuid,
            uploadedByUserId: 200); // Different user
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        var canAccess = await service.CanAccessDocumentAsync(
            documentGuid,
            100, // Different userId
            CancellationToken.None);

        // Assert
        canAccess.Should().BeFalse("non-uploader should not have access without proper entity-level authorization");
    }

    #endregion

    #region CanModifyDocumentAsync Tests

    [Fact]
    public async Task CanModifyDocument_Authorizes_WhenUserIsUploader()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<DocumentAuthorizationService>>();
        var service = new DocumentAuthorizationService(context, Enumerable.Empty<IDocumentAuthorizationHandler>(), loggerMock.Object);

        const int userId = 100;
        var documentGuid = Guid.NewGuid();

        var document = EntityTestHelpers.CreateDocumentEntity(
            documentGuid: documentGuid,
            uploadedByUserId: userId);
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        var canModify = await service.CanModifyDocumentAsync(
            documentGuid,
            userId,
            CancellationToken.None);

        // Assert
        canModify.Should().BeTrue("uploader should be able to modify their documents");
    }

    [Fact]
    public async Task CanModifyDocument_ReturnsFalse_WhenUserIsNotUploader()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<DocumentAuthorizationService>>();
        var service = new DocumentAuthorizationService(context, Enumerable.Empty<IDocumentAuthorizationHandler>(), loggerMock.Object);

        var documentGuid = Guid.NewGuid();

        var document = EntityTestHelpers.CreateDocumentEntity(
            documentGuid: documentGuid,
            uploadedByUserId: 200);
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Act
        var canModify = await service.CanModifyDocumentAsync(
            documentGuid,
            100,
            CancellationToken.None);

        // Assert
        canModify.Should().BeFalse("non-uploader should not be able to modify documents");
    }

    #endregion
}
