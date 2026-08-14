using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.DTOs.Email;
using NtisPlatform.Application.DTOs.TwoFactor;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Owns authenticator setup, enable, disable, reset, and recovery-code lifecycle. Does not
/// handle login-time MFA challenges — see <see cref="MfaChallengeService"/>.
/// </summary>
public class TwoFactorAuthenticationService : ITwoFactorAuthenticationService
{
    private static readonly char[] RecoveryCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();
    private const string EnableEmailVerifyPurpose = "2fa-enable-email-verify";
    private const string DefaultCompanyName = "NTIS Platform";

    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorRecoveryCodeRepository _recoveryCodeRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IMfaChallengeRepository _challengeRepository;
    private readonly ITotpService _totpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITwoFactorSecretProtector _secretProtector;
    private readonly ISecurityAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly TwoFactorAuthenticationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TwoFactorAuthenticationService> _logger;

    public TwoFactorAuthenticationService(
        IUserRepository userRepository,
        ITwoFactorRecoveryCodeRepository recoveryCodeRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IMfaChallengeRepository challengeRepository,
        ITotpService totpService,
        IPasswordHasher passwordHasher,
        ITwoFactorSecretProtector secretProtector,
        ISecurityAuditService auditService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<TwoFactorAuthenticationOptions> options,
        TimeProvider timeProvider,
        ILogger<TwoFactorAuthenticationService> logger)
    {
        _userRepository = userRepository;
        _recoveryCodeRepository = recoveryCodeRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _challengeRepository = challengeRepository;
        _totpService = totpService;
        _passwordHasher = passwordHasher;
        _secretProtector = secretProtector;
        _auditService = auditService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TwoFactorStatusResponseDto> GetStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return new TwoFactorStatusResponseDto();
        }

        var recoveryCodesRemaining = user.TwoFactorEnabled
            ? await _recoveryCodeRepository.CountActiveByUserIdAsync(userId, cancellationToken)
            : 0;

