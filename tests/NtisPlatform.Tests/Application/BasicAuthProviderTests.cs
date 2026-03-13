using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces.Auth;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services.Auth;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class BasicAuthProviderTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<BasicAuthProvider>> _loggerMock;
    private readonly BasicAuthProvider _authProvider;
    private static int _userIdCounter = 1;

    public BasicAuthProviderTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = new Mock<ILogger<BasicAuthProvider>>();

        _authProvider = new BasicAuthProvider(_context, _passwordHasherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void ProviderType_ShouldReturnBasic()
    {
        // Act
        var providerType = _authProvider.ProviderType;

        // Assert
        Assert.Equal(AuthProviderType.Basic, providerType);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", isActive: true);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Refresh user from context to get updated values
        await _context.Entry(user).ReloadAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AuthResultStatus.Success, result.Status);
        Assert.NotNull(result.User);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal(user.Username, result.User.Username);
        Assert.Equal(user.Email, result.User.Email);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.False(user.IsLocked);
        Assert.Null(user.LockoutEnd);
        Assert.NotNull(user.LastLoginAt);
    }

    [Fact]
    public async Task AuthenticateAsync_WithEmailInsteadOfUsername_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", isActive: true);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "test@example.com",
            Password = "password123"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.User);
        Assert.Equal(user.Email, result.User.Email);
    }

    [Fact]
    public async Task AuthenticateAsync_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistent",
            Password = "password123"
        };

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultStatus.InvalidCredentials, result.Status);
        Assert.Equal("Invalid username or password", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsync_WithLockedAccount_ShouldReturnAccountLocked()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", isActive: true);
        user.IsLocked = true;
        user.LockoutEnd = DateTime.Now.AddMinutes(10);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultStatus.AccountLocked, result.Status);
        Assert.Contains("Account is locked until", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsync_WithExpiredLockout_ShouldProceedWithAuthentication()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", isActive: true);
        user.IsLocked = true;
        user.LockoutEnd = DateTime.Now.AddMinutes(-10); // Expired lockout
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AuthResultStatus.Success, result.Status);
    }

    [Fact]
    public async Task AuthenticateAsync_WithDisabledAccount_ShouldReturnAccountDisabled()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", isActive: false);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultStatus.AccountDisabled, result.Status);
        Assert.Equal("Account is disabled", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ShouldIncrementFailedAttempts()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", isActive: true);
        user.FailedLoginAttempts = 2;
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Refresh user from context
        await _context.Entry(user).ReloadAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultStatus.InvalidCredentials, result.Status);
        Assert.Equal(3, user.FailedLoginAttempts);
        Assert.False(user.IsLocked);
    }

    [Fact]
    public async Task AuthenticateAsync_WithMaxFailedAttempts_ShouldLockAccount()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", isActive: true);
        user.FailedLoginAttempts = 4; // One more failed attempt will lock
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Refresh user from context
        await _context.Entry(user).ReloadAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultStatus.InvalidCredentials, result.Status);
        Assert.Equal(5, user.FailedLoginAttempts);
        Assert.True(user.IsLocked);
        Assert.NotNull(user.LockoutEnd);
        Assert.True(user.LockoutEnd > DateTime.Now.AddMinutes(-1)); // Allow small time difference
    }

    [Fact]
    public async Task AuthenticateAsync_WithTwoFactorRequired_ShouldReturnTwoFactorRequired()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", isActive: true);
        user.RequiresTwoFactor = true;
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultStatus.TwoFactorRequired, result.Status);
        Assert.NotNull(result.User);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidTwoFactorCode_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", isActive: true);
        user.RequiresTwoFactor = true;
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123",
            TwoFactorCode = "123456"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AuthResultStatus.Success, result.Status);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidTwoFactorCode_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser("testuser", "test@example.com", isActive: true);
        user.RequiresTwoFactor = true;
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123",
            TwoFactorCode = "000000"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultStatus.InvalidCredentials, result.Status);
        Assert.Equal("Invalid two-factor code", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsync_WithUserRoles_ShouldIncludeRolesInUserInfo()
    {
        // Arrange
        var role1 = new Role { Id = 1, Name = "Admin" };
        var role2 = new Role { Id = 2, Name = "User" };
        await _context.Roles.AddRangeAsync(role1, role2);
        await _context.SaveChangesAsync();

        var user = CreateTestUser("testuser", "test@example.com", isActive: true);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var userRole1 = new UserRole { UserId = user.Id, RoleId = role1.Id };
        var userRole2 = new UserRole { UserId = user.Id, RoleId = role2.Id };
        await _context.UserRoles.AddRangeAsync(userRole1, userRole2);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _authProvider.AuthenticateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.User);
        Assert.NotNull(result.User.Roles);
        Assert.Equal(2, result.User.Roles.Count);
        Assert.Contains("Admin", result.User.Roles);
        Assert.Contains("User", result.User.Roles);
    }

    [Fact]
    public async Task AuthenticateAsync_WithException_ShouldThrowException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var disposableContext = new ApplicationDbContext(options);
        var authProvider = new BasicAuthProvider(disposableContext, _passwordHasherMock.Object, _loggerMock.Object);

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        // Dispose context to trigger exception
        await disposableContext.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await authProvider.AuthenticateAsync(request));
    }

    [Fact]
    public async Task AuthenticateAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _authProvider.AuthenticateAsync(request, cts.Token));
    }

    [Fact]
    public async Task ValidateTwoFactorAsync_WithValidCode_ShouldReturnTrue()
    {
        // Act
        var result = await _authProvider.ValidateTwoFactorAsync(1, "123456");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTwoFactorAsync_WithInvalidCode_ShouldReturnFalse()
    {
        // Act
        var result = await _authProvider.ValidateTwoFactorAsync(1, "000000");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WithEnabledProvider_ShouldReturnTrue()
    {
        // Arrange
        var authProvider = new AuthProvider
        {
            Id = 1,
            ProviderType = "Basic",
            IsEnabled = true
        };
        await _context.AuthProviders.AddAsync(authProvider);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authProvider.IsEnabledAsync("org123");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WithDisabledProvider_ShouldReturnFalse()
    {
        // Arrange
        var authProvider = new AuthProvider
        {
            Id = 1,
            ProviderType = "Basic",
            IsEnabled = false
        };
        await _context.AuthProviders.AddAsync(authProvider);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authProvider.IsEnabledAsync("org123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WithNoProvider_ShouldReturnFalse()
    {
        // Act
        var result = await _authProvider.IsEnabledAsync("org123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _authProvider.IsEnabledAsync("org123", cts.Token));
    }

    private static User CreateTestUser(string username, string email, bool isActive)
    {
        return new User
        {
            Id = Interlocked.Increment(ref _userIdCounter),
            Username = username,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashedpassword",
            IsActive = isActive,
            IsLocked = false,
            FailedLoginAttempts = 0,
            RequiresTwoFactor = false,
            UserRoles = new List<UserRole>()
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}