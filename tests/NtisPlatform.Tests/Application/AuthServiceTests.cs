using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for AuthService
/// Tests authentication, login validation, lockout, and the 2FA login branch.
/// Token/refresh-token issuance itself is owned by IAuthTokenIssuerService and is mocked here —
/// see AuthTokenIssuerServiceTests for that behavior.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IMfaChallengeService> _mfaChallengeServiceMock;
    private readonly Mock<IAuthTokenIssuerService> _authTokenIssuerMock;
    private readonly Mock<ISecuritySettingsService> _securitySettingsMock;
    private readonly Mock<IOtpChallengeService> _otpChallengeServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _configurationMock = new Mock<IConfiguration>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _mfaChallengeServiceMock = new Mock<IMfaChallengeService>();
        _authTokenIssuerMock = new Mock<IAuthTokenIssuerService>();
        _securitySettingsMock = new Mock<ISecuritySettingsService>();
        _otpChallengeServiceMock = new Mock<IOtpChallengeService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        // Setup default configuration
        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("60");
        _configurationMock.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");

        // 2FALOGIN off by default — existing tests exercise the pre-OTP behavior
        _securitySettingsMock
            .Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _configurationMock.Object,
            new Mock<IRepository<UserRoleMasterEntity>>().Object,
            _refreshTokenRepositoryMock.Object,
            _mfaChallengeServiceMock.Object,
            _authTokenIssuerMock.Object,
            _securitySettingsMock.Object,
            _otpChallengeServiceMock.Object,
            _loggerMock.Object
        );
    }

    private void SetUpSuccessfulTokenIssuance(UserEntity user, string token = "mock-jwt-token")
    {
        _authTokenIssuerMock
            .Setup(x => x.IssueAsync(It.Is<UserEntity>(u => u.Id == user.Id), "pwd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponseDto
            {
                Success = true,
                Token = token,
                RefreshToken = "mock-refresh-token",
                UserId = user.Id,
                Username = user.UserName,
                Message = "Login successful",
                ExpiresAt = DateTime.Now.AddMinutes(60)
            });
    }

    #region Login Success Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            FailedLoginCount = 0,
            LockedUntilAt = null,
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);
        _userRepositoryMock.Setup(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.UpdateLastLoginAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetUpSuccessfulTokenIssuance(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("mock-jwt-token", result.Token);
        Assert.False(result.RequiresTwoFactor);

        Assert.Equal("testuser", result.Username);
        Assert.Equal("Login successful", result.Message);
        Assert.NotNull(result.ExpiresAt);

        // Verify security operations were called
        _userRepositoryMock.Verify(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateLastLoginAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mfaChallengeServiceMock.Verify(x => x.CreateLoginChallengeAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentialsButNoRole_ReturnsSuccessWithoutRoleName()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            FailedLoginCount = 0
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);
        _userRepositoryMock.Setup(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.UpdateLastLoginAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetUpSuccessfulTokenIssuance(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task LoginAsync_DelegatesTokenIssuanceToAuthTokenIssuer()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);
        SetUpSuccessfulTokenIssuance(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert — LoginAsync never talks to ITokenService directly; that responsibility
        // belongs entirely to IAuthTokenIssuerService now.
        Assert.True(result.Success);
        _authTokenIssuerMock.Verify(x => x.IssueAsync(It.Is<UserEntity>(u => u.Id == 1), "pwd", It.IsAny<CancellationToken>()), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    #endregion

    #region Login Two-Factor Tests

    [Fact]
    public async Task LoginAsync_WithTwoFactorEnabled_ReturnsRequiresTwoFactorWithoutTokens()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            TwoFactorEnabled = true
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);
        _mfaChallengeServiceMock
            .Setup(x => x.CreateLoginChallengeAsync(1, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MfaLoginChallenge("opaque-challenge-id", expiresAt));

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.RequiresTwoFactor);
        Assert.Equal("opaque-challenge-id", result.ChallengeId);
        Assert.Equal(expiresAt, result.ChallengeExpiresAtUtc);
        Assert.Null(result.Token);
        Assert.Null(result.RefreshToken);

        _authTokenIssuerMock.Verify(x => x.IssueAsync(It.IsAny<UserEntity>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Login Failure Tests - Invalid Credentials

    [Fact]
    public async Task LoginAsync_WithNonExistentUsername_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "nonexistent",
            Password = "SomePassword123"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Equal("Invalid username or password", result.Message);

        // Verify no security operations were called
        _userRepositoryMock.Verify(x => x.IncrementFailedLoginCountAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailureAndIncrementsFailedCount()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            FailedLoginCount = 2
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("WrongPassword", "$2a$12$hashedpassword"))
            .Returns(false);
        _userRepositoryMock.Setup(x => x.IncrementFailedLoginCountAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Equal("Invalid username or password", result.Message);

        // Verify failed login count was incremented
        _userRepositoryMock.Verify(x => x.IncrementFailedLoginCountAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.ResetFailedLoginCountAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Login Failure Tests - Inactive User

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "inactiveuser",
            Password = "ValidPassword123"
        };

        var user = new UserEntity
        {
            Id = 1,
            UserName = "inactiveuser",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = false
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("inactiveuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Equal("User account is inactive. Please contact administrator.", result.Message);

        // Verify password was never checked
        _passwordHasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Login Failure Tests - Locked Account

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "lockeduser",
            Password = "ValidPassword123"
        };

        var lockedUntil = DateTime.Now.AddMinutes(15);
        var user = new UserEntity
        {
            Id = 1,
            UserName = "lockeduser",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            LockedUntilAt = lockedUntil,
            FailedLoginCount = 5
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("lockeduser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Contains("Account is locked until", result.Message);

        // Verify password was never checked
        _passwordHasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithExpiredLockout_AllowsLogin()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var expiredLockout = DateTime.Now.AddMinutes(-5); // Lockout expired 5 minutes ago
        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            LockedUntilAt = expiredLockout,
            FailedLoginCount = 5,
            MobileNo = "1234567890"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);

        _userRepositoryMock.Setup(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.UpdateLastLoginAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetUpSuccessfulTokenIssuance(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
    }

    #endregion

    #region Login Failure Tests - No Password Hash

    [Fact]
    public async Task LoginAsync_WithNoPasswordHash_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = null, // No password set
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Equal("Password not set for this user. Please contact administrator.", result.Message);

        // Verify password verification was never attempted
        _passwordHasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithEmptyPasswordHash_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            MobileNo = "1234567890",
            PasswordHash = "", // Empty password hash
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Password not set for this user. Please contact administrator.", result.Message);
    }

    #endregion

    #region ValidateSessionAsync Tests

    [Fact]
    public async Task ValidateSessionAsync_WithValidToken_ReturnsValid()
    {
        // Arrange
        var request = new ValidateSessionRequestDto
        {
            AccessToken = "valid-jwt-token"
        };

        var tokenValidationResult = new JwtValidationResult
        {
            IsValid = true,
            UserId = 123,
            Username = "testuser",
            ExpiresAt = DateTime.Now.AddMinutes(30)
        };

        _tokenServiceMock.Setup(x => x.ValidateToken("valid-jwt-token"))
            .Returns(tokenValidationResult);

        // Act
        var result = await _authService.ValidateSessionAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(123, result.UserId);
        Assert.Equal("testuser", result.Username);
        Assert.NotNull(result.ExpiresAt);
        Assert.Equal("Token is valid", result.Message);
    }

    [Fact]
    public async Task ValidateSessionAsync_WithInvalidToken_ReturnsInvalid()
    {
        // Arrange
        var request = new ValidateSessionRequestDto
        {
            AccessToken = "invalid-jwt-token"
        };

        var tokenValidationResult = new JwtValidationResult
        {
            IsValid = false,
            ErrorMessage = "Token has expired"
        };

        _tokenServiceMock.Setup(x => x.ValidateToken("invalid-jwt-token"))
            .Returns(tokenValidationResult);

        // Act
        var result = await _authService.ValidateSessionAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Token has expired", result.Message);
    }

    [Fact]
    public async Task ValidateSessionAsync_WithMalformedToken_ReturnsInvalidWithDefaultMessage()
    {
        // Arrange
        var request = new ValidateSessionRequestDto
        {
            AccessToken = "malformed-token"
        };

        var tokenValidationResult = new JwtValidationResult
        {
            IsValid = false,
            ErrorMessage = null
        };

        _tokenServiceMock.Setup(x => x.ValidateToken("malformed-token"))
            .Returns(tokenValidationResult);

        // Act
        var result = await _authService.ValidateSessionAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid token", result.Message);
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_WithValidRefreshToken_RevokesTokenSuccessfully()
    {
        // Arrange
        var request = new LogoutRequestDto
        {
            RefreshToken = "valid-refresh-token"
        };

        var refreshTokenEntity = new RefreshTokenEntity
        {
            Id = 1,
            UserId = 123,
            Token = "hashed-refresh-token",
            IsRevoked = false,
            ExpiresAt = DateTime.Now.AddDays(7),
            CreatedDate = DateTime.Now
        };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync("valid-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenEntity);
        _refreshTokenRepositoryMock.Setup(x => x.RevokeTokenAsync("valid-refresh-token", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LogoutAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Logged out successfully", result.Message);
        _refreshTokenRepositoryMock.Verify(x => x.RevokeTokenAsync("valid-refresh-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidRefreshToken_ReturnsFailure()
    {
        // Arrange
        var request = new LogoutRequestDto
        {
            RefreshToken = "invalid-refresh-token"
        };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync("invalid-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenEntity?)null);

        // Act
        var result = await _authService.LogoutAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid refresh token", result.Message);
        _refreshTokenRepositoryMock.Verify(x => x.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_WithNullRefreshToken_ThrowsOrReturnsFailure()
    {
        // Arrange
        var request = new LogoutRequestDto
        {
            RefreshToken = null!
        };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenEntity?)null);

        // Act & Assert - Test should handle both exception and graceful failure
        try
        {
            var result = await _authService.LogoutAsync(request);
            Assert.False(result.Success);
        }
        catch (ArgumentException)
        {
            Assert.True(true); // Expected if validation added
        }
    }

    #endregion

    #region MustChangePassword Tests

    [Fact]
    public async Task LoginAsync_WithMustChangePassword_ReturnsFailureWithFlag()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var user = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            MustChangePassword = true
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.RequiresPasswordChange);
        Assert.Contains("must change your password", result.Message);
        Assert.Null(result.Token);
    }

    #endregion
}
