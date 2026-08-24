using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.DTOs.PasswordReset;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class PasswordResetServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IOtpChallengeService> _otpChallengeServiceMock = new();
    private readonly Mock<IMfaChallengeRepository> _challengeRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<ISecuritySettingsService> _securitySettingsMock = new();
    private readonly Mock<ISecurityAuditService> _auditServiceMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<ITwoFactorSecretProtector> _secretProtectorMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly PasswordResetService _service;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public PasswordResetServiceTests()
    {
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(Now);
        _timeProviderMock.Setup(x => x.LocalTimeZone).Returns(TimeZoneInfo.Utc);

        var options = Options.Create(new OtpChallengeOptions
        {
            LifetimeMinutes = 5,
            MaximumVerificationAttempts = 3,
            PasswordResetTokenLifetimeMinutes = 10
        });

        // Default: security settings returns default value passed in
        _securitySettingsMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, bool defaultValue, CancellationToken _) => defaultValue);

        _service = new PasswordResetService(
            _userRepositoryMock.Object,
            _otpChallengeServiceMock.Object,
            _challengeRepositoryMock.Object,
            _passwordHasherMock.Object,
            _refreshTokenRepositoryMock.Object,
            _securitySettingsMock.Object,
            _auditServiceMock.Object,
            _totpServiceMock.Object,
            _secretProtectorMock.Object,
            options,
            _timeProviderMock.Object,
            new Mock<ILogger<PasswordResetService>>().Object);
    }

    [Fact]
    public async Task GetAvailableMethodsAsync_WhenFeatureDisabled_ReturnsUnavailableMessage()
    {
        _securitySettingsMock
            .Setup(x => x.GetAsync("2FALOGINFORFPASS", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _service.GetAvailableMethodsAsync(new ForgotPasswordAvailableMethodsRequestDto
        {
            UsernameOrEmail = "jdoe"
        });

        Assert.True(response.Success);
        Assert.Empty(response.Methods);
        Assert.Contains("not available", response.Message);
    }

    [Fact]
    public async Task GetAvailableMethodsAsync_WhenFeatureEnabledAndUserHasEmail_ReturnsEmailMethod()
    {
        _userRepositoryMock
            .Setup(x => x.GetByUsernameOrEmailAsync("jdoe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserEntity { Id = 1, UserName = "jdoe", Email = "jdoe@example.com", IsActive = true });

        var response = await _service.GetAvailableMethodsAsync(new ForgotPasswordAvailableMethodsRequestDto
        {
            UsernameOrEmail = "jdoe"
        });

        Assert.True(response.Success);
        Assert.Contains("Email", response.Methods);
        Assert.NotNull(response.MaskedEmail);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenFeatureDisabled_ReturnsFailure()
    {
        _securitySettingsMock
            .Setup(x => x.GetAsync("2FALOGINFORFPASS", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _service.ForgotPasswordAsync(new ForgotPasswordRequestDto
        {
            UsernameOrEmail = "jdoe",
            Method = "Email"
        });

        Assert.False(response.Success);
        Assert.Contains("not available", response.Message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenValidUserAndEmailMethod_CreatesChallengeAndReturnsId()
    {
        var user = new UserEntity { Id = 1, UserName = "jdoe", Email = "jdoe@example.com", IsActive = true };
        _userRepositoryMock
            .Setup(x => x.GetByUsernameOrEmailAsync("jdoe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _otpChallengeServiceMock
            .Setup(x => x.CreateAsync(user, OtpChallengePurpose.ForgotPasswordOtp, true, false, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OtpChallengeCreationResult.Succeeded(new OtpChallengeResult("challenge-123", Now.UtcDateTime.AddMinutes(5))));

        var response = await _service.ForgotPasswordAsync(new ForgotPasswordRequestDto
        {
            UsernameOrEmail = "jdoe",
            Method = "Email"
        });

        Assert.True(response.Success);
        Assert.Equal("challenge-123", response.ChallengeId);
        Assert.NotNull(response.ChallengeExpiresAt);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenInvalidMethod_SuppressesErrorAndReturnsNullChallengeId()
    {
        var user = new UserEntity { Id = 1, UserName = "jdoe", Email = "jdoe@example.com", IsActive = true };
        _userRepositoryMock
            .Setup(x => x.GetByUsernameOrEmailAsync("jdoe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var response = await _service.ForgotPasswordAsync(new ForgotPasswordRequestDto
        {
            UsernameOrEmail = "jdoe",
            Method = "string" // Invalid method
        });

        Assert.True(response.Success);
        Assert.Null(response.ChallengeId);
    }
}
