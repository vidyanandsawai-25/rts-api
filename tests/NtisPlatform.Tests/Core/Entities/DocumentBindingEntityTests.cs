using NtisPlatform.Core.Entities;
using NtisPlatform.Tests.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Comprehensive tests for DocumentBindingEntity to achieve 100% coverage
/// </summary>
public class DocumentBindingEntityTests
{
    #region CreateWithIntReference Factory Method Tests

    [Fact]
    public void CreateWithIntReference_WithValidParameters_ReturnsNewEntity()
    {
        // Arrange & Act
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100,
            bindingPurpose: "MainCertificate");

        // Assert
        Assert.Equal(1, entity.DocumentId);
        Assert.Equal("PROPERTY", entity.ModuleCode);
        Assert.Equal("PropertyCertificate", entity.ReferenceTableName);
        Assert.Equal(100, entity.ReferenceTableId);
        Assert.Null(entity.ReferenceTableIdGuid);
        Assert.Equal("MainCertificate", entity.BindingPurpose);
        Assert.False(entity.IsPrimaryDocument);
        Assert.True(entity.IsReferenceValid);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void CreateWithIntReference_WithInvalidDocumentId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentBindingEntity.CreateWithIntReference(
                documentId: 0,
                moduleCode: "PROPERTY",
                referenceTableName: "PropertyCertificate",
                referenceTableId: 100));

        Assert.Contains("Document ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void CreateWithIntReference_WithEmptyModuleCode_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentBindingEntity.CreateWithIntReference(
                documentId: 1,
                moduleCode: "",
                referenceTableName: "PropertyCertificate",
                referenceTableId: 100));

        Assert.Contains("Module code cannot be empty", exception.Message);
    }

    [Fact]
    public void CreateWithIntReference_WithEmptyReferenceTableName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentBindingEntity.CreateWithIntReference(
                documentId: 1,
                moduleCode: "PROPERTY",
                referenceTableName: "",
                referenceTableId: 100));

        Assert.Contains("Reference table name cannot be empty", exception.Message);
    }

    [Fact]
    public void CreateWithIntReference_WithInvalidReferenceTableId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentBindingEntity.CreateWithIntReference(
                documentId: 1,
                moduleCode: "PROPERTY",
                referenceTableName: "PropertyCertificate",
                referenceTableId: 0));

        Assert.Contains("Reference table ID must be greater than zero", exception.Message);
    }

    #endregion

    #region CreateWithGuidReference Factory Method Tests

    [Fact]
    public void CreateWithGuidReference_WithValidParameters_ReturnsNewEntity()
    {
        // Arrange
        var referenceGuid = Guid.NewGuid();

        // Act
        var entity = DocumentBindingEntity.CreateWithGuidReference(
            documentId: 1,
            moduleCode: "WATER_TAX",
            referenceTableName: "WaterConnection",
            referenceTableIdGuid: referenceGuid,
            bindingPurpose: "ConnectionProof");

        // Assert
        Assert.Equal(1, entity.DocumentId);
        Assert.Equal("WATER_TAX", entity.ModuleCode);
        Assert.Equal("WaterConnection", entity.ReferenceTableName);
        Assert.Null(entity.ReferenceTableId);
        Assert.Equal(referenceGuid, entity.ReferenceTableIdGuid);
        Assert.Equal("ConnectionProof", entity.BindingPurpose);
        Assert.False(entity.IsPrimaryDocument);
        Assert.True(entity.IsReferenceValid);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void CreateWithGuidReference_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DocumentBindingEntity.CreateWithGuidReference(
                documentId: 1,
                moduleCode: "WATER_TAX",
                referenceTableName: "WaterConnection",
                referenceTableIdGuid: Guid.Empty));

        Assert.Contains("Reference table GUID cannot be empty", exception.Message);
    }

    #endregion

    #region Internal Constructor Tests

    [Fact]
    public void InternalConstructor_CreatesEntityWithAllProperties()
    {
        // Arrange & Act
        var entity = new DocumentBindingEntity(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100,
            referenceTableIdGuid: null,
            bindingPurpose: "MainCertificate",
            isPrimaryDocument: true);

        // Assert
        Assert.Equal(1, entity.DocumentId);
        Assert.Equal("PROPERTY", entity.ModuleCode);
        Assert.Equal("PropertyCertificate", entity.ReferenceTableName);
        Assert.Equal(100, entity.ReferenceTableId);
        Assert.Null(entity.ReferenceTableIdGuid);
        Assert.Equal("MainCertificate", entity.BindingPurpose);
        Assert.True(entity.IsPrimaryDocument);
        Assert.True(entity.IsReferenceValid);
        Assert.True(entity.IsActive);
    }

    #endregion

    #region Domain Method Tests

    [Fact]
    public void MarkAsPrimary_SetsIsPrimaryDocumentToTrue()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentBindingEntity(isPrimaryDocument: false);

        // Act
        entity.MarkAsPrimary();

        // Assert
        Assert.True(entity.IsPrimaryDocument);
    }

    [Fact]
    public void SetBindingPurpose_UpdatesBindingPurpose()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentBindingEntity();

        // Act
        entity.SetBindingPurpose("UpdatedPurpose");

        // Assert
        Assert.Equal("UpdatedPurpose", entity.BindingPurpose);
    }

    [Fact]
    public void SetAuthorizationContext_UpdatesAuthFields()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentBindingEntity();

        // Act
        entity.SetAuthorizationContext("AUTH_MODULE", 123);

        // Assert
        Assert.Equal("AUTH_MODULE", entity.AuthModuleCode);
        Assert.Equal(123, entity.AuthReferenceId);
    }

    [Fact]
    public void MarkAsInvalid_UpdatesValidationFields()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentBindingEntity();

        // Act
        entity.MarkAsInvalid("Reference not found");

        // Assert
        Assert.False(entity.IsReferenceValid);
        Assert.Equal("Reference not found", entity.ValidationError);
        Assert.NotNull(entity.LastValidatedDate);
    }

    [Fact]
    public void MarkAsValid_UpdatesValidationFields()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentBindingEntity();
        entity.MarkAsInvalid("Some error");

        // Act
        entity.MarkAsValid();

        // Assert
        Assert.True(entity.IsReferenceValid);
        Assert.Null(entity.ValidationError);
        Assert.NotNull(entity.LastValidatedDate);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void DocumentBindingEntity_InheritsFromBaseEntity()
    {
        // Arrange
        var entity = EntityTestHelpers.CreateDocumentBindingEntity();

        // Assert
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void ModuleCode_NormalizesToUpperCase()
    {
        // Arrange & Act
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "property",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Assert
        Assert.Equal("PROPERTY", entity.ModuleCode);
    }

    #endregion
}
