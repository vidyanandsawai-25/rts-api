using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Authentication service implementation
/// Business logic for user authentication
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IRepository<UserRoleMasterEntity> _userRoleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IMfaChallengeService _mfaChallengeService;
    private readonly IAuthTokenIssuerService _authTokenIssuer;
    private readonly ISecuritySettingsService _securitySettings;
    private readonly IOtpChallengeService _otpChallengeService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IConfiguration configuration,
        IRepository<UserRoleMasterEntity> userRoleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IMfaChallengeService mfaChallengeService,
        IAuthTokenIssuerService authTokenIssuer,
        ISecuritySettingsService securitySettings,
        IOtpChallengeService otpChallengeService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _configuration = configuration;
        _userRoleRepository = userRoleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _mfaChallengeService = mfaChallengeService;
        _authTokenIssuer = authTokenIssuer;
        _securitySettings = securitySettings;
        _otpChallengeService = otpChallengeService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        // Find user by username
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent username: {Username}", request.Username);
            return new LoginResponseDto { Success = false, Message = "Invalid username or password" };
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
            return new LoginResponseDto { Success = false, Message = "User account is inactive. Please contact administrator." };
        }

        // Check if account is locked
        if (user.LockedUntilAt.HasValue && user.LockedUntilAt.Value > DateTime.Now)
        {
            _logger.LogWarning("Login attempt for locked account: {UserId}, locked until {LockedUntil}",
                user.Id, user.LockedUntilAt.Value);
            return new LoginResponseDto { Success = false, Message = $"Account is locked until {user.LockedUntilAt.Value:u}. Please try again later." };
        }

        // Verify password (if PasswordHash is null, fail authentication)
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger.LogWarning("Login attempt for user with no password set: {UserId}", user.Id);
            return new LoginResponseDto { Success = false, Message = "Password not set for this user. Please contact administrator." };
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            var increment = await _userRepository.IncrementFailedLoginCountAsync(user.Id, cancellationToken);
            _logger.LogWarning("Failed login attempt for user: {UserId}", user.Id);

            if (increment.LockedUntil.HasValue)
            {
                return new LoginResponseDto { Success = false, Message = $"Account is locked until {increment.LockedUntil.Value:u}. Please try again later." };
            }

            return new LoginResponseDto
            {
                Success = false,
                Message = "Invalid username or password",
                RemainingLoginAttempts = increment.RemainingAttempts
            };
        }

        // Check if user must change password. Success is deliberately true here (not false) so
        // this reaches the client as a normal 200 response with RequiresPasswordChange set —
        // same pattern as the RequiresTwoFactor branches below — rather than being discarded by
        // the controller's generic Unauthorized(...) path for Success=false responses.
        if (user.MustChangePassword)
        {
            _logger.LogInformation("User {UserId} must change password before proceeding", user.Id);
            return new LoginResponseDto
            {
                Success = true,
                Message = "You must change your password before logging in. Please contact administrator.",
                RequiresPasswordChange = true
            };
        }

        // Check password expiry (0 or unset disables the check).
        var passwordExpiryDays = await _securitySettings.GetAsync("PASSWORDEXPIRYDAYS", 90, cancellationToken);
        if (passwordExpiryDays > 0 && user.PasswordChangedAt.HasValue &&
            (DateTime.Now - user.PasswordChangedAt.Value).TotalDays >= passwordExpiryDays)
        {
            _logger.LogInformation("User {UserId} password has expired", user.Id);
            return new LoginResponseDto
            {
                Success = true,
                Message = "Your password has expired. Please reset it to continue.",
                RequiresPasswordChange = true
            };
        }

        // Password valid - reset failed login count and update last login
        await _userRepository.ResetFailedLoginCountAsync(user.Id, cancellationToken);
        await _userRepository.UpdateLastLoginAsync(user.Id, cancellationToken);

        // 2FALOGIN is the master switch for 2FA enforcement at login — an admin can flip it off
        // to suspend 2FA org-wide (e.g. an incident, a rollout phase) without touching any
        // individual user's TwoFactorEnabled enrollment. Nobody gets challenged while it's off,
        // TOTP-enrolled or not; flipping it back on immediately resumes enforcement for everyone,
        // exactly as configured, since no per-user state was changed.
        var twoFactorLoginEnabled = await _securitySettings.GetAsync("2FALOGIN", false, cancellationToken);

        if (twoFactorLoginEnabled && user.TwoFactorEnabled)
        {
            // Per-user TOTP enrollment — a short-lived challenge must be verified first via
            // IMfaChallengeService before an access/refresh token is issued.
            var creation = await _mfaChallengeService.CreateLoginChallengeAsync(user.Id, ipAddress: null, userAgent: null, cancellationToken);
            if (!creation.Success)
            {
                _logger.LogWarning("MFA challenge creation throttled for user {UserId}", user.Id);
                return new LoginResponseDto
                {
                    Success = false,
                    Throttled = true,
                    Message = "Too many recent failed verification attempts. Please try again later."
                };
            }

            var challenge = creation.Challenge!;
            _logger.LogInformation("Password verified for user {UserId}; awaiting MFA verification", user.Id);

            return new LoginResponseDto
            {
                Success = true,
                RequiresTwoFactor = true,
                TwoFactorMethod = "totp",
                ChallengeId = challenge.ChallengeId,
                ChallengeExpiresAt = challenge.ExpiresAt,
                UserId = user.Id,
                Username = user.UserName,
                Message = "Two-factor authentication code required"
            };
        }

        if (twoFactorLoginEnabled && !user.TwoFactorEnabled)
        {
            // No per-user TOTP enrollment — fall back to the config-driven (SECURITY_AUTH) OTP
            // layer. This makes 2FA mandatory org-wide instead of leaving unenrolled users with
            // no second factor at all.
            var sendEmail = await _securitySettings.GetAsync("LOGINOTPONMAIL", false, cancellationToken);
            var sendSms = await _securitySettings.GetAsync("LOGINOTPONSMS", false, cancellationToken);

            if (!sendEmail && !sendSms)
            {
                // No delivery channel is configured — there's no way to actually send a code.
                // Don't block every login on a misconfiguration; log it and fall through to a
                // normal password-only login instead.
                _logger.LogWarning(
                    "2FALOGIN is enabled but neither LOGINOTPONMAIL nor LOGINOTPONSMS is on; skipping OTP step for user {UserId}.",
                    user.Id);
            }
            else
            {
                var creation = await _otpChallengeService.CreateAsync(
                    user, OtpChallengePurpose.LoginOtp, sendEmail, sendSms, ipAddress: null, userAgent: null, cancellationToken);
                if (!creation.Success)
                {
                    _logger.LogWarning("Login OTP challenge creation throttled for user {UserId}", user.Id);
                    return new LoginResponseDto
                    {
                        Success = false,
                        Throttled = true,
                        Message = "Too many recent failed verification attempts. Please try again later."
                    };
                }

                var otpChallenge = creation.Challenge!;
                _logger.LogInformation("Password verified for user {UserId}; awaiting login OTP verification", user.Id);

                return new LoginResponseDto
                {
                    Success = true,
                    RequiresTwoFactor = true,
                    TwoFactorMethod = "otp",
                    ChallengeId = otpChallenge.ChallengeId,
                    ChallengeExpiresAt = otpChallenge.ExpiresAt,
                    UserId = user.Id,
                    Username = user.UserName,
                    Message = "One-time verification code sent"
                };
            }
        }

        _logger.LogInformation("Successful login for user: {UserId}", user.Id);

        return await _authTokenIssuer.IssueAsync(user, "pwd", cancellationToken);
    }

    public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        // Find the refresh token (verifies hash)
        var refreshTokenEntity = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshTokenEntity == null)
        {
            _logger.LogWarning("Refresh token not found");
            return new RefreshTokenResponseDto
            {
                Success = false,
                Message = "Invalid refresh token"
            };
        }

        // Check if token is active (not revoked and not expired)
        if (!refreshTokenEntity.IsActive)
        {
            _logger.LogWarning("Attempted to use inactive refresh token for user: {UserId}", refreshTokenEntity.UserId);
            return new RefreshTokenResponseDto
            {
                Success = false,
                Message = "Refresh token is no longer valid"
            };
        }

        // Get the user
        var user = await _userRepository.GetByIdAsync(refreshTokenEntity.UserId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("User not found or inactive for refresh token: {UserId}", refreshTokenEntity.UserId);
            return new RefreshTokenResponseDto
            {
                Success = false,
                Message = "User not found or inactive"
            };
        }

        // Atomically consume the refresh token
        // This prevents concurrent replay attacks - only one request will successfully consume the token
        var consumed = await _refreshTokenRepository.ConsumeTokenAsync(refreshTokenEntity.Id, cancellationToken);

        if (!consumed)
        {
            _logger.LogWarning("Failed to consume refresh token for user: {UserId} - possible concurrent use detected", refreshTokenEntity.Id);
            return new RefreshTokenResponseDto
            {
                Success = false,
                Message = "Refresh token has already been used or expired"
            };
        }

        // Token successfully consumed - now generate new tokens
        // Generate new access token

        var newAccessToken = _tokenService.GenerateToken(user.Id, user.UserName, "pwd", user.SecurityStamp);

        // Generate new refresh token (rotating refresh tokens for security)
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiryDays = int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var days) ? days : 7;

        // Hash the refresh token before storing (same security as passwords)
        var newRefreshTokenHash = _passwordHasher.HashPassword(newRefreshToken);

        // Create new refresh token entity
        var newRefreshTokenEntity = new RefreshTokenEntity
        {
            Token = newRefreshTokenHash, // Store hash, not plaintext
            UserId = user.Id,
            ExpiresAt = DateTime.Now.AddDays(refreshTokenExpiryDays),
            IsRevoked = false,
            CreatedBy = user.Id,
            CreatedDate = DateTime.Now
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        // Save all changes to database
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        // Calculate expiration
        var expiresInMinutes = int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var minutes) ? minutes : 60;
        var expiresAt = DateTime.Now.AddMinutes(expiresInMinutes);

        _logger.LogInformation("Token refreshed for user: {UserId}", user.Id);

        return new RefreshTokenResponseDto
        {
            Success = true,
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt,
            Message = "Token refreshed successfully"
        };
    }

    public Task<ValidateSessionResponseDto> ValidateSessionAsync(ValidateSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = _tokenService.ValidateToken(request.AccessToken);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Token validation failed: {ErrorMessage}", validationResult.ErrorMessage);
            return Task.FromResult(new ValidateSessionResponseDto
            {
                IsValid = false,
                Message = validationResult.ErrorMessage ?? "Invalid token"
            });
        }

        _logger.LogInformation("Token validated for user: {UserId}", validationResult.UserId);

        return Task.FromResult(new ValidateSessionResponseDto
        {
            IsValid = true,
            UserId = validationResult.UserId,
            Username = validationResult.Username,
            ExpiresAt = validationResult.ExpiresAt,
            Message = "Token is valid"
        });
    }

    /// <summary>
    /// Logs out a user by revoking their refresh token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Important limitation:</b> This method only revokes the refresh token.
    /// The access token (JWT) remains valid until its natural expiration.
    /// </para>
    /// <para>
    /// Rationale: JWTs are stateless by design. Implementing access token revocation
    /// would require a distributed blocklist check on every authenticated request,
    /// which adds latency and infrastructure complexity.
    /// </para>
    /// <para>
    /// Mitigations in place:
    /// - Short-lived access tokens (configurable via Jwt:ExpiresInMinutes)
    /// - Refresh token rotation prevents long-term session hijacking
    /// - RevokeAllUserTokensAsync can be used for forced logout scenarios
    /// </para>
    /// <para>
    /// Future enhancement: If immediate invalidation is required, implement
    /// access token blocklist using Redis or similar distributed cache.
    /// </para>
    /// </remarks>
    public async Task<LogoutResponseDto> LogoutAsync(LogoutRequestDto request, CancellationToken cancellationToken = default)
    {
        // Revoke only the refresh token - access token remains valid until expiry
        // This is an intentional limitation of stateless JWT architecture
        var refreshTokenEntity = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshTokenEntity == null)
        {
            _logger.LogWarning("Logout attempted with non-existent refresh token");
            return new LogoutResponseDto
            {
                Success = false,
                Message = "Invalid refresh token"
            };
        }

        // Revoke the token
        await _refreshTokenRepository.RevokeTokenAsync(request.RefreshToken, cancellationToken);

        _logger.LogInformation("User logged out: {UserId}", refreshTokenEntity.UserId);

        return new LogoutResponseDto
        {
            Success = true,
            Message = "Logged out successfully"
        };
    }

    /// <inheritdoc />
    public async Task<ChangePasswordResponseDto> ChangePasswordAsync(int? userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return new ChangePasswordResponseDto { Success = false, Message = "Invalid request payload." };
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return new ChangePasswordResponseDto { Success = false, Message = "Current password is required." };
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return new ChangePasswordResponseDto { Success = false, Message = "New password is required." };
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return new ChangePasswordResponseDto { Success = false, Message = "New password and confirmation password do not match." };
        }

        UserEntity? user = null;
        if (userId.HasValue && userId.Value > 0)
        {
            user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            user = await _userRepository.GetByUsernameOrEmailAsync(request.UserName.Trim(), cancellationToken);
        }

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Change password attempted for non-existent or inactive user: {UserIdOrName}", userId?.ToString() ?? request.UserName);
            return new ChangePasswordResponseDto { Success = false, Message = "User not found or account is inactive." };
        }

        // Verify current password
        if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Change password failed for user {UserId}: incorrect current password.", user.Id);
            return new ChangePasswordResponseDto { Success = false, Message = "Current password is incorrect." };
        }

        // Ensure new password is not identical to current password
        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            return new ChangePasswordResponseDto { Success = false, Message = "New password must be different from current password." };
        }

        // Validate password against dynamic security policies
        var validationError = await ValidatePasswordPolicyAsync(request.NewPassword, cancellationToken);
        if (validationError != null)
        {
            return new ChangePasswordResponseDto { Success = false, Message = validationError };
        }

        // Hash new password
        string newPasswordHash;
        try
        {
            newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash new password during change password for user {UserId}", user.Id);
            return new ChangePasswordResponseDto { Success = false, Message = "Unable to process the new password. Please choose a different password." };
        }

        var newSecurityStamp = Guid.NewGuid().ToString("N");
        var updated = await _userRepository.ResetPasswordAsync(user.Id, newPasswordHash, newSecurityStamp, cancellationToken);
        if (!updated)
        {
            _logger.LogError("Failed to update password for user {UserId}", user.Id);
            return new ChangePasswordResponseDto { Success = false, Message = "An error occurred while updating the password." };
        }

        // Revoke all existing refresh tokens for security
        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, cancellationToken);

        _logger.LogInformation("Password changed successfully for user {UserId}", user.Id);

        return new ChangePasswordResponseDto
        {
            Success = true,
            Message = "Password has been changed successfully. Please log in with your new password."
        };
    }

    private async Task<string?> ValidatePasswordPolicyAsync(string password, CancellationToken cancellationToken)
    {
        var minLength = await _securitySettings.GetAsync("MINPASSWORDLENGTH", 6, cancellationToken);
        var maxLength = await _securitySettings.GetAsync("MAXPASSWORDLENGTH", 128, cancellationToken);
        var requireUppercase = await _securitySettings.GetAsync("REQUIREUPPERCASE", false, cancellationToken);
        var requireLowercase = await _securitySettings.GetAsync("REQUIRELOWERCASE", false, cancellationToken);
        var requireDigit = await _securitySettings.GetAsync("REQUIREDIGIT", false, cancellationToken);
        var requireSpecial = await _securitySettings.GetAsync("REQUIRESPECIALCHAR", false, cancellationToken);

        if (minLength < 1) minLength = 6;
        if (maxLength <= minLength || maxLength > 1000) maxLength = 128;

        if (password.Length < minLength)
        {
            return $"Password must be at least {minLength} characters long.";
        }

        if (password.Length > maxLength)
        {
            return $"Password cannot exceed {maxLength} characters.";
        }

        if (requireUppercase && !password.Any(char.IsUpper))
        {
            return "Password must contain at least one uppercase letter (A-Z).";
        }

        if (requireLowercase && !password.Any(char.IsLower))
        {
            return "Password must contain at least one lowercase letter (a-z).";
        }

        if (requireDigit && !password.Any(char.IsDigit))
        {
            return "Password must contain at least one number (0-9).";
        }

        if (requireSpecial && !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return "Password must contain at least one special character.";
        }

        return null;
    }
}
