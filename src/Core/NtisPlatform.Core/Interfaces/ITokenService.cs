namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate a JWT access token for authenticated user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="username">Username</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(int userId, string username);

    /// <summary>
    /// Generate a cryptographically secure refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Generates a short-lived token (SLT) for a queued report request.
    /// JWT carrying scope="report-slt" and reportRequestId — presented by the worker to
    /// <c>/authenticate</c> in exchange for an LLT. Uses the same key/issuer/audience as all other
    /// tokens; type is distinguished by the scope claim only.
    /// </summary>
    string GenerateShortLivedToken(Guid reportRequestId, int userId, int expiresInMinutes);

    /// <summary>
    /// Validates an SLT produced by <see cref="GenerateShortLivedToken"/>.
    /// Checks signature, expiry, and scope="report-slt".
    /// Returns (reportRequestId, userId) on success; null on any failure.
    /// </summary>
    (Guid reportRequestId, int userId)? ValidateShortLivedToken(string token);

    /// <summary>
    /// Generates a long-lived worker token (LLT) scoped to a single report request.
    /// Carries scope=report-worker and the reportRequestId claim. Uses the same audience as all
    /// tokens; scope enforces the policy requirement on worker endpoints.
    /// </summary>
    string GenerateReportWorkerToken(Guid reportRequestId, int subjectUserId, int expiresInMinutes);

    /// <summary>
    /// Generates a short-lived hub token (5 min) scoped to a single user for SignalR authentication.
    /// Carries scope=report-hub; the ReportHub policy requires this claim.
    /// </summary>
    string GenerateHubToken(int userId, int expiresInMinutes = 5);

    /// <summary>
    /// Validate a JWT token and extract claims
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>Token validation result with user information</returns>
    JwtValidationResult ValidateToken(string token);
}

/// <summary>
/// Result of JWT token validation
/// </summary>
public class JwtValidationResult
{
    public bool IsValid { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}
