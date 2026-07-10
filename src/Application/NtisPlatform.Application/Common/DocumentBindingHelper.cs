using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Common;

/// <summary>
/// Shared static helpers for safely extracting document metadata from a
/// <see cref="DocumentBindingEntity"/> navigation property chain.
///
/// <para>
/// These helpers previously existed as private methods duplicated across
/// <c>PropertyPhotoApplicationService</c>, <c>PropertyCertificateApplicationService</c>,
/// and other services. Centralising them here follows the DRY principle and ensures
/// consistent null-safety and active/deleted checks across all modules.
/// </para>
/// </summary>
public static class DocumentBindingHelper
{
    /// <summary>
    /// Safely extracts the <see cref="DocumentEntity.DocumentGuid"/> from a
    /// <see cref="DocumentBindingEntity"/> navigation property.
    /// Returns <c>null</c> if the document is inactive, marked for deletion,
    /// or if any part of the navigation chain is <c>null</c>.
    /// </summary>
    public static Guid? GetSafeDocumentGuid(DocumentBindingEntity? documentBinding)
    {
        if (documentBinding == null || !documentBinding.IsActive || documentBinding.MarkedForDeletion)
            return null;

        var document = documentBinding.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.DocumentGuid;
    }

    /// <summary>
    /// Safely extracts the original file name from a <see cref="DocumentBindingEntity"/>
    /// navigation property. Returns <c>null</c> if the document is inactive or marked for deletion.
    /// </summary>
    public static string? GetSafeFileName(DocumentBindingEntity? documentBinding)
    {
        if (documentBinding == null || !documentBinding.IsActive || documentBinding.MarkedForDeletion)
            return null;

        var document = documentBinding.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.OriginalFileName;
    }

    /// <summary>
    /// Safely extracts the MIME type from a <see cref="DocumentBindingEntity"/>
    /// navigation property. Returns <c>null</c> if the document is inactive or marked for deletion.
    /// </summary>
    public static string? GetSafeMimeType(DocumentBindingEntity? documentBinding)
    {
        if (documentBinding == null || !documentBinding.IsActive || documentBinding.MarkedForDeletion)
            return null;

        var document = documentBinding.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.MimeType;
    }
}
