using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class ChangePasswordTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IRepository<UserRoleMasterEntity>> _userRoleRepositoryMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IMfaChallengeService> _mfaChallengeServiceMock = new();
    private readonly Mock<IAuthTokenIssuerService> _authTokenIssuerMock = new();
    private readonly Mock<ISecuritySettingsService> _securitySettingsMock = new();
    private readonly Mock<IOtpChallengeService> _otpChallengeServiceMock = new();
    private readonly AuthService _authService;

    public ChangePasswordTests()
    {
        // Default security settings for tests: standard policy
        _securitySettingsMock
            .Setup(x => x.GetAsync("MINPASSWORDLENGTH", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);
        _securitySettingsMock
            .Setup(x => x.GetAsync("MAXPASSWORDLENGTH", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(128);
        _securitySettingsMock
            .Setup(x => x.GetAsync("REQUIREUPPERCASE", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _securitySettingsMock
            .Setup(x => x.GetAsync("REQUIRELOWERCASE", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _securitySettingsMock
            .Setup(x => x.GetAsync("REQUIREDIGIT", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _securitySettingsMock
            .Setup(x => x.GetAsync("REQUIRESPECIALCHAR", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _configurationMock.Object,
            _userRoleRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _mfaChallengeServiceMock.Object,
            _authTokenIssuerMock.Object,
            _securitySettingsMock.Object,
            _otpChallengeServiceMock.Object,
            new Mock<ILogger<AuthService>>().Object);
    }

    [Fact]
    public async Task ChangePassword_WhenUserNotFound_ReturnsFailure()
    {
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var response = await _authService.ChangePasswordAsync(999, request);

        Assert.False(response.Success);
        Assert.Contains("User not found", response.Message);
    }

    [Fact]
    public async Task ChangePassword_WhenCurrentPasswordIncorrect_ReturnsFailure()
    {
        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$11$existinghash",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword("WrongPassword123!", user.PasswordHash))
            .Returns(false);

        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var response = await _authService.ChangePasswordAsync(1, request);

        Assert.False(response.Success);
        Assert.Contains("Current password is incorrect", response.Message);
    }

    [Fact]
    public async Task ChangePassword_WhenNewPasswordMatchesCurrentPassword_ReturnsFailure()
    {
        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$11$existinghash",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword("SamePassword123!", user.PasswordHash))
            .Returns(true);

        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "SamePassword123!",
            NewPassword = "SamePassword123!",
            ConfirmPassword = "SamePassword123!"
        };

        var response = await _authService.ChangePasswordAsync(1, request);

        Assert.False(response.Success);
        Assert.Contains("must be different", response.Message);
    }

    [Fact]
    public async Task ChangePassword_WhenNewPasswordFailsComplexityPolicy_ReturnsFailure()
    {
        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$11$existinghash",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword("OldPassword123!", user.PasswordHash))
            .Returns(true);

        // Password missing digits and special characters
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "onlyletters",
            ConfirmPassword = "onlyletters"
        };

        var response = await _authService.ChangePasswordAsync(1, request);

        Assert.False(response.Success);
        Assert.True(response.Message.Contains("uppercase") || response.Message.Contains("number") || response.Message.Contains("special"));
    }

    [Fact]
    public async Task ChangePassword_WhenValid_UpdatesPasswordAndRevokesTokens()
    {
        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$11$existinghash",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword("OldPassword123!", user.PasswordHash))
            .Returns(true);

        _passwordHasherMock
            .Setup(x => x.HashPassword("NewPassword123!"))
            .Returns("$2a$11$newhash");

        _userRepositoryMock
            .Setup(x => x.ResetPasswordAsync(1, "$2a$11$newhash", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var response = await _authService.ChangePasswordAsync(1, request);

        Assert.True(response.Success);
        Assert.Contains("successfully", response.Message);

        _userRepositoryMock.Verify(x => x.ResetPasswordAsync(1, "$2a$11$newhash", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(x => x.RevokeAllUserTokensAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
