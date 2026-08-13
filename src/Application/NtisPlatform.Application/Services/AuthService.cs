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
            // Increment failed login count
            await _userRepository.IncrementFailedLoginCountAsync(user.Id, cancellationToken);
            _logger.LogWarning("Failed login attempt for user: {UserId}", user.Id);
            return new LoginResponseDto { Success = false, Message = "Invalid username or password" };
        }

        // Check if user must change password
        if (user.MustChangePassword)
        {
            _logger.LogInformation("User {UserId} must change password before proceeding", user.Id);
            return new LoginResponseDto
            {
                Success = false,
                Message = "You must change your password before logging in. Please contact administrator.",
                RequiresPasswordChange = true
            };
        }

        // Password valid - reset failed login count and update last login
        await _userRepository.ResetFailedLoginCountAsync(user.Id, cancellationToken);
        await _userRepository.UpdateLastLoginAsync(user.Id, cancellationToken);

        // 2FA-enabled accounts do not get an access/refresh token yet — a short-lived challenge
        // must be verified first via IMfaChallengeService.
        if (user.TwoFactorEnabled)
        {
            var challenge = await _mfaChallengeService.CreateLoginChallengeAsync(user.Id, ipAddress: null, userAgent: null, cancellationToken);

            _logger.LogInformation("Password verified for user {UserId}; awaiting MFA verification", user.Id);

            return new LoginResponseDto
            {
                Success = true,
                RequiresTwoFactor = true,
                TwoFactorMethod = "totp",
                ChallengeId = challenge.ChallengeId,
                ChallengeExpiresAtUtc = challenge.ExpiresAtUtc,
                UserId = user.Id,
                Username = user.UserName,
                Message = "Two-factor authentication code required"
            };
        }

        // No per-user TOTP enrollment — fall back to the config-driven (SECURITY_AUTH) OTP layer
        // if the organization has switched it on. This makes 2FA mandatory org-wide instead of
        // leaving unenrolled users with no second factor at all.
        if (await _securitySettings.GetAsync("2FALOGIN", false, cancellationToken))
        {
            var sendEmail = await _securitySettings.GetAsync("LOGINOTPONMAIL", false, cancellationToken);
            var sendSms = await _securitySettings.GetAsync("LoginOtpOnSms", false, cancellationToken);

            var otpChallenge = await _otpChallengeService.CreateAsync(
                user, OtpChallengePurpose.LoginOtp, sendEmail, sendSms, ipAddress: null, userAgent: null, cancellationToken);

            _logger.LogInformation("Password verified for user {UserId}; awaiting login OTP verification", user.Id);

            return new LoginResponseDto
            {
                Success = true,
                RequiresTwoFactor = true,
                TwoFactorMethod = "otp",
                ChallengeId = otpChallenge.ChallengeId,
                ChallengeExpiresAtUtc = otpChallenge.ExpiresAtUtc,
                UserId = user.Id,
                Username = user.UserName,
                Message = "One-time verification code sent"
            };
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
}
