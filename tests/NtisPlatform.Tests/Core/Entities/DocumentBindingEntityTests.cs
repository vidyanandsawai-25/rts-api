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

    #region ValidateBinding Method Tests

    [Fact]
    public void ValidateBinding_WithValidIntReference_ReturnsTrue()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act
        var result = entity.ValidateBinding();

        // Assert
        Assert.True(result);
        Assert.True(entity.IsReferenceValid);
        Assert.Null(entity.ValidationError);
        Assert.NotNull(entity.LastValidatedDate);
    }

    [Fact]
    public void ValidateBinding_WithValidGuidReference_ReturnsTrue()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithGuidReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableIdGuid: Guid.NewGuid());

        // Act
        var result = entity.ValidateBinding();

        // Assert
        Assert.True(result);
        Assert.True(entity.IsReferenceValid);
        Assert.Null(entity.ValidationError);
        Assert.NotNull(entity.LastValidatedDate);
    }

    [Fact]
    public void ValidateBinding_WithNoReference_ReturnsFalse()
    {
        // Arrange - using internal constructor to create entity without references
        var entity = new DocumentBindingEntity(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: null,
            referenceTableIdGuid: null,
            bindingPurpose: "Test",
            isPrimaryDocument: false);

        // Act
        var result = entity.ValidateBinding();

        // Assert
        Assert.False(result);
        Assert.False(entity.IsReferenceValid);
        Assert.Contains("must have either ReferenceTableId or ReferenceTableIdGuid", entity.ValidationError);
    }

    [Fact]
    public void ValidateBinding_WithBothIntAndGuidReference_ReturnsFalse()
    {
        // Arrange - using internal constructor to create entity with both references
        var entity = new DocumentBindingEntity(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100,
            referenceTableIdGuid: Guid.NewGuid(),
            bindingPurpose: "Test",
            isPrimaryDocument: false);

        // Act
        var result = entity.ValidateBinding();

        // Assert
        Assert.False(result);
        Assert.False(entity.IsReferenceValid);
        Assert.Contains("cannot have both ReferenceTableId and ReferenceTableIdGuid", entity.ValidationError);
    }

    [Fact]
    public void ValidateBinding_WithExpiredBinding_ReturnsFalse()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Use reflection to set ExpiryDate to a past date
        var expiryProperty = typeof(DocumentBindingEntity).GetProperty("ExpiryDate");
        expiryProperty!.SetValue(entity, DateTime.Now.AddDays(-1));

        // Act
        var result = entity.ValidateBinding();

        // Assert
        Assert.False(result);
        Assert.False(entity.IsReferenceValid);
        Assert.Contains("expired", entity.ValidationError);
    }

    [Fact]
    public void ValidateBinding_WithZeroIntReference_ReturnsFalse()
    {
        // Arrange - using internal constructor to create entity with zero int reference
        var entity = new DocumentBindingEntity(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 0,
            referenceTableIdGuid: null,
            bindingPurpose: "Test",
            isPrimaryDocument: false);

        // Act
        var result = entity.ValidateBinding();

        // Assert
        Assert.False(result);
        Assert.False(entity.IsReferenceValid);
    }

    [Fact]
    public void ValidateBinding_WithEmptyGuidReference_ReturnsFalse()
    {
        // Arrange - using internal constructor to create entity with empty GUID
        var entity = new DocumentBindingEntity(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: null,
            referenceTableIdGuid: Guid.Empty,
            bindingPurpose: "Test",
            isPrimaryDocument: false);

        // Act
        var result = entity.ValidateBinding();

        // Assert
        Assert.False(result);
        Assert.False(entity.IsReferenceValid);
    }

    #endregion

    #region Additional Domain Method Tests

    [Fact]
    public void UnmarkAsPrimary_SetsIsPrimaryDocumentToFalse()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        entity.MarkAsPrimary();

        // Act
        entity.UnmarkAsPrimary();

        // Assert
        Assert.False(entity.IsPrimaryDocument);
    }

    [Fact]
    public void SetDisplayOrder_WithValidOrder_UpdatesDisplayOrder()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act
        entity.SetDisplayOrder(5);

        // Assert
        Assert.Equal(5, entity.DisplayOrder);
    }

    [Fact]
    public void SetDisplayOrder_WithNegativeOrder_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetDisplayOrder(-1));
        Assert.Contains("Display order cannot be negative", exception.Message);
    }

    [Fact]
    public void AddNotes_WithValidNotes_UpdatesNotes()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act
        entity.AddNotes("Important document for property verification");

        // Assert
        Assert.Equal("Important document for property verification", entity.Notes);
    }

    [Fact]
    public void AddNotes_WithEmptyNotes_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.AddNotes(""));
        Assert.Contains("Notes cannot be empty", exception.Message);
    }

    [Fact]
    public void AddNotes_WithTooLongNotes_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        var longNotes = new string('A', 1001);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.AddNotes(longNotes));
        Assert.Contains("Notes cannot exceed 1000 characters", exception.Message);
    }

    [Fact]
    public void SetExpiryDate_WithFutureDate_UpdatesExpiryDate()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        var futureDate = DateTime.Now.AddDays(30);

        // Act
        entity.SetExpiryDate(futureDate);

        // Assert
        Assert.NotNull(entity.ExpiryDate);
        Assert.True(entity.ExpiryDate.Value > DateTime.Now);
    }

    [Fact]
    public void SetExpiryDate_WithPastDate_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        var pastDate = DateTime.Now.AddDays(-1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetExpiryDate(pastDate));
        Assert.Contains("Expiry date must be in the future", exception.Message);
    }

    [Fact]
    public void IsExpired_WhenNotExpired_ReturnsFalse()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        entity.SetExpiryDate(DateTime.Now.AddDays(30));

        // Act
        var result = entity.IsExpired();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpired_WhenExpired_ReturnsTrue()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Use reflection to set expired date
        var expiryProperty = typeof(DocumentBindingEntity).GetProperty("ExpiryDate");
        expiryProperty!.SetValue(entity, DateTime.Now.AddDays(-1));

        // Act
        var result = entity.IsExpired();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpired_WhenNoExpiryDate_ReturnsFalse()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act
        var result = entity.IsExpired();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SetAuthorizationContext_WithEmptyModuleCode_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetAuthorizationContext("", 123));
        Assert.Contains("Authorization module code cannot be empty", exception.Message);
    }

    [Fact]
    public void SetAuthorizationContext_WithInvalidReferenceId_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetAuthorizationContext("AUTH", 0));
        Assert.Contains("Authorization reference ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void IsActiveAndValid_WhenActiveAndValid_ReturnsTrue()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act
        var result = entity.IsActiveAndValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsActiveAndValid_WhenInactive_ReturnsFalse()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        entity.IsActive = false;

        // Act
        var result = entity.IsActiveAndValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsActiveAndValid_WhenInvalid_ReturnsFalse()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        entity.MarkAsInvalid("Test error");

        // Act
        var result = entity.IsActiveAndValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsActiveAndValid_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Use reflection to set expired date
        var expiryProperty = typeof(DocumentBindingEntity).GetProperty("ExpiryDate");
        expiryProperty!.SetValue(entity, DateTime.Now.AddDays(-1));

        // Act
        var result = entity.IsActiveAndValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateReferenceTableId_WithValidId_UpdatesReference()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act
        entity.UpdateReferenceTableId(200);

        // Assert
        Assert.Equal(200, entity.ReferenceTableId);
    }

    [Fact]
    public void UpdateReferenceTableId_WithInvalidId_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.UpdateReferenceTableId(0));
        Assert.Contains("Reference table ID must be greater than zero", exception.Message);
    }

    [Fact]
    public void UpdateReferenceTableId_OnGuidBasedBinding_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithGuidReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableIdGuid: Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.UpdateReferenceTableId(100));
        Assert.Contains("Cannot update reference table ID for a GUID-based binding", exception.Message);
    }

    [Fact]
    public void UpdateReferenceTableIdGuid_WithValidGuid_UpdatesReference()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithGuidReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableIdGuid: Guid.NewGuid());
        var newGuid = Guid.NewGuid();

        // Act
        entity.UpdateReferenceTableIdGuid(newGuid);

        // Assert
        Assert.Equal(newGuid, entity.ReferenceTableIdGuid);
    }

    [Fact]
    public void UpdateReferenceTableIdGuid_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithGuidReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableIdGuid: Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.UpdateReferenceTableIdGuid(Guid.Empty));
        Assert.Contains("Reference table GUID cannot be empty", exception.Message);
    }

    [Fact]
    public void UpdateReferenceTableIdGuid_OnIntBasedBinding_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => entity.UpdateReferenceTableIdGuid(Guid.NewGuid()));
        Assert.Contains("Cannot update reference table GUID for an INT-based binding", exception.Message);
    }

    [Fact]
    public void MarkAsInvalid_WithEmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.MarkAsInvalid(""));
        Assert.Contains("Reason cannot be empty", exception.Message);
    }

    [Fact]
    public void SetBindingPurpose_WithEmptyPurpose_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetBindingPurpose(""));
        Assert.Contains("Binding purpose cannot be empty", exception.Message);
    }

    [Fact]
    public void SetBindingPurpose_WithTooLongPurpose_ThrowsArgumentException()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);
        var longPurpose = new string('A', 201);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => entity.SetBindingPurpose(longPurpose));
        Assert.Contains("Binding purpose cannot exceed 200 characters", exception.Message);
    }

    [Fact]
    public void MarkAsPrimary_SetsDisplayOrderToZero()
    {
        // Arrange
        var entity = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1,
            moduleCode: "PROPERTY",
            referenceTableName: "PropertyCertificate",
            referenceTableId: 100);

        // Act
        entity.MarkAsPrimary();

        // Assert
        Assert.Equal(0, entity.DisplayOrder);
    }

    #endregion
}
