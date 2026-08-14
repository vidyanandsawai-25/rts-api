namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Login response DTO
/// </summary>
public class LoginResponseDto
{
    public bool Success { get; set; }

    /// <summary>
    /// JWT access token (short-lived)
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Refresh token (long-lived, used to obtain new access tokens)
    /// </summary>
    public string? RefreshToken { get; set; }

    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool RequiresPasswordChange { get; set; }

    /// <summary>
    /// True when the password was valid but the account has 2FA enabled — no access or refresh
    /// token has been issued yet. The client must call the two-factor verify endpoint with
    /// <see cref="ChallengeId"/> to complete login. <see cref="Token"/> and
    /// <see cref="RefreshToken"/> are guaranteed null whenever this is true.
    /// </summary>
    public bool RequiresTwoFactor { get; set; }

    /// <summary>
    /// Which verification flow the pending challenge belongs to: "totp" (authenticator app or
    /// recovery code, verify via <c>two-factor/verify</c>) or "otp" (emailed/texted one-time
    /// code, verify via <c>login-otp/verify</c>). Only present when
    /// <see cref="RequiresTwoFactor"/> is true.
    /// </summary>
    public string? TwoFactorMethod { get; set; }

    /// <summary>
    /// Opaque, one-time-use challenge id for the pending MFA verification. Only present when
    /// <see cref="RequiresTwoFactor"/> is true.
    /// </summary>
    public string? ChallengeId { get; set; }

    /// <summary>
    /// Expiry of the MFA challenge, in server-local time (no UTC offset). Only present when <see cref="RequiresTwoFactor"/> is true.
    /// </summary>
    public DateTime? ChallengeExpiresAt { get; set; }

    /// <summary>
    /// True when an administrator has required this account to set up 2FA but the user hasn't
    /// completed enrollment yet. Unlike <see cref="RequiresTwoFactor"/>, this does NOT block
    /// login — <see cref="Token"/>/<see cref="RefreshToken"/> are still issued normally. It's a
    /// signal for the frontend to route the user to the authenticator setup page instead of
    /// their usual landing page.
    /// </summary>
    public bool RequiresTwoFactorSetup { get; set; }

    /// <summary>
    /// True when the password was correct but a new MFA/OTP challenge could not be issued because
    /// this account recently exhausted too many challenges (see the "MaxOtpChallengeLockouts"
    /// SECURITY_AUTH setting). <see cref="Success"/> is false when this is true — the client
    /// should surface <see cref="Message"/> and let the user retry after the cooldown.
    /// </summary>
    public bool Throttled { get; set; }

    /// <summary>
    /// Number of further wrong-password attempts allowed before the account locks. Only present
    /// when this attempt's password was wrong and the account is not yet locked (i.e.
    /// <see cref="Success"/> is false, <see cref="Message"/> is a generic invalid-credentials
    /// message, and the account isn't locked). Lets the client show "N attempts remaining"
    /// instead of a flat error. Not set once the account actually locks — <see cref="Message"/>
    /// carries the lockout time in that case instead.
    /// </summary>
    public int? RemainingLoginAttempts { get; set; }
}
