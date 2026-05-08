using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Tests.Helpers;

/// <summary>
/// Helper methods for creating test entities with internal constructors
/// </summary>
public static class EntityTestHelpers
{
    /// <summary>
    /// Creates a minimal DocumentEntity for testing
    /// </summary>
    public static DocumentEntity CreateDocumentEntity(
        int id = 1,
        Guid? documentGuid = null,
        int uploadedByUserId = 1,
        string fileName = "test.pdf",
        string originalFileName = "test.pdf",
        string fileExtension = ".pdf",
        string mimeType = "application/pdf",
        long fileSizeBytes = 1024,
        string storagePath = "/uploads/test.pdf",
        string? storageProvider = null,
        int? ownerUserId = null,
        string? documentType = null,
        string? uploadStatusCode = null,
        int downloadCount = 0)
    {
        var entity = new DocumentEntity(
            documentGuid: documentGuid ?? Guid.NewGuid(),
            uploadedByUserId: uploadedByUserId,
            fileName: fileName,
            originalFileName: originalFileName,
            fileExtension: fileExtension,
            mimeType: mimeType,
            fileSizeBytes: fileSizeBytes,
            storagePath: storagePath,
            storageProvider: storageProvider,
            ownerUserId: ownerUserId,
            documentType: documentType,
            uploadStatusCode: uploadStatusCode,
            downloadCount: downloadCount);

        // Use reflection to set Id for testing purposes
        var idProperty = typeof(DocumentEntity).GetProperty("Id");
        idProperty?.SetValue(entity, id);

        return entity;
    }

    /// <summary>
    /// Creates a minimal DocumentBindingEntity for testing
    /// </summary>
    public static DocumentBindingEntity CreateDocumentBindingEntity(
        int documentId = 1,
        string moduleCode = "TEST",
        string referenceTableName = "TestTable",
        int? referenceTableId = 1,
        Guid? referenceTableIdGuid = null,
        string? bindingPurpose = null,
        bool isPrimaryDocument = false)
    {
        return new DocumentBindingEntity(
            documentId: documentId,
            moduleCode: moduleCode,
            referenceTableName: referenceTableName,
            referenceTableId: referenceTableId,
            referenceTableIdGuid: referenceTableIdGuid,
            bindingPurpose: bindingPurpose,
            isPrimaryDocument: isPrimaryDocument);
    }

    /// <summary>
    /// Creates a minimal PropertyCertificateEntity for testing
    /// </summary>
    public static PropertyCertificateEntity CreatePropertyCertificateEntity(
        int propertyId = 1,
        int certificateTypeId = 1,
        string? certificateNo = null,
        DateTime? issueDate = null,
        int? documentBindingId = null,
        bool isEnabled = false,
        bool markedForDeletion = false,
        DateTime? markedForDeletionDate = null)
    {
        return new PropertyCertificateEntity(
            propertyId: propertyId,
            certificateTypeId: certificateTypeId,
            certificateNo: certificateNo,
            issueDate: issueDate,
            documentBindingId: documentBindingId,
            isEnabled: isEnabled,
            markedForDeletion: markedForDeletion,
            markedForDeletionDate: markedForDeletionDate);
    }
}
