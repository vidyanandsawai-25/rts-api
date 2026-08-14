namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Resolves the current authenticated user's id from the active HTTP request's claims.
/// Centralizes the claim-lookup logic that was previously duplicated in individual controllers,
/// so Application-layer services can resolve "who is performing this operation" themselves
/// instead of requiring every controller action to compute and pass a <c>userId</c> parameter.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Returns the current user's id, resolved from (in order) the <c>NameIdentifier</c>,
    /// <c>Name</c>, or <c>"userId"</c> claim on the active request's principal.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when no valid, positive integer user identifier claim is present.
    /// </exception>
    int GetCurrentUserId();
}