        return new TwoFactorStatusResponseDto
        {
            IsEnabled = user.TwoFactorEnabled,
            RecoveryCodesRemaining = recoveryCodesRemaining,
            HasAuthenticatorKey = !string.IsNullOrEmpty(user.TwoFactorSecretEncrypted)
        };
    }

    public async Task<TwoFactorOperationResult<TwoFactorSetupResponseDto>> BeginSetupAsync(int userId, bool isReset, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.UserNotFound);
        }

        if (user.TwoFactorEnabled && !isReset)
        {
            return TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.AlreadyEnabled);
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.EmailNotOnFile);
        }

        var secret = _totpService.GenerateSecret();
        var encryptedSecret = _secretProtector.Protect(secret);
        await _userRepository.SetPendingTwoFactorSecretAsync(userId, encryptedSecret, cancellationToken);

        var authenticatorUri = _totpService.BuildAuthenticatorUri(_options.Issuer, user.UserName, secret);

        await _auditService.RecordAsync(SecurityAuditEventType.TwoFactorSetupStarted, userId, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Two-factor setup started for user {UserId}", userId);

        return TwoFactorOperationResult<TwoFactorSetupResponseDto>.Succeeded(new TwoFactorSetupResponseDto
        {
            SharedKey = FormatSharedKey(secret),
            AuthenticatorUri = authenticatorUri,
            Issuer = _options.Issuer,
            AccountName = user.UserName
        });
    }

    public async Task<TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>> EnableAsync(int userId, string totpCode, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>.Failed(TwoFactorOperationError.UserNotFound);
        }

        if (user.TwoFactorEnabled)
        {
            return TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>.Failed(TwoFactorOperationError.AlreadyEnabled);
        }

        if (string.IsNullOrEmpty(user.TwoFactorSecretEncrypted))
        {
            return TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>.Failed(TwoFactorOperationError.SetupNotStarted);
        }

        // Re-checked here (not just at BeginSetupAsync) in case the email was cleared from the
        // user's profile in between the two steps.
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>.Failed(TwoFactorOperationError.EmailNotOnFile);
        }

        var normalized = TwoFactorCodeNormalizer.NormalizeTotpCode(totpCode);
        if (!TwoFactorCodeNormalizer.IsSixDigits(normalized) || !ValidateTotp(user.TwoFactorSecretEncrypted, normalized))
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, userId, success: false, detail: "EnableSetup", cancellationToken: cancellationToken);
            _logger.LogWarning("Two-factor enable failed for user {UserId}: invalid TOTP code", userId);
            return TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>.Failed(TwoFactorOperationError.InvalidCode);
        }

        // TOTP proven — the caller can operate *some* authenticator app, but that doesn't prove
        // it's bound to this account. Challenge the registered email before finalizing enable.
        var now = _timeProvider.GetLocalNow().DateTime;
        var rawEmailCode = GenerateNumericEmailCode();
        var challenge = new MfaChallengeEntity
        {
            Id = Guid.NewGuid(),
            ChallengeHash = HashChallengeToken(rawEmailCode),
            UserId = userId,
            Purpose = EnableEmailVerifyPurpose,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_options.EmailVerificationLifetimeMinutes),
            FailedAttemptCount = 0
        };
        await _challengeRepository.AddAsync(challenge, cancellationToken);
        await _challengeRepository.SaveChangesAsync(cancellationToken);

        await SendEmailVerificationCodeAsync(user, rawEmailCode, cancellationToken);

        await _auditService.RecordAsync(SecurityAuditEventType.TwoFactorEmailVerificationSent, userId, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Two-factor email verification code sent for user {UserId}", userId);

        return TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>.Succeeded(new TwoFactorEmailVerificationPendingResponseDto
        {
            MaskedEmail = ContactMasking.MaskEmail(user.Email!)
        });
    }

    public async Task<TwoFactorOperationResult<EnableTwoFactorResponseDto>> ConfirmEnableAsync(int userId, string emailCode, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return TwoFactorOperationResult<EnableTwoFactorResponseDto>.Failed(TwoFactorOperationError.UserNotFound);
        }

        if (user.TwoFactorEnabled)
        {
            return TwoFactorOperationResult<EnableTwoFactorResponseDto>.Failed(TwoFactorOperationError.AlreadyEnabled);
        }

        var challenge = await _challengeRepository.GetActiveByUserIdAndPurposeAsync(userId, EnableEmailVerifyPurpose, cancellationToken);
        if (challenge == null)
        {
            // No active challenge to compare against (never requested, already used, or expired)
            // — nothing to record a failed attempt against either, so just report it as invalid.
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, userId, success: false, detail: "EnableEmailNoActiveChallenge", cancellationToken: cancellationToken);
            return TwoFactorOperationResult<EnableTwoFactorResponseDto>.Failed(TwoFactorOperationError.InvalidCode);
        }

        var normalized = NormalizeNumericEmailCode(emailCode);
        if (normalized.Length == 0 || HashChallengeToken(normalized) != challenge.ChallengeHash)
        {
            var outcome = await _challengeRepository.RecordFailedAttemptAsync(challenge.Id, _options.MaximumVerificationAttempts, cancellationToken);
            if (outcome == MfaChallengeFailureOutcome.NowLocked)
            {
                await _userRepository.IncrementOtpChallengeLockoutAsync(userId, cancellationToken);
            }
            var eventType = outcome == MfaChallengeFailureOutcome.NowLocked
                ? SecurityAuditEventType.MfaChallengeLocked
                : SecurityAuditEventType.MfaVerificationFailed;
            await _auditService.RecordAsync(eventType, userId, success: false, detail: "EnableEmail", cancellationToken: cancellationToken);
            return TwoFactorOperationResult<EnableTwoFactorResponseDto>.Failed(TwoFactorOperationError.InvalidCode);
        }

        // Consume before enabling so a losing concurrent request can never also succeed.
        var consumed = await _challengeRepository.TryConsumeAsync(challenge.Id, cancellationToken);
        if (!consumed)
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, userId, success: false, detail: "EnableEmailConcurrentConsume", cancellationToken: cancellationToken);
            return TwoFactorOperationResult<EnableTwoFactorResponseDto>.Failed(TwoFactorOperationError.InvalidCode);
        }

        var newStamp = NewSecurityStamp();
        var enabled = await _userRepository.EnableTwoFactorAsync(userId, newStamp, cancellationToken);
        if (!enabled)
        {
            return TwoFactorOperationResult<EnableTwoFactorResponseDto>.Failed(TwoFactorOperationError.UserNotFound);
        }

        var recoveryCodes = await IssueRecoveryCodesAsync(userId, cancellationToken);

        await _userRepository.ResetOtpChallengeLockoutAsync(userId, cancellationToken);
        await _auditService.RecordAsync(SecurityAuditEventType.TwoFactorEnabled, userId, success: true, cancellationToken: cancellationToken);
        await _auditService.RecordAsync(SecurityAuditEventType.TwoFactorEmailVerificationConfirmed, userId, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Two-factor authentication enabled for user {UserId} (email-verified)", userId);

        return TwoFactorOperationResult<EnableTwoFactorResponseDto>.Succeeded(new EnableTwoFactorResponseDto
        {
            IsEnabled = true,
            RecoveryCodes = recoveryCodes
        });
    }

    public async Task<TwoFactorOperationResult<RecoveryCodesResponseDto>> RegenerateRecoveryCodesAsync(int userId, string verificationCode, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return TwoFactorOperationResult<RecoveryCodesResponseDto>.Failed(TwoFactorOperationError.UserNotFound);
        }

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretEncrypted))
        {
            return TwoFactorOperationResult<RecoveryCodesResponseDto>.Failed(TwoFactorOperationError.NotEnabled);
        }

        var normalized = TwoFactorCodeNormalizer.NormalizeTotpCode(verificationCode);
        if (!TwoFactorCodeNormalizer.IsSixDigits(normalized) || !ValidateTotp(user.TwoFactorSecretEncrypted, normalized))
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, userId, success: false, detail: "RegenerateRecoveryCodes", cancellationToken: cancellationToken);
            return TwoFactorOperationResult<RecoveryCodesResponseDto>.Failed(TwoFactorOperationError.InvalidCode);
        }

        await _recoveryCodeRepository.RevokeAllActiveAsync(userId, cancellationToken);
        var recoveryCodes = await IssueRecoveryCodesAsync(userId, cancellationToken);

        await _auditService.RecordAsync(SecurityAuditEventType.RecoveryCodesRegenerated, userId, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Recovery codes regenerated for user {UserId}", userId);

        return TwoFactorOperationResult<RecoveryCodesResponseDto>.Succeeded(new RecoveryCodesResponseDto
        {
            RecoveryCodes = recoveryCodes
        });
    }

    public async Task<TwoFactorOperationResult<bool>> DisableAsync(int userId, string verificationCode, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return TwoFactorOperationResult<bool>.Failed(TwoFactorOperationError.UserNotFound);
        }

        if (!user.TwoFactorEnabled)
        {
            return TwoFactorOperationResult<bool>.Failed(TwoFactorOperationError.NotEnabled);
        }

        if (!await VerifyTotpOrRecoveryCodeAsync(user, verificationCode, cancellationToken))
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, userId, success: false, detail: "Disable", cancellationToken: cancellationToken);
            return TwoFactorOperationResult<bool>.Failed(TwoFactorOperationError.InvalidCode);
        }

        var newStamp = NewSecurityStamp();
        await _userRepository.DisableTwoFactorAsync(userId, newStamp, cancellationToken);
        await _recoveryCodeRepository.RevokeAllActiveAsync(userId, cancellationToken);
        await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, cancellationToken);

        await _auditService.RecordAsync(SecurityAuditEventType.TwoFactorDisabled, userId, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Two-factor authentication disabled for user {UserId}", userId);

        return TwoFactorOperationResult<bool>.Succeeded(true);
    }

    public async Task<TwoFactorOperationResult<TwoFactorSetupResponseDto>> ResetAsync(int userId, string verificationCode, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.UserNotFound);
        }

        if (!user.TwoFactorEnabled)
        {
            return TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.NotEnabled);
        }

        if (!await VerifyTotpOrRecoveryCodeAsync(user, verificationCode, cancellationToken))
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, userId, success: false, detail: "Reset", cancellationToken: cancellationToken);
            return TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.InvalidCode);
        }

        var newStamp = NewSecurityStamp();
        await _userRepository.DisableTwoFactorAsync(userId, newStamp, cancellationToken);
        await _recoveryCodeRepository.RevokeAllActiveAsync(userId, cancellationToken);
        await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, cancellationToken);

        await _auditService.RecordAsync(SecurityAuditEventType.TwoFactorReset, userId, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Two-factor authenticator reset for user {UserId}", userId);

        return await BeginSetupAsync(userId, isReset: true, cancellationToken: cancellationToken);
    }

    private bool ValidateTotp(string encryptedSecret, string normalizedCode)
    {
        var secret = _secretProtector.Unprotect(encryptedSecret);
        return _totpService.ValidateCode(secret, normalizedCode, _timeProvider.GetLocalNow());
    }

    private async Task<bool> VerifyTotpOrRecoveryCodeAsync(UserEntity user, string rawCode, CancellationToken cancellationToken)
    {
        var normalizedTotp = TwoFactorCodeNormalizer.NormalizeTotpCode(rawCode);
        if (TwoFactorCodeNormalizer.IsSixDigits(normalizedTotp)
            && !string.IsNullOrEmpty(user.TwoFactorSecretEncrypted)
            && ValidateTotp(user.TwoFactorSecretEncrypted, normalizedTotp))
        {
            return true;
        }

        return await TryRedeemRecoveryCodeAsync(user.Id, rawCode, cancellationToken);
    }

    private async Task<bool> TryRedeemRecoveryCodeAsync(int userId, string rawCode, CancellationToken cancellationToken)
    {
        var normalized = TwoFactorCodeNormalizer.NormalizeRecoveryCode(rawCode);
        if (normalized.Length == 0)
        {
            return false;
        }

        var activeCodes = await _recoveryCodeRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        foreach (var candidate in activeCodes)
        {
            if (!_passwordHasher.VerifyPassword(normalized, candidate.CodeHash))
            {
                continue;
            }

            var redeemed = await _recoveryCodeRepository.TryRedeemAsync(candidate.Id, cancellationToken);
            if (redeemed)
            {
                await _auditService.RecordAsync(SecurityAuditEventType.RecoveryCodeUsed, userId, success: true, cancellationToken: cancellationToken);
            }

            return redeemed;
        }

        return false;
    }

    private async Task<IReadOnlyCollection<string>> IssueRecoveryCodesAsync(int userId, CancellationToken cancellationToken)
    {
        var plaintextCodes = new List<string>(_options.RecoveryCodeCount);

        for (var i = 0; i < _options.RecoveryCodeCount; i++)
        {
            var code = GenerateRecoveryCode();
            plaintextCodes.Add(FormatRecoveryCode(code));

            await _recoveryCodeRepository.AddAsync(new TwoFactorRecoveryCodeEntity
            {
                UserId = userId,
                CodeHash = _passwordHasher.HashPassword(code)
            }, cancellationToken);
        }

        await _recoveryCodeRepository.SaveChangesAsync(cancellationToken);

        return plaintextCodes;
    }

    private static string GenerateRecoveryCode()
    {
        const int length = 10;
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = RecoveryCodeAlphabet[bytes[i] % RecoveryCodeAlphabet.Length];
        }

        return new string(chars);
    }

    private static string FormatRecoveryCode(string rawCode) => $"{rawCode[..5]}-{rawCode[5..]}";

    private static string FormatSharedKey(string secret)
    {
        var groups = new List<string>();
        for (var i = 0; i < secret.Length; i += 4)
        {
            groups.Add(secret.Substring(i, Math.Min(4, secret.Length - i)));
        }

        return string.Join(' ', groups);
    }

    private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Generates a 6-digit numeric code, formatted for humans to type back in from an email —
    /// deliberately not the recovery-code alphabet/format, so the two can't be visually confused.
    /// </summary>
    private static string GenerateNumericEmailCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }

    private static string NormalizeNumericEmailCode(string rawCode) =>
        new(rawCode.Where(char.IsDigit).ToArray());

    private static string HashChallengeToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }

    private async Task SendEmailVerificationCodeAsync(UserEntity user, string rawEmailCode, CancellationToken cancellationToken)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", user.UserName },
            { "VerificationCode", rawEmailCode },
            { "ExpiryMinutes", _options.EmailVerificationLifetimeMinutes.ToString() },
            { "CompanyName", DefaultCompanyName }
        };

        var emailBody = await _emailTemplateService.GetTemplateAsync("TwoFactorEmailVerification", placeholders, cancellationToken);

        var emailRequest = new EmailRequest
        {
            ToEmail = user.Email!,
            ToName = $"{user.FirstName} {user.LastName}".Trim(),
            Subject = "Confirm Two-Factor Authentication Setup",
            Body = emailBody,
            IsHtml = true
        };

        await _emailService.SendEmailAsync(emailRequest, cancellationToken);
    }
}
