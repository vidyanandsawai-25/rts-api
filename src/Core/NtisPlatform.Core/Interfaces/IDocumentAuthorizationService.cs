namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for document access authorization
/// Ensures users can only access documents they own or have been granted access to
/// </summary>
public interface IDocumentAuthorizationService
{
    /// <summary>
    /// Check if user can access (read) a document
    /// </summary>
    /// <param name="documentGuid">Document unique identifier</param>
    /// <param name="userId">User requesting access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has read access, false otherwise</returns>
    Task<bool> CanAccessDocumentAsync(
        Guid documentGuid, 
        int userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user can modify (update/delete) a document
    /// </summary>
    /// <param name="documentGuid">Document unique identifier</param>
    /// <param name="userId">User requesting access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has write access, false otherwise</returns>
    Task<bool> CanModifyDocumentAsync(
        Guid documentGuid, 
        int userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user can access a document binding
    /// </summary>
    /// <param name="documentBindingId">Document binding ID</param>
    /// <param name="userId">User requesting access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has access, false otherwise</returns>
    Task<bool> CanAccessDocumentBindingAsync(
        int documentBindingId, 
        int userId, 
        CancellationToken cancellationToken = default);
}
