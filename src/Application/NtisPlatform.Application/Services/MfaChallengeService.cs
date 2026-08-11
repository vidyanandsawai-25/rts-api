using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Manages the short-lived, one-time-use MFA login challenge issued after a successful password
/// check for a user with two-factor authentication enabled.
/// </summary>
public class MfaChallengeService : IMfaChallengeService
{
    private const string LoginChallengePurpose = "mfa-login";
    private const int ChallengeTokenBytes = 32;

    private readonly IMfaChallengeRepository _challengeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorRecoveryCodeRepository _recoveryCodeRepository;
    private readonly ITotpService _totpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITwoFactorSecretProtector _secretProtector;
    private readonly IAuthTokenIssuerService _authTokenIssuer;
    private readonly ISecurityAuditService _auditService;
    private readonly TwoFactorAuthenticationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MfaChallengeService> _logger;

    public MfaChallengeService(
        IMfaChallengeRepository challengeRepository,
        IUserRepository userRepository,
        ITwoFactorRecoveryCodeRepository recoveryCodeRepository,
        ITotpService totpService,
        IPasswordHasher passwordHasher,
        ITwoFactorSecretProtector secretProtector,
        IAuthTokenIssuerService authTokenIssuer,
        ISecurityAuditService auditService,
        IOptions<TwoFactorAuthenticationOptions> options,
        TimeProvider timeProvider,
        ILogger<MfaChallengeService> logger)
    {
        _challengeRepository = challengeRepository;
        _userRepository = userRepository;
        _recoveryCodeRepository = recoveryCodeRepository;
        _totpService = totpService;
        _passwordHasher = passwordHasher;
        _secretProtector = secretProtector;
        _authTokenIssuer = authTokenIssuer;
        _auditService = auditService;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<MfaLoginChallenge> CreateLoginChallengeAsync(
        int userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.ChallengeLifetimeMinutes);

        var rawToken = GenerateChallengeToken();
        var challenge = new MfaChallengeEntity
        {
            Id = Guid.NewGuid(),
            ChallengeHash = HashChallengeToken(rawToken),
            UserId = userId,
            Purpose = LoginChallengePurpose,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            FailedAttemptCount = 0,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _challengeRepository.AddAsync(challenge, cancellationToken);
        await _challengeRepository.SaveChangesAsync(cancellationToken);

        return new MfaLoginChallenge(rawToken, expiresAt);
    }

    public async Task<MfaVerificationResult> VerifyLoginChallengeAsync(
        string challengeId,
        string code,
        bool useRecoveryCode,
        CancellationToken cancellationToken = default)
    {
        var challengeHash = HashChallengeToken(challengeId);
        var challenge = await _challengeRepository.GetByHashAsync(challengeHash, cancellationToken);

        if (challenge == null || challenge.Purpose != LoginChallengePurpose)
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, null, success: false, detail: "UnknownChallenge", cancellationToken: cancellationToken);
            return MfaVerificationResult.Failed(MfaVerificationFailureReason.InvalidChallenge);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (challenge.RevokedAt != null)
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, challenge.UserId, success: false, detail: "ChallengeRevoked", cancellationToken: cancellationToken);
            return MfaVerificationResult.Failed(MfaVerificationFailureReason.ChallengeLocked);
        }

        if (challenge.ConsumedAt != null)
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, challenge.UserId, success: false, detail: "ChallengeConsumed", cancellationToken: cancellationToken);
            return MfaVerificationResult.Failed(MfaVerificationFailureReason.ChallengeConsumed);
        }

        if (challenge.ExpiresAt <= now)
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaChallengeExpired, challenge.UserId, success: false, cancellationToken: cancellationToken);
            return MfaVerificationResult.Failed(MfaVerificationFailureReason.ChallengeExpired);
        }

        var user = await _userRepository.GetByIdAsync(challenge.UserId, cancellationToken);
        if (user == null || !user.IsActive || !user.TwoFactorEnabled)
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, challenge.UserId, success: false, detail: "UserNotEligible", cancellationToken: cancellationToken);
            return MfaVerificationResult.Failed(MfaVerificationFailureReason.InvalidChallenge);
        }

        var codeIsValid = useRecoveryCode
            ? await TryRedeemRecoveryCodeAsync(user.Id, code, cancellationToken)
            : ValidateTotp(user, code);

        if (!codeIsValid)
        {
            var outcome = await _challengeRepository.RecordFailedAttemptAsync(challenge.Id, _options.MaximumVerificationAttempts, cancellationToken);

            if (outcome == MfaChallengeFailureOutcome.NowLocked)
            {
                await _auditService.RecordAsync(SecurityAuditEventType.MfaChallengeLocked, user.Id, success: false, cancellationToken: cancellationToken);
                _logger.LogWarning("MFA challenge locked for user {UserId} after too many failed attempts", user.Id);
                return MfaVerificationResult.Failed(MfaVerificationFailureReason.ChallengeLocked);
            }

            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, user.Id, success: false, cancellationToken: cancellationToken);
            _logger.LogWarning("MFA verification failed for user {UserId}", user.Id);
            return MfaVerificationResult.Failed(MfaVerificationFailureReason.InvalidCode);
        }

        // Consume before issuing tokens so a losing concurrent request can never also succeed.
        var consumed = await _challengeRepository.TryConsumeAsync(challenge.Id, cancellationToken);
        if (!consumed)
        {
            await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationFailed, user.Id, success: false, detail: "ConcurrentConsume", cancellationToken: cancellationToken);
            return MfaVerificationResult.Failed(MfaVerificationFailureReason.ChallengeConsumed);
        }

        await _userRepository.ResetFailedLoginCountAsync(user.Id, cancellationToken);

        var loginResponse = await _authTokenIssuer.IssueAsync(user, "mfa", cancellationToken);

        await _auditService.RecordAsync(SecurityAuditEventType.MfaVerificationSucceeded, user.Id, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("MFA verification succeeded for user {UserId}", user.Id);

        return MfaVerificationResult.Succeeded(loginResponse);
    }

    private bool ValidateTotp(UserEntity user, string rawCode)
    {
        if (string.IsNullOrEmpty(user.TwoFactorSecretEncrypted))
        {
            return false;
        }

        var normalized = TwoFactorCodeNormalizer.NormalizeTotpCode(rawCode);
        if (!TwoFactorCodeNormalizer.IsSixDigits(normalized))
        {
            return false;
        }

        var secret = _secretProtector.Unprotect(user.TwoFactorSecretEncrypted);
        return _totpService.ValidateCode(secret, normalized, _timeProvider.GetUtcNow());
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

    private static string GenerateChallengeToken()
    {
        var bytes = new byte[ChallengeTokenBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashChallengeToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }
}
