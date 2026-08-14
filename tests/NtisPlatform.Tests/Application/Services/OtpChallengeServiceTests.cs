using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.DTOs.Email;
using NtisPlatform.Application.DTOs.Sms;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Unit tests for OtpChallengeService: OTP challenge creation/verification, expiry, one-time
/// consumption, per-challenge attempt lockout, and the account-level challenge-issuance throttle.
/// </summary>
public class OtpChallengeServiceTests
{
    private readonly Mock<IMfaChallengeRepository> _challengeRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();
    private readonly Mock<ISmsService> _smsServiceMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly OtpChallengeService _service;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public OtpChallengeServiceTests()
    {
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(Now);
        _timeProviderMock.Setup(x => x.LocalTimeZone).Returns(TimeZoneInfo.Utc);
        _emailTemplateServiceMock
            .Setup(x => x.GetTemplateAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html></html>");

        var options = Options.Create(new OtpChallengeOptions
        {
            LifetimeMinutes = 5,
            MaximumVerificationAttempts = 3
        });

        _service = new OtpChallengeService(
            _challengeRepositoryMock.Object,
            _userRepositoryMock.Object,
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object,
            _smsServiceMock.Object,
            options,
            _timeProviderMock.Object,
            new Mock<ILogger<OtpChallengeService>>().Object);
    }

    private static UserEntity NewUser(int id = 1, DateTime? otpLockedUntil = null) => new()
    {
        Id = id,
        UserName = "jdoe",
        Email = "jdoe@example.com",
        OtpChallengeLockedUntilAt = otpLockedUntil
    };

    [Fact]
    public async Task CreateAsync_WhenAccountThrottled_ReturnsAccountThrottledWithoutCreatingChallenge()
    {
        var user = NewUser(otpLockedUntil: Now.UtcDateTime.AddMinutes(5));

        var creation = await _service.CreateAsync(user, OtpChallengePurpose.LoginOtp, sendEmail: true, sendSms: false, null, null);

        Assert.False(creation.Success);
        Assert.Equal(ChallengeCreationFailureReason.AccountThrottled, creation.FailureReason);
        _challengeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<MfaChallengeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenThrottleWindowHasPassed_CreatesChallengeNormally()
    {
        var user = NewUser(otpLockedUntil: Now.UtcDateTime.AddMinutes(-5));
        _challengeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<MfaChallengeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MfaChallengeEntity c, CancellationToken _) => c);

        var creation = await _service.CreateAsync(user, OtpChallengePurpose.LoginOtp, sendEmail: true, sendSms: false, null, null);

        Assert.True(creation.Success);
        Assert.NotNull(creation.Challenge);
    }

    [Fact]
    public async Task CreateAsync_WhenNotThrottled_CreatesChallengeAndSendsEmail()
    {
        var user = NewUser();
        _challengeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<MfaChallengeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MfaChallengeEntity c, CancellationToken _) => c);

        var creation = await _service.CreateAsync(user, OtpChallengePurpose.LoginOtp, sendEmail: true, sendSms: false, null, null);

        Assert.True(creation.Success);
        _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetUpChallengeLookup(MfaChallengeEntity challenge)
    {
        _challengeRepositoryMock
            .Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
    }

    private static MfaChallengeEntity ActiveChallenge(int userId = 1, int failedAttempts = 0) => new()
    {
        Id = Guid.NewGuid(),
        ChallengeHash = "irrelevant-in-these-tests",
        CodeHash = "hash-of-000000",
        UserId = userId,
        Purpose = OtpChallengePurpose.LoginOtp,
        CreatedAt = Now.UtcDateTime,
        ExpiresAt = Now.UtcDateTime.AddMinutes(5),
        FailedAttemptCount = failedAttempts
    };

    [Fact]
    public async Task VerifyAsync_WhenAttemptLimitReached_IncrementsAccountLockoutCounter()
    {
        var challenge = ActiveChallenge(failedAttempts: 2);
        SetUpChallengeLookup(challenge);
        _challengeRepositoryMock
            .Setup(x => x.RecordFailedAttemptAsync(challenge.Id, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaChallengeFailureOutcome.NowLocked);

        var result = await _service.VerifyAsync("raw-token", OtpChallengePurpose.LoginOtp, "999999");

        Assert.False(result.Success);
        Assert.Equal(OtpVerificationFailureReason.ChallengeLocked, result.FailureReason);
        _userRepositoryMock.Verify(x => x.IncrementOtpChallengeLockoutAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyAsync_WithWrongCode_DoesNotIncrementAccountLockoutCounter()
    {
        var challenge = ActiveChallenge();
        SetUpChallengeLookup(challenge);
        _challengeRepositoryMock
            .Setup(x => x.RecordFailedAttemptAsync(challenge.Id, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaChallengeFailureOutcome.AttemptRecorded);

        var result = await _service.VerifyAsync("raw-token", OtpChallengePurpose.LoginOtp, "999999");

        Assert.False(result.Success);
        Assert.Equal(OtpVerificationFailureReason.InvalidCode, result.FailureReason);
        _userRepositoryMock.Verify(x => x.IncrementOtpChallengeLockoutAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyAsync_WithValidCode_ResetsAccountLockoutCounter()
    {
        var challenge = ActiveChallenge();
        challenge.CodeHash = NtisPlatform.Application.Helpers.ChallengeTokenHasher.HashToken("123456");
        SetUpChallengeLookup(challenge);
        _challengeRepositoryMock.Setup(x => x.TryConsumeAsync(challenge.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.VerifyAsync("raw-token", OtpChallengePurpose.LoginOtp, "123456");

        Assert.True(result.Success);
        _userRepositoryMock.Verify(x => x.ResetOtpChallengeLockoutAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
