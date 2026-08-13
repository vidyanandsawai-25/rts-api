using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// A short-lived, server-side, one-time-use MFA challenge issued after a successful password
/// check for a user with two-factor authentication enabled. The client only ever holds the
/// opaque random challenge id; this table stores a SHA-256 hash of it (never the raw value)
/// so a leaked database row cannot be replayed without also knowing the original token.
/// </summary>
[Table("TwoFactorChallenge", Schema = "CORE")]
public class MfaChallengeEntity
{
    /// <summary>
    /// Internal primary key. Never exposed to clients — the opaque challenge id is a separate,
    /// independently random value that only this row's <see cref="ChallengeHash"/> can verify.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// SHA-256 hash (hex) of the raw challenge token presented by the client. Indexed for lookup.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string ChallengeHash { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to UserMaster.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// What this challenge authorizes, e.g. "mfa-login". Reserved for future challenge types.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Purpose { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public int FailedAttemptCount { get; set; }

    /// <summary>
    /// Set once the challenge has been successfully verified. A non-null value makes the
    /// challenge permanently unusable, even if not yet expired.
    /// </summary>
    public DateTime? ConsumedAt { get; set; }

    /// <summary>
    /// Set when the challenge is invalidated other than by successful use — e.g. the maximum
    /// number of failed attempts was reached.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// SHA-256 hash (hex) of the one-time numeric code sent to the user via email/SMS. Only set
    /// for code-carrying purposes (e.g. "login-otp", "forgot-password-otp",
    /// "2fa-enable-email-verify"); NULL for bearer-token-only purposes (e.g. "mfa-login",
    /// "password-reset"), where the code is instead validated externally (TOTP secret) or the
    /// challenge itself is the secret (reset token).
    /// </summary>
    [MaxLength(64)]
    public string? CodeHash { get; set; }

    /// <summary>
    /// How the code in <see cref="CodeHash"/> was delivered, e.g. "Email", "Sms", "Email,Sms".
    /// NULL when this challenge does not carry a delivered code.
    /// </summary>
    [MaxLength(20)]
    public string? Channel { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity? User { get; set; }
}
