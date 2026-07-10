using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Handler for document authorization at the entity level.
/// Each department/module implements a handler to resolve whether a user
/// can access a document bound to that module's entities.
///
/// Example: PtisDocumentAuthorizationHandler checks if user can access
/// the Property (AuthReferenceId) that the document is bound to.
/// </summary>
public interface IDocumentAuthorizationHandler
{
    /// <summary>
    /// The department ID this handler is responsible for.
    /// Example: 3 for "PTIS" department
    /// </summary>
    int DepartmentId { get; }

    /// <summary>
    /// Check if user can access a document bound to an entity in this handler's department.
    /// Resolves authorization via binding.AuthDepartmentId + binding.AuthReferenceId
    /// (e.g., PropertyId for PTIS).
    /// </summary>
    /// <param name="binding">The document binding with AuthReferenceId = entity PK</param>
    /// <param name="userId">User requesting access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can access, false otherwise</returns>
    Task<bool> CanAccessAsync(DocumentBindingEntity binding, int userId, CancellationToken cancellationToken);
}
