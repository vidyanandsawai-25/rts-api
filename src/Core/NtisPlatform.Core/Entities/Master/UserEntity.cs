using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// User entity mapped to [CORE].[UserMaster].
/// Implements IHardDeletable — delete sets IsActive = false + MarkedForDeletion = true.
/// The nightly cleanup task permanently removes rows where MarkedForDeletion = true.
/// Activate / Deactivate only toggle IsActive without touching MarkedForDeletion.
/// </summary>
[Table("UserMaster", Schema = "CORE")]
public class UserEntity : BaseEntity, IHardDeletable
{
    // Identity

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    // Profile

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(50)]
    public string? UserCode { get; set; }

    [MaxLength(400)]
    public string? Address { get; set; }

    [MaxLength(30)]
    public string? MobileNo { get; set; }

    [MaxLength(30)]
    public string? AlternateMobileNo { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    public bool MustChangePassword { get; set; }

    public int? EmployeeTypeID { get; set; }

    [MaxLength(10)]
    public string? Language { get; set; }

    [MaxLength(400)]
    public string? Remark { get; set; }

    // Security

    /// <summary>
    /// Bcrypt hashed password — never returned to clients.
    /// </summary>
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    // IHardDeletable
    // IsActive is inherited from BaseEntity.
    // When DeleteAsync runs: IsActive = false + MarkedForDeletion = true + MarkedForDeletionDate = now.
    // When DeactivateUserAsync runs: IsActive = false only — user is blocked but NOT queued for deletion.

    /// <inheritdoc/>
    public bool MarkedForDeletion { get; set; }

    /// <inheritdoc/>
    public DateTime? MarkedForDeletionDate { get; set; }

    // Auth tracking — owned by auth flow, never exposed in DTOs

    /// <summary>
    /// Uppercase-invariant username for case-insensitive, index-efficient login lookups.
    /// Computed server-side — never accepted from clients.
    /// </summary>
    //[MaxLength(100)]
    // public string? UserNameNormalized { get; set; }

    /// <summary>
    /// Incremented on each failed login. Reset to 0 on successful login.
    /// </summary>
    public int? FailedLoginCount { get; set; }

    /// <summary>
    /// Set when FailedLoginCount reaches the MaxFailedAttempts threshold.
    /// </summary>
    public DateTime? LockedUntilAt { get; set; }

    /// <summary>
    /// Updated on each successful login.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Two-factor authentication (TOTP) — owned by the 2FA flow, never exposed directly in DTOs

    /// <summary>
    /// Whether authenticator-app based two-factor authentication is enabled for this user.
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// The TOTP shared secret (base32), encrypted at rest via ASP.NET Core Data Protection.
    /// Never hashed — it must be decryptable to validate authenticator codes.
    /// Populated as soon as setup starts (pending) and remains set once 2FA is enabled;
    /// cleared on disable/reset.
    /// </summary>
    [MaxLength(500)]
    public string? TwoFactorSecretEncrypted { get; set; }

    /// <summary>
    /// UTC timestamp when 2FA was successfully enabled (first code verified).
    /// </summary>
    public DateTime? TwoFactorEnabledAt { get; set; }

    /// <summary>
    /// Set by an administrator to require this user to complete authenticator-app setup.
    /// While true and <see cref="TwoFactorEnabled"/> is still false, login succeeds normally but
    /// the login response carries a flag telling the frontend to route the user to 2FA setup
    /// before anywhere else — this is a policy nudge, not a login block (blocking outright would
    /// leave the user unable to ever reach the self-service setup page).
    /// </summary>
    public bool TwoFactorRequired { get; set; }

    /// <summary>
    /// Opaque stamp regenerated on every sensitive security change (2FA enable/disable/reset).
    /// Embedded in access tokens (as the "sst" claim) so previously issued tokens can be
    /// invalidated immediately instead of waiting for natural expiry.
    /// </summary>
    [MaxLength(64)]
    public string? SecurityStamp { get; set; }
}