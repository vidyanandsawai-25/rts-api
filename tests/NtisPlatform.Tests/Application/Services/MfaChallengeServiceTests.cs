using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Unit tests for MfaChallengeService: login-challenge creation and verification (TOTP and
/// recovery code), expiry, one-time consumption, and attempt lockout. Uses a fixed TimeProvider
/// throughout — no test waits on real time.
/// </summary>
public class MfaChallengeServiceTests
{
    private readonly Mock<IMfaChallengeRepository> _challengeRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITwoFactorRecoveryCodeRepository> _recoveryCodeRepositoryMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITwoFactorSecretProtector> _secretProtectorMock = new();
    private readonly Mock<IAuthTokenIssuerService> _authTokenIssuerMock = new();
    private readonly Mock<ISecurityAuditService> _auditServiceMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly MfaChallengeService _service;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public MfaChallengeServiceTests()
    {
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(Now);
        _secretProtectorMock.Setup(x => x.Unprotect(It.IsAny<string>())).Returns((string s) => s.Replace("enc:", string.Empty));
        _passwordHasherMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string plain, string hash) => hash == $"hash:{plain}");

        var options = Options.Create(new TwoFactorAuthenticationOptions
        {
            Issuer = "NtisPlatform",
            ChallengeLifetimeMinutes = 5,
            MaximumVerificationAttempts = 3
        });

        _service = new MfaChallengeService(
            _challengeRepositoryMock.Object,
            _userRepositoryMock.Object,
            _recoveryCodeRepositoryMock.Object,
            _totpServiceMock.Object,
            _passwordHasherMock.Object,
            _secretProtectorMock.Object,
            _authTokenIssuerMock.Object,
            _auditServiceMock.Object,
            options,
            _timeProviderMock.Object,
            new Mock<ILogger<MfaChallengeService>>().Object);
    }

    private static UserEntity NewUser(int id = 1) => new()
    {
        Id = id,
        UserName = "jdoe",
        IsActive = true,
        TwoFactorEnabled = true,
        TwoFactorSecretEncrypted = "enc:SECRET"
    };

    [Fact]
    public async Task CreateLoginChallengeAsync_PersistsHashNotRawToken_AndReturnsRawTokenToCaller()
    {
        MfaChallengeEntity? captured = null;
        _challengeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<MfaChallengeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<MfaChallengeEntity, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync((MfaChallengeEntity c, CancellationToken _) => c);

        var challenge = await _service.CreateLoginChallengeAsync(1, "127.0.0.1", "test-agent");

        Assert.NotNull(captured);
        Assert.NotEqual(challenge.ChallengeId, captured!.ChallengeHash);
        Assert.Equal("mfa-login", captured.Purpose);
        Assert.Equal(1, captured.UserId);
        Assert.Equal(Now.UtcDateTime.AddMinutes(5), challenge.ExpiresAtUtc);
        _challengeRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        UserId = userId,
        Purpose = "mfa-login",
        CreatedAt = Now.UtcDateTime,
        ExpiresAt = Now.UtcDateTime.AddMinutes(5),
        FailedAttemptCount = failedAttempts
    };

    [Fact]
    public async Task VerifyLoginChallengeAsync_WithUnknownChallenge_ReturnsInvalidChallenge()
    {
        _challengeRepositoryMock.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MfaChallengeEntity?)null);

        var result = await _service.VerifyLoginChallengeAsync("unknown-token", "123456", useRecoveryCode: false);

        Assert.False(result.Success);
        Assert.Equal(MfaVerificationFailureReason.InvalidChallenge, result.FailureReason);
    }

    [Fact]
    public async Task VerifyLoginChallengeAsync_WithExpiredChallenge_ReturnsChallengeExpired()
    {
        var challenge = ActiveChallenge();
        challenge.ExpiresAt = Now.UtcDateTime.AddMinutes(-1);
        SetUpChallengeLookup(challenge);

        var result = await _service.VerifyLoginChallengeAsync("token", "123456", false);

        Assert.False(result.Success);
        Assert.Equal(MfaVerificationFailureReason.ChallengeExpired, result.FailureReason);
    }

    [Fact]
    public async Task VerifyLoginChallengeAsync_WithConsumedChallenge_ReturnsChallengeConsumed()
    {
        var challenge = ActiveChallenge();
        challenge.ConsumedAt = Now.UtcDateTime.AddSeconds(-30);
        SetUpChallengeLookup(challenge);

        var result = await _service.VerifyLoginChallengeAsync("token", "123456", false);

        Assert.False(result.Success);
        Assert.Equal(MfaVerificationFailureReason.ChallengeConsumed, result.FailureReason);
    }

    [Fact]
    public async Task VerifyLoginChallengeAsync_WithRevokedChallenge_ReturnsChallengeLocked()
    {
        var challenge = ActiveChallenge();
        challenge.RevokedAt = Now.UtcDateTime.AddSeconds(-10);
        SetUpChallengeLookup(challenge);

        var result = await _service.VerifyLoginChallengeAsync("token", "123456", false);

        Assert.False(result.Success);
        Assert.Equal(MfaVerificationFailureReason.ChallengeLocked, result.FailureReason);
    }

    [Fact]
    public async Task VerifyLoginChallengeAsync_WithValidTotpCode_ConsumesChallengeAndIssuesTokens()
    {
        var challenge = ActiveChallenge();
        SetUpChallengeLookup(challenge);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser());
        _totpServiceMock.Setup(x => x.ValidateCode("SECRET", "123456", Now)).Returns(true);
        _challengeRepositoryMock.Setup(x => x.TryConsumeAsync(challenge.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var loginResponse = new LoginResponseDto { Success = true, Token = "jwt", RefreshToken = "rt" };
        _authTokenIssuerMock.Setup(x => x.IssueAsync(It.Is<UserEntity>(u => u.Id == 1), "mfa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginResponse);

        var result = await _service.VerifyLoginChallengeAsync("raw-token", "123 456", useRecoveryCode: false);

        Assert.True(result.Success);
        Assert.Same(loginResponse, result.LoginResponse);
        _challengeRepositoryMock.Verify(x => x.TryConsumeAsync(challenge.Id, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyLoginChallengeAsync_WithWrongCode_RecordsFailedAttemptAndDoesNotIssueTokens()
    {
        var challenge = ActiveChallenge();
        SetUpChallengeLookup(challenge);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser());
        _totpServiceMock.Setup(x => x.ValidateCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>())).Returns(false);
        _challengeRepositoryMock
            .Setup(x => x.RecordFailedAttemptAsync(challenge.Id, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaChallengeFailureOutcome.AttemptRecorded);

        var result = await _service.VerifyLoginChallengeAsync("raw-token", "000000", false);

        Assert.False(result.Success);
        Assert.Equal(MfaVerificationFailureReason.InvalidCode, result.FailureReason);
        _authTokenIssuerMock.Verify(x => x.IssueAsync(It.IsAny<UserEntity>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _challengeRepositoryMock.Verify(x => x.TryConsumeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyLoginChallengeAsync_WhenAttemptLimitReached_ReturnsChallengeLocked()
    {
        var challenge = ActiveChallenge(failedAttempts: 2);
        SetUpChallengeLookup(challenge);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser());
        _totpServiceMock.Setup(x => x.ValidateCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>())).Returns(false);
        _challengeRepositoryMock
            .Setup(x => x.RecordFailedAttemptAsync(challenge.Id, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaChallengeFailureOutcome.NowLocked);

        var result = await _service.VerifyLoginChallengeAsync("raw-token", "000000", false);

        Assert.False(result.Success);
        Assert.Equal(MfaVerificationFailureReason.ChallengeLocked, result.FailureReason);
    }

    [Fact]
    public async Task VerifyLoginChallengeAsync_WithValidRecoveryCode_RedeemsExactlyOneCodeAndIssuesTokens()
    {
        var challenge = ActiveChallenge();
        SetUpChallengeLookup(challenge);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser());

        var recoveryCode = new TwoFactorRecoveryCodeEntity { Id = 99, UserId = 1, CodeHash = "hash:ABCDEFGHJK" };
        _recoveryCodeRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { recoveryCode });
        _recoveryCodeRepositoryMock.Setup(x => x.TryRedeemAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _challengeRepositoryMock.Setup(x => x.TryConsumeAsync(challenge.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _authTokenIssuerMock.Setup(x => x.IssueAsync(It.IsAny<UserEntity>(), "mfa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponseDto { Success = true });

        var result = await _service.VerifyLoginChallengeAsync("raw-token", "ABCDE-FGHJK", useRecoveryCode: true);

        Assert.True(result.Success);
        _recoveryCodeRepositoryMock.Verify(x => x.TryRedeemAsync(99, It.IsAny<CancellationToken>()), Times.Once);
        _totpServiceMock.Verify(x => x.ValidateCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task VerifyLoginChallengeAsync_WhenChallengeLosesConsumeRace_ReturnsChallengeConsumed()
    {
        var challenge = ActiveChallenge();
        SetUpChallengeLookup(challenge);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(NewUser());
        _totpServiceMock.Setup(x => x.ValidateCode("SECRET", "123456", Now)).Returns(true);
        _challengeRepositoryMock.Setup(x => x.TryConsumeAsync(challenge.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _service.VerifyLoginChallengeAsync("raw-token", "123456", false);

        Assert.False(result.Success);
        Assert.Equal(MfaVerificationFailureReason.ChallengeConsumed, result.FailureReason);
        _authTokenIssuerMock.Verify(x => x.IssueAsync(It.IsAny<UserEntity>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
