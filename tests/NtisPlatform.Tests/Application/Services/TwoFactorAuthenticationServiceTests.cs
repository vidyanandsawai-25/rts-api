using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.DTOs.Email;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Unit tests for TwoFactorAuthenticationService: authenticator setup, enable, disable, reset,
/// and recovery-code lifecycle. All collaborators are mocked; no real crypto or database.
/// </summary>
public class TwoFactorAuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITwoFactorRecoveryCodeRepository> _recoveryCodeRepositoryMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IMfaChallengeRepository> _challengeRepositoryMock = new();
    private readonly Mock<ITotpService> _totpServiceMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITwoFactorSecretProtector> _secretProtectorMock = new();
    private readonly Mock<ISecurityAuditService> _auditServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly TwoFactorAuthenticationService _service;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public TwoFactorAuthenticationServiceTests()
    {
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(Now);

        // Protector is a pass-through for tests — the "encrypted" value is just a prefixed marker.
        _secretProtectorMock.Setup(x => x.Protect(It.IsAny<string>())).Returns((string s) => $"enc:{s}");
        _secretProtectorMock.Setup(x => x.Unprotect(It.IsAny<string>())).Returns((string s) => s.Replace("enc:", string.Empty));

        // Recovery code hashing: treat the hash as the code itself prefixed, and verify accordingly.
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<string>())).Returns((string p) => $"hash:{p}");
        _passwordHasherMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string plain, string hash) => hash == $"hash:{plain}");

        _emailTemplateServiceMock
            .Setup(x => x.GetTemplateAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>stub email body</html>");

        var options = Options.Create(new TwoFactorAuthenticationOptions
        {
            Issuer = "NtisPlatform",
            RecoveryCodeCount = 10,
            MaximumVerificationAttempts = 5,
            ChallengeLifetimeMinutes = 5,
            EmailVerificationLifetimeMinutes = 15
        });

        _service = new TwoFactorAuthenticationService(
            _userRepositoryMock.Object,
            _recoveryCodeRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _challengeRepositoryMock.Object,
            _totpServiceMock.Object,
            _passwordHasherMock.Object,
            _secretProtectorMock.Object,
            _auditServiceMock.Object,
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object,
            options,
            _timeProviderMock.Object,
            new Mock<ILogger<TwoFactorAuthenticationService>>().Object);
    }

    private static UserEntity NewUser(int id = 1, bool enabled = false, string? secret = null, string? email = "jdoe@example.com") => new()
    {
        Id = id,
        UserName = "jdoe",
        FirstName = "J",
        LastName = "Doe",
        IsActive = true,
        TwoFactorEnabled = enabled,
        TwoFactorSecretEncrypted = secret,
        Email = email
    };

    private static string HashForTest(string raw)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    #region GetStatusAsync

    [Fact]
    public async Task GetStatusAsync_WhenEnabled_ReturnsEnabledWithRecoveryCodeCount()
    {
        var user = NewUser(enabled: true, secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _recoveryCodeRepositoryMock.Setup(x => x.CountActiveByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(7);

        var status = await _service.GetStatusAsync(1);

        Assert.True(status.IsEnabled);
        Assert.True(status.HasAuthenticatorKey);
        Assert.Equal(7, status.RecoveryCodesRemaining);
    }

    [Fact]
    public async Task GetStatusAsync_WhenNotEnabled_ReturnsZeroRecoveryCodes()
    {
        var user = NewUser(enabled: false);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var status = await _service.GetStatusAsync(1);

        Assert.False(status.IsEnabled);
        Assert.Equal(0, status.RecoveryCodesRemaining);
        _recoveryCodeRepositoryMock.Verify(x => x.CountActiveByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region BeginSetupAsync

    [Fact]
    public async Task BeginSetupAsync_WhenNoKeyExists_ReturnsSetupResponse()
    {
        var user = NewUser();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpServiceMock.Setup(x => x.GenerateSecret()).Returns("SECRET");
        _totpServiceMock.Setup(x => x.BuildAuthenticatorUri("NtisPlatform", "jdoe", "SECRET")).Returns("otpauth://totp/example");

        var result = await _service.BeginSetupAsync(1, isReset: false);

        Assert.True(result.Success);
        Assert.Equal("otpauth://totp/example", result.Value!.AuthenticatorUri);
        Assert.Equal("NtisPlatform", result.Value.Issuer);
        Assert.Equal("jdoe", result.Value.AccountName);
        _userRepositoryMock.Verify(x => x.SetPendingTwoFactorSecretAsync(1, "enc:SECRET", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BeginSetupAsync_WhenAlreadyEnabledAndNotReset_ReturnsAlreadyEnabled()
    {
        var user = NewUser(enabled: true, secret: "enc:OLD");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.BeginSetupAsync(1, isReset: false);

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.AlreadyEnabled, result.Error);
        _userRepositoryMock.Verify(x => x.SetPendingTwoFactorSecretAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BeginSetupAsync_WhenAlreadyEnabledAndIsReset_GeneratesNewSecretAnyway()
    {
        var user = NewUser(enabled: true, secret: "enc:OLD");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpServiceMock.Setup(x => x.GenerateSecret()).Returns("NEWSECRET");
        _totpServiceMock.Setup(x => x.BuildAuthenticatorUri(It.IsAny<string>(), It.IsAny<string>(), "NEWSECRET")).Returns("otpauth://totp/new");

        var result = await _service.BeginSetupAsync(1, isReset: true);

        Assert.True(result.Success);
        _userRepositoryMock.Verify(x => x.SetPendingTwoFactorSecretAsync(1, "enc:NEWSECRET", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BeginSetupAsync_WhenNoEmailOnFile_ReturnsEmailNotOnFile()
    {
        var user = NewUser(email: null);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.BeginSetupAsync(1, isReset: false);

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.EmailNotOnFile, result.Error);
        _userRepositoryMock.Verify(x => x.SetPendingTwoFactorSecretAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region EnableAsync

    [Fact]
    public async Task EnableAsync_WhenSetupNotStarted_ReturnsSetupNotStarted()
    {
        var user = NewUser();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.EnableAsync(1, "123456");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.SetupNotStarted, result.Error);
    }

    [Fact]
    public async Task EnableAsync_WhenAlreadyEnabled_ReturnsAlreadyEnabled()
    {
        var user = NewUser(enabled: true, secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.EnableAsync(1, "123456");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.AlreadyEnabled, result.Error);
    }

    [Fact]
    public async Task EnableAsync_WhenNoEmailOnFile_ReturnsEmailNotOnFile()
    {
        var user = NewUser(secret: "enc:SECRET", email: null);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.EnableAsync(1, "123456");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.EmailNotOnFile, result.Error);
        _totpServiceMock.Verify(x => x.ValidateCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task EnableAsync_WithMalformedCode_ReturnsInvalidCodeWithoutCallingTotpService()
    {
        var user = NewUser(secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.EnableAsync(1, "12a456");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.InvalidCode, result.Error);
        _totpServiceMock.Verify(x => x.ValidateCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task EnableAsync_WithWrongTotpCode_ReturnsInvalidCodeAndDoesNotEmail()
    {
        var user = NewUser(secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpServiceMock.Setup(x => x.ValidateCode("SECRET", "000000", Now)).Returns(false);

        var result = await _service.EnableAsync(1, "000000");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.InvalidCode, result.Error);
        _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _challengeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<MfaChallengeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnableAsync_WithValidTotpCode_EmailsCodeWithoutEnabling()
    {
        var user = NewUser(secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpServiceMock.Setup(x => x.ValidateCode("SECRET", "123456", Now)).Returns(true);

        var result = await _service.EnableAsync(1, "123 456"); // authenticator apps often render with a space

        Assert.True(result.Success);
        Assert.Equal("jd***@example.com", result.Value!.MaskedEmail);
        _challengeRepositoryMock.Verify(x => x.AddAsync(
            It.Is<MfaChallengeEntity>(c => c.UserId == 1 && c.Purpose == "2fa-enable-email-verify"),
            It.IsAny<CancellationToken>()), Times.Once);
        _challengeRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendEmailAsync(
            It.Is<EmailRequest>(r => r.ToEmail == "jdoe@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.EnableTwoFactorAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ConfirmEnableAsync

    [Fact]
    public async Task ConfirmEnableAsync_WhenAlreadyEnabled_ReturnsAlreadyEnabled()
    {
        var user = NewUser(enabled: true, secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.ConfirmEnableAsync(1, "482913");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.AlreadyEnabled, result.Error);
    }

    [Fact]
    public async Task ConfirmEnableAsync_WhenNoActiveChallenge_ReturnsInvalidCode()
    {
        var user = NewUser(secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _challengeRepositoryMock
            .Setup(x => x.GetActiveByUserIdAndPurposeAsync(1, "2fa-enable-email-verify", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MfaChallengeEntity?)null);

        var result = await _service.ConfirmEnableAsync(1, "482913");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.InvalidCode, result.Error);
    }

    [Fact]
    public async Task ConfirmEnableAsync_WithWrongEmailCode_RecordsFailedAttemptAndDoesNotEnable()
    {
        var user = NewUser(secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var challenge = new MfaChallengeEntity
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            Purpose = "2fa-enable-email-verify",
            ChallengeHash = HashForTest("482913"),
            CreatedAt = Now.UtcDateTime,
            ExpiresAt = Now.UtcDateTime.AddMinutes(15)
        };
        _challengeRepositoryMock
            .Setup(x => x.GetActiveByUserIdAndPurposeAsync(1, "2fa-enable-email-verify", It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _challengeRepositoryMock
            .Setup(x => x.RecordFailedAttemptAsync(challenge.Id, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaChallengeFailureOutcome.AttemptRecorded);

        var result = await _service.ConfirmEnableAsync(1, "000000");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.InvalidCode, result.Error);
        _challengeRepositoryMock.Verify(x => x.RecordFailedAttemptAsync(challenge.Id, 5, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.EnableTwoFactorAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmEnableAsync_WithValidEmailCode_EnablesAndReturnsRecoveryCodes()
    {
        var user = NewUser(secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var challenge = new MfaChallengeEntity
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            Purpose = "2fa-enable-email-verify",
            ChallengeHash = HashForTest("482913"),
            CreatedAt = Now.UtcDateTime,
            ExpiresAt = Now.UtcDateTime.AddMinutes(15)
        };
        _challengeRepositoryMock
            .Setup(x => x.GetActiveByUserIdAndPurposeAsync(1, "2fa-enable-email-verify", It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        _challengeRepositoryMock
            .Setup(x => x.TryConsumeAsync(challenge.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(x => x.EnableTwoFactorAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.ConfirmEnableAsync(1, "482913");

        Assert.True(result.Success);
        Assert.True(result.Value!.IsEnabled);
        Assert.Equal(10, result.Value.RecoveryCodes.Count);
        _userRepositoryMock.Verify(x => x.EnableTwoFactorAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RegenerateRecoveryCodesAsync

    [Fact]
    public async Task RegenerateRecoveryCodesAsync_WhenNotEnabled_ReturnsNotEnabled()
    {
        var user = NewUser(enabled: false);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.RegenerateRecoveryCodesAsync(1, "123456");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.NotEnabled, result.Error);
    }

    [Fact]
    public async Task RegenerateRecoveryCodesAsync_WithValidCode_RevokesOldCodesAndIssuesNewOnes()
    {
        var user = NewUser(enabled: true, secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpServiceMock.Setup(x => x.ValidateCode("SECRET", "123456", Now)).Returns(true);

        var result = await _service.RegenerateRecoveryCodesAsync(1, "123456");

        Assert.True(result.Success);
        Assert.Equal(10, result.Value!.RecoveryCodes.Count);
        _recoveryCodeRepositoryMock.Verify(x => x.RevokeAllActiveAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DisableAsync

    [Fact]
    public async Task DisableAsync_WhenNotEnabled_ReturnsNotEnabled()
    {
        var user = NewUser(enabled: false);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.DisableAsync(1, "123456");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.NotEnabled, result.Error);
    }

    [Fact]
    public async Task DisableAsync_WithInvalidCode_ReturnsInvalidCodeAndDoesNotDisable()
    {
        var user = NewUser(enabled: true, secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpServiceMock.Setup(x => x.ValidateCode("SECRET", "000000", Now)).Returns(false);
        _recoveryCodeRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TwoFactorRecoveryCodeEntity>());

        var result = await _service.DisableAsync(1, "000000");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.InvalidCode, result.Error);
        _userRepositoryMock.Verify(x => x.DisableTwoFactorAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DisableAsync_WithValidTotpCode_DisablesAndRevokesSessionsAndRecoveryCodes()
    {
        var user = NewUser(enabled: true, secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpServiceMock.Setup(x => x.ValidateCode("SECRET", "123456", Now)).Returns(true);
        _userRepositoryMock.Setup(x => x.DisableTwoFactorAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.DisableAsync(1, "123456");

        Assert.True(result.Success);
        _userRepositoryMock.Verify(x => x.DisableTwoFactorAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _recoveryCodeRepositoryMock.Verify(x => x.RevokeAllActiveAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(x => x.RevokeAllUserTokensAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableAsync_WithValidRecoveryCode_Succeeds()
    {
        var user = NewUser(enabled: true, secret: "enc:SECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpServiceMock.Setup(x => x.ValidateCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>())).Returns(false);
        _userRepositoryMock.Setup(x => x.DisableTwoFactorAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var recoveryCode = new TwoFactorRecoveryCodeEntity { Id = 42, UserId = 1, CodeHash = "hash:ABCDEFGHJK" };
        _recoveryCodeRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { recoveryCode });
        _recoveryCodeRepositoryMock.Setup(x => x.TryRedeemAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.DisableAsync(1, "abcde-fghjk"); // lowercase + hyphen, as a user might type it

        Assert.True(result.Success);
        _recoveryCodeRepositoryMock.Verify(x => x.TryRedeemAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ResetAsync

    [Fact]
    public async Task ResetAsync_WhenNotEnabled_ReturnsNotEnabled()
    {
        var user = NewUser(enabled: false);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.ResetAsync(1, "123456");

        Assert.False(result.Success);
        Assert.Equal(TwoFactorOperationError.NotEnabled, result.Error);
    }

    [Fact]
    public async Task ResetAsync_WithValidCode_DisablesThenReturnsFreshSetupPayload()
    {
        var user = NewUser(enabled: true, secret: "enc:OLDSECRET");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _totpServiceMock.Setup(x => x.ValidateCode("OLDSECRET", "123456", Now)).Returns(true);
        _userRepositoryMock.Setup(x => x.DisableTwoFactorAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _totpServiceMock.Setup(x => x.GenerateSecret()).Returns("NEWSECRET");
        _totpServiceMock.Setup(x => x.BuildAuthenticatorUri(It.IsAny<string>(), It.IsAny<string>(), "NEWSECRET")).Returns("otpauth://totp/reset");

        var result = await _service.ResetAsync(1, "123456");

        Assert.True(result.Success);
        Assert.Equal("otpauth://totp/reset", result.Value!.AuthenticatorUri);
        _userRepositoryMock.Verify(x => x.DisableTwoFactorAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(x => x.RevokeAllUserTokensAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.SetPendingTwoFactorSecretAsync(1, "enc:NEWSECRET", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
