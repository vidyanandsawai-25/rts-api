using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.DTOs.PasswordReset;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <inheritdoc cref="IPasswordResetService"/>
public class PasswordResetService : IPasswordResetService
{
    private const string AuthenticatorChannel = "Authenticator";
    private const string GenericSentMessage = "If an account matching that information exists, a verification code has been sent.";
    private const string FeatureUnavailableMessage = "Self-service password reset is not available. Please contact your administrator.";
    private const string NoMethodsAvailableMessage = "No verification methods are available for this account. Please contact your administrator.";
    private const string InvalidOrExpiredMessage = "Invalid or expired verification code.";

    private readonly IUserRepository _userRepository;
    private readonly IOtpChallengeService _otpChallengeService;
    private readonly IMfaChallengeRepository _challengeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISecuritySettingsService _securitySettings;
    private readonly ISecurityAuditService _auditService;
    private readonly ITotpService _totpService;
    private readonly ITwoFactorSecretProtector _secretProtector;
    private readonly OtpChallengeOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IUserRepository userRepository,
        IOtpChallengeService otpChallengeService,
        IMfaChallengeRepository challengeRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        ISecuritySettingsService securitySettings,
        ISecurityAuditService auditService,
        ITotpService totpService,
        ITwoFactorSecretProtector secretProtector,
        IOptions<OtpChallengeOptions> options,
        TimeProvider timeProvider,
        ILogger<PasswordResetService> logger)
    {
        _userRepository = userRepository;
        _otpChallengeService = otpChallengeService;
        _challengeRepository = challengeRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _securitySettings = securitySettings;
        _auditService = auditService;
        _totpService = totpService;
        _secretProtector = secretProtector;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ForgotPasswordAvailableMethodsResponseDto> GetAvailableMethodsAsync(ForgotPasswordAvailableMethodsRequestDto request, CancellationToken cancellationToken = default)
    {
        var featureEnabled = await _securitySettings.GetAsync("2FALOGINFORFPASS", false, cancellationToken);
        if (!featureEnabled)
        {
            return new ForgotPasswordAvailableMethodsResponseDto { Success = true, Methods = new List<string>(), Message = FeatureUnavailableMessage };
        }

        var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail, cancellationToken);
        var methods = await ComputeAvailableMethodsAsync(user, cancellationToken);

        if (methods.Count == 0)
        {
            // Same response whether the account doesn't exist or simply has no usable channel —
            // never reveal which case it was.
            return new ForgotPasswordAvailableMethodsResponseDto { Success = true, Methods = methods, Message = NoMethodsAvailableMessage };
        }

        return new ForgotPasswordAvailableMethodsResponseDto
        {
            Success = true,
            Methods = methods,
            MaskedEmail = methods.Contains(ForgotPasswordMethodNames.Email) ? ContactMasking.MaskEmail(user!.Email!) : null,
            MaskedMobile = methods.Contains(ForgotPasswordMethodNames.Sms) ? ContactMasking.MaskMobile(user!.MobileNo!) : null
        };
    }

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var featureEnabled = await _securitySettings.GetAsync("2FALOGINFORFPASS", false, cancellationToken);
        if (!featureEnabled)
        {
            return new ForgotPasswordResponseDto { Success = false, Message = FeatureUnavailableMessage };
        }

        var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail, cancellationToken);
        var availableMethods = await ComputeAvailableMethodsAsync(user, cancellationToken);

        // Never reveal whether the account exists, which channels it has, or that the client sent
        // a method that isn't actually valid for it — same generic response for all of these. The
        // client-selected method is never trusted blindly; it's re-checked against the same
        // availability computation used by GetAvailableMethodsAsync.
        if (user == null || !availableMethods.Contains(request.Method, StringComparer.OrdinalIgnoreCase))
        {
            if (user != null)
            {
                _logger.LogWarning("Forgot-password requested for user {UserId} with method '{Method}', which is not available for this account.", user.Id, request.Method);
            }

            return new ForgotPasswordResponseDto { Success = true, Message = GenericSentMessage };
        }

        var method = request.Method.Trim();
        var creation = string.Equals(method, ForgotPasswordMethodNames.Authenticator, StringComparison.OrdinalIgnoreCase)
            ? await CreateAuthenticatorChallengeAsync(user, cancellationToken)
            : await _otpChallengeService.CreateAsync(
                user,
                OtpChallengePurpose.ForgotPasswordOtp,
                sendEmail: string.Equals(method, ForgotPasswordMethodNames.Email, StringComparison.OrdinalIgnoreCase),
                sendSms: string.Equals(method, ForgotPasswordMethodNames.Sms, StringComparison.OrdinalIgnoreCase),
                ipAddress: null,
                userAgent: null,
                cancellationToken);

        if (!creation.Success)
        {
            // Account is throttled — same generic response as "account doesn't exist" above.
            // Must not reveal throttling state, or an attacker could use it to enumerate accounts.
            _logger.LogWarning("Forgot-password challenge suppressed for user {UserId}: account is throttled.", user.Id);
            return new ForgotPasswordResponseDto { Success = true, Message = GenericSentMessage };
        }

        var challenge = creation.Challenge!;

        await _auditService.RecordAsync(SecurityAuditEventType.ForgotPasswordOtpRequested, user.Id, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Forgot-password challenge created for user {UserId} via '{Method}'", user.Id, method);

        return new ForgotPasswordResponseDto
        {
            Success = true,
            Message = GenericSentMessage,
            ChallengeId = challenge.ChallengeId,
            ChallengeExpiresAt = challenge.ExpiresAt
        };
    }

    /// <summary>
    /// Computes which forgot-password OTP methods (<see cref="ForgotPasswordMethodNames"/>) are
    /// actually usable for this account, given the SECURITY_AUTH config flags and the account's
    /// own contact/enrollment data. Shared by <see cref="GetAvailableMethodsAsync"/> and
    /// <see cref="ForgotPasswordAsync"/> (which re-derives this rather than trusting the client).
    /// </summary>
    private async Task<List<string>> ComputeAvailableMethodsAsync(UserEntity? user, CancellationToken cancellationToken)
    {
        var methods = new List<string>();
        if (user == null)
        {
            return methods;
        }

        if (!string.IsNullOrWhiteSpace(user.Email) && await _securitySettings.GetAsync("FPASSOTPMAIL", false, cancellationToken))
        {
            methods.Add(ForgotPasswordMethodNames.Email);
        }

        if (!string.IsNullOrWhiteSpace(user.MobileNo) && await _securitySettings.GetAsync("FPASSOTPONSMS", false, cancellationToken))
        {
            methods.Add(ForgotPasswordMethodNames.Sms);
        }

        if (user.TwoFactorEnabled)
        {
            methods.Add(ForgotPasswordMethodNames.Authenticator);
        }

        return methods;
    }

    /// <summary>
    /// Mints a forgot-password challenge for the authenticator-app method: no code is generated
    /// or sent — the user's already-enrolled TOTP app is the source of truth. Mirrors
    /// <c>MfaChallengeService.CreateLoginChallengeAsync</c>, bypassing <see cref="IOtpChallengeService"/>
    /// since there is no code to hash or deliver.
    /// </summary>
    private async Task<OtpChallengeCreationResult> CreateAuthenticatorChallengeAsync(UserEntity user, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        if (user.OtpChallengeLockedUntilAt is { } lockedUntil && lockedUntil > now)
        {
            return OtpChallengeCreationResult.Failed(ChallengeCreationFailureReason.AccountThrottled);
        }

        var expiresAt = now.AddMinutes(_options.LifetimeMinutes);
        var rawChallengeId = ChallengeTokenHasher.GenerateToken();

        var challenge = new MfaChallengeEntity
        {
            Id = Guid.NewGuid(),
            ChallengeHash = ChallengeTokenHasher.HashToken(rawChallengeId),
            UserId = user.Id,
            Purpose = OtpChallengePurpose.ForgotPasswordOtp,
            Channel = AuthenticatorChannel,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            FailedAttemptCount = 0
        };

        await _challengeRepository.AddAsync(challenge, cancellationToken);
        await _challengeRepository.SaveChangesAsync(cancellationToken);

        return OtpChallengeCreationResult.Succeeded(new OtpChallengeResult(rawChallengeId, expiresAt));
    }

    public async Task<VerifyForgotPasswordOtpResponseDto> VerifyForgotPasswordOtpAsync(VerifyForgotPasswordOtpRequestDto request, CancellationToken cancellationToken = default)
    {
        var challengeHash = ChallengeTokenHasher.HashToken(request.ChallengeId);
        var challenge = await _challengeRepository.GetByHashAsync(challengeHash, cancellationToken);

        if (challenge == null || challenge.Purpose != OtpChallengePurpose.ForgotPasswordOtp)
        {
            return new VerifyForgotPasswordOtpResponseDto { Success = false, Message = InvalidOrExpiredMessage };
        }

        int userId;
        if (string.Equals(challenge.Channel, AuthenticatorChannel, StringComparison.OrdinalIgnoreCase))
        {
            var verified = await VerifyAuthenticatorChallengeAsync(challenge, request.Code, cancellationToken);
            if (!verified)
            {
                _logger.LogWarning("Forgot-password authenticator verification failed for user {UserId}", challenge.UserId);
                return new VerifyForgotPasswordOtpResponseDto { Success = false, Message = InvalidOrExpiredMessage };
            }

            userId = challenge.UserId;
        }
        else
        {
            var result = await _otpChallengeService.VerifyAsync(
                request.ChallengeId, OtpChallengePurpose.ForgotPasswordOtp, request.Code, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("Forgot-password OTP verification failed: {Reason}", result.FailureReason);
                return new VerifyForgotPasswordOtpResponseDto { Success = false, Message = InvalidOrExpiredMessage };
            }

            userId = result.UserId;
        }

        var now = _timeProvider.GetLocalNow().DateTime;
        var expiresAt = now.AddMinutes(_options.PasswordResetTokenLifetimeMinutes);
        var rawResetToken = ChallengeTokenHasher.GenerateToken();

        var resetChallenge = new MfaChallengeEntity
        {
            Id = Guid.NewGuid(),
            ChallengeHash = ChallengeTokenHasher.HashToken(rawResetToken),
            UserId = userId,
            Purpose = OtpChallengePurpose.PasswordReset,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            FailedAttemptCount = 0
        };

        await _challengeRepository.AddAsync(resetChallenge, cancellationToken);
        await _challengeRepository.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(SecurityAuditEventType.ForgotPasswordOtpVerified, userId, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Forgot-password OTP verified for user {UserId}; reset token issued", userId);

        return new VerifyForgotPasswordOtpResponseDto
        {
            Success = true,
            ResetToken = rawResetToken,
            ResetTokenExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Verifies a code against a forgot-password challenge whose <c>Channel</c> is "Authenticator":
    /// no <c>CodeHash</c> exists for these rows, the code is validated against the user's TOTP
    /// secret instead — same technique as <c>MfaChallengeService.ValidateTotp</c> for login.
    /// </summary>
    private async Task<bool> VerifyAuthenticatorChallengeAsync(MfaChallengeEntity challenge, string code, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        if (challenge.RevokedAt != null || challenge.ConsumedAt != null || challenge.ExpiresAt <= now)
        {
            return false;
        }

        var user = await _userRepository.GetByIdAsync(challenge.UserId, cancellationToken);
        if (user == null || !user.IsActive || !user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretEncrypted))
        {
            return false;
        }

        var normalized = TwoFactorCodeNormalizer.NormalizeTotpCode(code);
        var codeIsValid = TwoFactorCodeNormalizer.IsSixDigits(normalized) &&
            _totpService.ValidateCode(_secretProtector.Unprotect(user.TwoFactorSecretEncrypted), normalized, _timeProvider.GetLocalNow());

        if (!codeIsValid)
        {
            var outcome = await _challengeRepository.RecordFailedAttemptAsync(challenge.Id, _options.MaximumVerificationAttempts, cancellationToken);
            if (outcome == MfaChallengeFailureOutcome.NowLocked)
            {
                await _userRepository.IncrementOtpChallengeLockoutAsync(challenge.UserId, cancellationToken);
                _logger.LogWarning("Forgot-password authenticator challenge locked for user {UserId} after too many failed attempts", challenge.UserId);
            }

            return false;
        }

        var consumed = await _challengeRepository.TryConsumeAsync(challenge.Id, cancellationToken);
        if (consumed)
        {
            await _userRepository.ResetOtpChallengeLockoutAsync(challenge.UserId, cancellationToken);
        }

        return consumed;
    }

    public async Task<ResetPasswordResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var challengeHash = ChallengeTokenHasher.HashToken(request.ResetToken);
        var challenge = await _challengeRepository.GetByHashAsync(challengeHash, cancellationToken);

        if (challenge == null || challenge.Purpose != OtpChallengePurpose.PasswordReset)
        {
            return new ResetPasswordResponseDto { Success = false, Message = InvalidOrExpiredMessage };
        }

        var now = _timeProvider.GetLocalNow().DateTime;
        if (challenge.RevokedAt != null || challenge.ConsumedAt != null || challenge.ExpiresAt <= now)
        {
            return new ResetPasswordResponseDto { Success = false, Message = InvalidOrExpiredMessage };
        }

        // Hash before consuming the token: hashing can throw for pathological inputs (e.g. bcrypt
        // rejects passwords over 72 bytes), and a thrown exception must not burn the one-time
        // token — the user should be able to retry with a valid password on the same token.
        string newPasswordHash;
        try
        {
            newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password hashing failed during reset for user {UserId}", challenge.UserId);
            return new ResetPasswordResponseDto { Success = false, Message = "The provided password is not valid. Please choose a different password." };
        }

        var consumed = await _challengeRepository.TryConsumeAsync(challenge.Id, cancellationToken);
        if (!consumed)
        {
            return new ResetPasswordResponseDto { Success = false, Message = InvalidOrExpiredMessage };
        }

        var newSecurityStamp = Guid.NewGuid().ToString("N");

        var updated = await _userRepository.ResetPasswordAsync(challenge.UserId, newPasswordHash, newSecurityStamp, cancellationToken);
        if (!updated)
        {
            _logger.LogError("Password reset token verified for user {UserId} but user no longer exists.", challenge.UserId);
            return new ResetPasswordResponseDto { Success = false, Message = InvalidOrExpiredMessage };
        }

        await _refreshTokenRepository.RevokeAllUserTokensAsync(challenge.UserId, cancellationToken);
        await _auditService.RecordAsync(SecurityAuditEventType.PasswordResetCompleted, challenge.UserId, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Password reset completed for user {UserId}", challenge.UserId);

        return new ResetPasswordResponseDto { Success = true, Message = "Password has been reset successfully." };
    }
}
