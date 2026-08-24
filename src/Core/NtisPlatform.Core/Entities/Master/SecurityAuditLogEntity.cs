using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// An immutable structured security audit record. Used for 2FA/MFA lifecycle and verification
/// events. Never stores secrets, OTP codes, recovery codes, passwords, or tokens — see
/// <c>ISecurityAuditService</c> for the enforced event catalogue.
/// </summary>
[Table("SecurityAuditLog", Schema = "CORE")]
public class SecurityAuditLogEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Event type, e.g. "TwoFactorEnabled", "MfaVerificationFailed". See SecurityAuditEventType.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// The user the event concerns. Null only for events that occur before a user could be
    /// resolved (e.g. a challenge lookup that matched no row).
    /// </summary>
    public int? UserId { get; set; }

    [Required]
    public bool Success { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Optional non-sensitive context, e.g. "MaxAttemptsReached". Never a secret or code.
    /// </summary>
    [MaxLength(200)]
    public string? Detail { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }
}
