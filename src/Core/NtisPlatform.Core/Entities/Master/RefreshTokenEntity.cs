using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Refresh token entity for token-based authentication
/// Stores refresh tokens with expiration and revocation tracking
/// </summary>
[Table("RefreshToken", Schema = "CORE")]
public class RefreshTokenEntity : BaseEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The hashed refresh token value (stored using bcrypt hash for security)
    /// Never stores the plaintext token - only the hash for verification
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to UserMaster
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// When this refresh token expires
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this refresh token has been revoked (e.g., on logout)
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// When this token was revoked (if applicable)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// IP address from which the token was issued
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent from which the token was issued
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// If this token was replaced by a new one (via refresh endpoint), store the new token ID here
    /// </summary>
    public int? ReplacedByTokenId { get; set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual UserMasterEntity? User { get; set; }

    /// <summary>
    /// Check if token is active (not expired and not revoked)
    /// Note: This property intentionally shadows BaseEntity.IsActive with token-specific logic
    /// </summary>
    [NotMapped]
    public new bool IsActive => !IsRevoked && ExpiresAt > DateTime.Now;
}
