using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for AuthService
/// Tests authentication, login validation, lockout, and token generation
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IRepository<UserRoleMasterEntity>> _userRoleRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _configurationMock = new Mock<IConfiguration>();
        _userRoleRepositoryMock = new Mock<IRepository<UserRoleMasterEntity>>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        // Setup default configuration
        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("60");
        _configurationMock.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _configurationMock.Object,
            _userRoleRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _loggerMock.Object 
        );
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

        var user = new UserMasterEntity
        {
            Id = 1,
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            UserRoleID = 1,
            FailedLoginCount = 0,
            LockedUntilAt = null
        };

        var userRole = new UserRoleMasterEntity
        {
            Id = 1,
            UserRoleName = "Administrator"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);
        _userRoleRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRole);
        _tokenServiceMock.Setup(x => x.GenerateToken(1, "testuser", 1))
            .Returns("mock-jwt-token");
        _userRepositoryMock.Setup(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.UpdateLastLoginAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("mock-jwt-token", result.Token);
        
        Assert.Equal("testuser", result.Username);
        Assert.Equal("Test User", result.Name);
        Assert.Equal(1, result.UserRoleId);
        Assert.Equal("Administrator", result.UserRole);
        Assert.Equal("Login successful", result.Message);
        Assert.NotNull(result.ExpiresAt);

        // Verify security operations were called
        _userRepositoryMock.Verify(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateLastLoginAsync(1, It.IsAny<CancellationToken>()), Times.Once);
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

        var user = new UserMasterEntity
        {
            Id = 1,
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            UserRoleID = null,
            FailedLoginCount = 0
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);
        _tokenServiceMock.Setup(x => x.GenerateToken(1, "testuser", null))
            .Returns("mock-jwt-token");
        _userRepositoryMock.Setup(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.UpdateLastLoginAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.UserRoleId);
        Assert.Null(result.UserRole);
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
            .ReturnsAsync((UserMasterEntity?)null);

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

        var user = new UserMasterEntity
        {
            Id = 1,
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
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

        var user = new UserMasterEntity
        {
            Id = 1,
            UserName = "inactiveuser",
            UserNameNormalized = "INACTIVEUSER",
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
        var user = new UserMasterEntity
        {
            Id = 1,
            UserName = "lockeduser",
            UserNameNormalized = "LOCKEDUSER",
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
        var user = new UserMasterEntity
        {
            Id = 1,
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            LockedUntilAt = expiredLockout,
            FailedLoginCount = 5,
            UserRoleID = 1
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);
        _tokenServiceMock.Setup(x => x.GenerateToken(1, "testuser", 1))
            .Returns("mock-jwt-token");
        _userRoleRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRoleMasterEntity { Id = 1, UserRoleName = "User" });
        _userRepositoryMock.Setup(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.UpdateLastLoginAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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

        var user = new UserMasterEntity
        {
            Id = 1,
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
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

        var user = new UserMasterEntity
        {
            Id = 1,
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
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

    #region Token Expiration Tests

    [Fact]
    public async Task LoginAsync_UsesConfiguredTokenExpiration()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var user = new UserMasterEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            UserRoleID = 1
        };

        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("60"); // 1 hour
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);
        _tokenServiceMock.Setup(x => x.GenerateToken(1, "testuser", 1))
            .Returns("mock-jwt-token");
        _userRoleRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRoleMasterEntity { Id = 1, UserRoleName = "User" });
        _userRepositoryMock.Setup(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.UpdateLastLoginAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var beforeLogin = DateTime.Now;

        // Act
        var result = await _authService.LoginAsync(request);

        var afterLogin = DateTime.Now;

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ExpiresAt);
        
        // Verify expiration is approximately 60 minutes from now (within 1 minute tolerance)
        var expectedExpiration = beforeLogin.AddMinutes(60);
        Assert.True(result.ExpiresAt >= expectedExpiration.AddMinutes(-1));
        Assert.True(result.ExpiresAt <= afterLogin.AddMinutes(61));
    }

    [Fact]
    public async Task LoginAsync_WithInvalidExpirationConfig_UsesDefaultExpiration()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var user = new UserMasterEntity
        {
            Id = 1,
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            PasswordHash = "$2a$12$hashedpassword",
            IsActive = true,
            UserRoleID = 1
        };

        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("invalid"); // Invalid config
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("ValidPassword123", "$2a$12$hashedpassword"))
            .Returns(true);
        _tokenServiceMock.Setup(x => x.GenerateToken(1, "testuser", 1))
            .Returns("mock-jwt-token");
        _userRoleRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRoleMasterEntity { Id = 1, UserRoleName = "User" });
        _userRepositoryMock.Setup(x => x.ResetFailedLoginCountAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.UpdateLastLoginAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var beforeLogin = DateTime.Now;

        // Act
        var result = await _authService.LoginAsync(request);

        var afterLogin = DateTime.Now;

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ExpiresAt);
        
        // Verify default expiration of 60 minutes is used
        var expectedExpiration = beforeLogin.AddMinutes(60);
        Assert.True(result.ExpiresAt >= expectedExpiration.AddMinutes(-1));
        Assert.True(result.ExpiresAt <= afterLogin.AddMinutes(61));
    }

    #endregion
}
