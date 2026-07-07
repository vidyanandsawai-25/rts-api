using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Extension point for entity-specific document side-effects.
/// Implement this interface in the Infrastructure layer to handle upload and delete
/// side-effects for a specific business entity (e.g. PropertyPhoto, PropertyCertificate).
///
/// <para>
/// This is the OCP hook that keeps <c>DocumentApplicationService</c> fully ignorant of
/// individual business entities. To add document support to a new module, simply:
/// <list type="number">
///   <item>Create a new <see cref="IDocumentBindingHandler"/> implementation in Infrastructure.</item>
///   <item>Register it in DI as <c>IDocumentBindingHandler</c>.</item>
/// </list>
/// No changes to <c>DocumentApplicationService</c> are required.
/// </para>
/// </summary>
public interface IDocumentBindingHandler
{
    /// <summary>
    /// The canonical reference table name this handler is responsible for.
    /// <c>DocumentApplicationService</c> matches handlers case-insensitively by <see cref="ReferenceTableName"/>
    /// and may also call <see cref="Handles"/> to support aliases/plurals.
    /// </summary>
    string ReferenceTableName { get; }

    /// <summary>
    /// Returns true if this handler should process the given reference table name.
    /// Defaults to exact match with <see cref="ReferenceTableName"/>.
    /// Override to support plural/alias table names (e.g. "PropertyPhotos").
    /// </summary>
    bool Handles(string referenceTableName);

    /// <summary>
    /// Called by <c>DocumentApplicationService</c> after the Document record and the
    /// <see cref="DocumentBindingEntity"/> have been persisted inside the active transaction.
    /// Use this to perform entity-specific post-processing (e.g. linking back the new
    /// binding ID onto the parent business entity row).
    /// </summary>
    /// <param name="documentId">The newly created <see cref="DocumentEntity"/> PK.</param>
    /// <param name="bindingId">The newly created <see cref="DocumentBindingEntity"/> PK.</param>
    /// <param name="referenceTableId">The integer reference table ID from the upload DTO.</param>
    /// <param name="uploadedBy">User ID performing the upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task OnAfterUploadAsync(
        int documentId,
        int bindingId,
        int referenceTableId,
        int uploadedBy,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called by <c>DocumentApplicationService</c> before a document is soft-deleted,
    /// once per active binding whose <c>ReferenceTableName</c> matches this handler.
    /// Use this to unlink or soft-delete the associated business entity record.
    /// </summary>
    /// <param name="binding">The active <see cref="DocumentBindingEntity"/> being removed.</param>
    /// <param name="deletedBy">User ID performing the delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task OnBeforeDeleteAsync(
        DocumentBindingEntity binding,
        int deletedBy,
        CancellationToken cancellationToken);
}
