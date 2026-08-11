using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.DTOs.Email;
using NtisPlatform.Application.DTOs.Sms;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Generic primitive for issuing and verifying short-lived, one-time numeric codes delivered by
/// email and/or SMS. Backs the config-driven login OTP (<c>2FALOGIN</c>) and forgot-password OTP
/// (<c>2FALOGINFORFPASS</c>) flows. Reuses the same <c>CORE.TwoFactorChallenge</c> table as
/// <see cref="MfaChallengeService"/>, partitioned by <see cref="OtpChallengePurpose"/>.
/// </summary>
public class OtpChallengeService : IOtpChallengeService
{
    private const string DefaultCompanyName = "NTIS Platform";

    private readonly IMfaChallengeRepository _challengeRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ISmsService _smsService;
    private readonly OtpChallengeOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OtpChallengeService> _logger;

    public OtpChallengeService(
        IMfaChallengeRepository challengeRepository,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ISmsService smsService,
        IOptions<OtpChallengeOptions> options,
        TimeProvider timeProvider,
        ILogger<OtpChallengeService> logger)
    {
        _challengeRepository = challengeRepository;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _smsService = smsService;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<OtpChallengeResult> CreateAsync(
        UserEntity user,
        string purpose,
        bool sendEmail,
        bool sendSms,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (!sendEmail && !sendSms)
        {
            _logger.LogError(
                "OTP challenge requested for user {UserId}, purpose '{Purpose}', but neither email nor SMS delivery is enabled. This is a configuration error.",
                user.Id, purpose);
            throw new InvalidOperationException(
                $"Cannot create an OTP challenge for purpose '{purpose}': no delivery channel is enabled. Check the corresponding SECURITY_AUTH config flags.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.LifetimeMinutes);

        var rawChallengeId = ChallengeTokenHasher.GenerateToken();
        var rawCode = ChallengeTokenHasher.GenerateNumericCode();
        var channel = sendEmail && sendSms ? "Email,Sms" : sendEmail ? "Email" : "Sms";

        var challenge = new MfaChallengeEntity
        {
            Id = Guid.NewGuid(),
            ChallengeHash = ChallengeTokenHasher.HashToken(rawChallengeId),
            CodeHash = ChallengeTokenHasher.HashToken(rawCode),
            UserId = user.Id,
            Purpose = purpose,
            Channel = channel,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            FailedAttemptCount = 0,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _challengeRepository.AddAsync(challenge, cancellationToken);
        await _challengeRepository.SaveChangesAsync(cancellationToken);

        if (sendEmail)
        {
            await SendEmailCodeAsync(user, rawCode, purpose, cancellationToken);
        }

        if (sendSms)
        {
            await SendSmsCodeAsync(user, rawCode, purpose, cancellationToken);
        }

        _logger.LogInformation("OTP challenge created for user {UserId}, purpose '{Purpose}', channel '{Channel}'", user.Id, purpose, channel);

        return new OtpChallengeResult(rawChallengeId, expiresAt);
    }

    public async Task<OtpVerificationResult> VerifyAsync(
        string challengeId,
        string purpose,
        string code,
        CancellationToken cancellationToken = default)
    {
        var challengeHash = ChallengeTokenHasher.HashToken(challengeId);
        var challenge = await _challengeRepository.GetByHashAsync(challengeHash, cancellationToken);

        if (challenge == null || challenge.Purpose != purpose)
        {
            return OtpVerificationResult.Failed(OtpVerificationFailureReason.InvalidChallenge);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (challenge.RevokedAt != null)
        {
            return OtpVerificationResult.Failed(OtpVerificationFailureReason.ChallengeLocked);
        }

        if (challenge.ConsumedAt != null)
        {
            return OtpVerificationResult.Failed(OtpVerificationFailureReason.ChallengeConsumed);
        }

        if (challenge.ExpiresAt <= now)
        {
            return OtpVerificationResult.Failed(OtpVerificationFailureReason.ChallengeExpired);
        }

        var normalizedCode = new string(code.Where(char.IsDigit).ToArray());
        var codeIsValid = normalizedCode.Length > 0 &&
            !string.IsNullOrEmpty(challenge.CodeHash) &&
            ChallengeTokenHasher.HashToken(normalizedCode) == challenge.CodeHash;

        if (!codeIsValid)
        {
            var outcome = await _challengeRepository.RecordFailedAttemptAsync(challenge.Id, _options.MaximumVerificationAttempts, cancellationToken);

            if (outcome == MfaChallengeFailureOutcome.NowLocked)
            {
                _logger.LogWarning("OTP challenge locked for user {UserId}, purpose '{Purpose}', after too many failed attempts", challenge.UserId, purpose);
                return OtpVerificationResult.Failed(OtpVerificationFailureReason.ChallengeLocked);
            }

            _logger.LogWarning("OTP verification failed for user {UserId}, purpose '{Purpose}'", challenge.UserId, purpose);
            return OtpVerificationResult.Failed(OtpVerificationFailureReason.InvalidCode);
        }

        // Consume before the caller acts on success so a losing concurrent request can never also succeed.
        var consumed = await _challengeRepository.TryConsumeAsync(challenge.Id, cancellationToken);
        if (!consumed)
        {
            return OtpVerificationResult.Failed(OtpVerificationFailureReason.ChallengeConsumed);
        }

        _logger.LogInformation("OTP verification succeeded for user {UserId}, purpose '{Purpose}'", challenge.UserId, purpose);

        return OtpVerificationResult.Succeeded(challenge.UserId);
    }

    private static string BuildContextMessage(string purpose) => purpose switch
    {
        OtpChallengePurpose.LoginOtp => "Use this code to complete your sign-in.",
        OtpChallengePurpose.ForgotPasswordOtp => "Use this code to verify your identity and reset your password.",
        _ => "Use this code to verify your identity."
    };

    private async Task SendEmailCodeAsync(UserEntity user, string rawCode, string purpose, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("OTP email delivery requested for user {UserId} but no email is on file.", user.Id);
            return;
        }

        var placeholders = new Dictionary<string, string>
        {
            { "UserName", user.UserName },
            { "VerificationCode", rawCode },
            { "ExpiryMinutes", _options.LifetimeMinutes.ToString() },
            { "CompanyName", DefaultCompanyName },
            { "ContextMessage", BuildContextMessage(purpose) }
        };

        var emailBody = await _emailTemplateService.GetTemplateAsync("OtpVerification", placeholders, cancellationToken);

        await _emailService.SendEmailAsync(new EmailRequest
        {
            ToEmail = user.Email,
            ToName = user.UserName,
            Subject = "Your one-time verification code",
            Body = emailBody,
            IsHtml = true
        }, cancellationToken);
    }

    private async Task SendSmsCodeAsync(UserEntity user, string rawCode, string purpose, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.MobileNo))
        {
            _logger.LogWarning("OTP SMS delivery requested for user {UserId} but no mobile number is on file.", user.Id);
            return;
        }

        await _smsService.SendSmsAsync(new SmsRequest
        {
            PhoneNumber = user.MobileNo,
            Message = $"Your {DefaultCompanyName} verification code is {rawCode}. It expires in {_options.LifetimeMinutes} minutes."
        }, cancellationToken);
    }
}
