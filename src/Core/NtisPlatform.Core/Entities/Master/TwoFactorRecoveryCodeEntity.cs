using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// A single one-time-use two-factor recovery code for a user.
/// Codes are bcrypt-hashed (never stored or logged in plaintext) and consumed exactly once.
/// </summary>
[Table("TwoFactorRecoveryCode", Schema = "CORE")]
public class TwoFactorRecoveryCodeEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to UserMaster.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Bcrypt hash of the recovery code. The plaintext code is shown to the user exactly once
    /// at generation time and never persisted or logged.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when this code was redeemed. Null while still usable.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// UTC timestamp when this code was invalidated by a regeneration, disable, or reset
    /// operation, without ever being redeemed. Null while still usable.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity? User { get; set; }

    /// <summary>
    /// A code is available for redemption only while both markers are unset.
    /// Note: intentionally shadows BaseEntity.IsActive with redemption-specific logic.
    /// </summary>
    [NotMapped]
    public new bool IsActive => UsedAt == null && RevokedAt == null;
}
