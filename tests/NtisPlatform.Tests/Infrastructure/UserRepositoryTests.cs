using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;

namespace NtisPlatform.Tests.Infrastructure;

/// <summary>
/// Unit tests for UserRepository
/// Tests user lookup, failed login tracking, and lockout functionality
/// </summary>
public class UserRepositoryTests
{
    private readonly Mock<ISecuritySettingsService> _securitySettingsMock;

    public UserRepositoryTests() 
    {
        _securitySettingsMock = new Mock<ISecuritySettingsService>();
        
        // Setup default security settings (5 attempts, 30 minute lockout)
        _securitySettingsMock
            .Setup(x => x.GetAsync<int>("MaxFailedAttempts", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _securitySettingsMock
            .Setup(x => x.GetAsync<int>("LockoutDurationMinutes", 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(30);
    }

    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    #region GetByUsernameAsync Tests

    [Fact]
    public async Task GetByUsernameAsync_WithExistingUsername_ReturnsUser()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByUsernameAsync("testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.UserName);
        Assert.Equal("Test User", result.Name);
    }

    [Fact]
    public async Task GetByUsernameAsync_IsCaseInsensitive()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "TestUser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();

        // Act - Search with different casing
        var result1 = await repository.GetByUsernameAsync("testuser");
        var result2= await repository.GetByUsernameAsync("TESTUSER");
        var result3 = await repository.GetByUsernameAsync("TestUser");

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal("TestUser", result1.UserName);
        Assert.Equal("TestUser", result2.UserName);
        Assert.Equal("TestUser", result3.UserName);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithNonExistentUsername_ReturnsNull()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        // Act
        var result = await repository.GetByUsernameAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithMultipleUsers_ReturnsCorrectUser()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var users = new[]
        {
            new UserMasterEntity
            {
                UserName = "user1",
                UserNameNormalized = "USER1",
                Name = "User One",
                PasswordHash = "$2a$12$hash1",
                IsActive = true
            },
            new UserMasterEntity
            {
                UserName = "user2",
                UserNameNormalized = "USER2",
                Name = "User Two",
                PasswordHash = "$2a$12$hash2",
                IsActive = true
            },
            new UserMasterEntity
            {
                UserName = "user3",
                UserNameNormalized = "USER3",
                Name = "User Three",
                PasswordHash = "$2a$12$hash3",
                IsActive = true
            }
        };

        context.UserMasters.AddRange(users);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByUsernameAsync("user2");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user2", result.UserName);
        Assert.Equal("User Two", result.Name);
    }

    #endregion

    #region UpdateLastLoginAsync Tests

    [Fact]
    public async Task UpdateLastLoginAsync_WithExistingUser_UpdatesLastLoginTimestamp()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            LastLoginAt = null
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        var beforeUpdate = DateTime.Now.AddSeconds(-1);

        // Act
        await repository.UpdateLastLoginAsync(userId);

        var afterUpdate = DateTime.Now.AddSeconds(1);

        // Assert
        var updatedUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.LastLoginAt);
        Assert.True(updatedUser.LastLoginAt >= beforeUpdate);
        Assert.True(updatedUser.LastLoginAt <= afterUpdate);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_WithNonExistentUser_DoesNothing()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        // Act - Should not throw exception
        await repository.UpdateLastLoginAsync(9999);

        // Assert - No exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_UpdatesExistingTimestamp()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var oldTimestamp = DateTime.Now.AddDays(-7);
        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            LastLoginAt = oldTimestamp
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        // Act
        await repository.UpdateLastLoginAsync(userId);

        // Assert
        var updatedUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.LastLoginAt);
        Assert.True(updatedUser.LastLoginAt > oldTimestamp);
    }

    #endregion

    #region IncrementFailedLoginCountAsync Tests

    [Fact]
    public async Task IncrementFailedLoginCountAsync_IncreasesCount()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            FailedLoginCount = 2
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        // Act
        await repository.IncrementFailedLoginCountAsync(userId);

        // Assert
        var updatedUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(3, updatedUser.FailedLoginCount);
        Assert.Null(updatedUser.LockedUntilAt); // Not locked yet
    }

    [Fact]
    public async Task IncrementFailedLoginCountAsync_FromNull_SetsToOne()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            FailedLoginCount = null
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        // Act
        await repository.IncrementFailedLoginCountAsync(userId);

        // Assert
        var updatedUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(1, updatedUser.FailedLoginCount);
    }

    [Fact]
    public async Task IncrementFailedLoginCountAsync_AtFifthAttempt_LocksAccount()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            FailedLoginCount = 4 // 4 failed attempts already
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        var beforeLock = DateTime.Now;

        // Act
        await repository.IncrementFailedLoginCountAsync(userId);

        var afterLock = DateTime.Now;

        // Assert
        var updatedUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(5, updatedUser.FailedLoginCount);
        Assert.NotNull(updatedUser.LockedUntilAt);
        
        // Verify lockout is approximately 30 minutes from now
        var expectedLockout = beforeLock.AddMinutes(30);
        Assert.True(updatedUser.LockedUntilAt >= expectedLockout.AddSeconds(-1));
        Assert.True(updatedUser.LockedUntilAt <= afterLock.AddMinutes(30).AddSeconds(1));
    }

    [Fact]
    public async Task IncrementFailedLoginCountAsync_BeyondFifthAttempt_KeepsAccountLocked()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var existingLockout = DateTime.Now.AddMinutes(15);
        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            FailedLoginCount = 6,
            LockedUntilAt = existingLockout
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        // Act
        await repository.IncrementFailedLoginCountAsync(userId);

        // Assert
        var updatedUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(7, updatedUser.FailedLoginCount);
        Assert.NotNull(updatedUser.LockedUntilAt);
        // Lockout time should be updated to a new 30 minute period
        Assert.True(updatedUser.LockedUntilAt > existingLockout);
    }

    [Fact]
    public async Task IncrementFailedLoginCountAsync_WithNonExistentUser_DoesNothing()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        // Act - Should not throw exception
        await repository.IncrementFailedLoginCountAsync(9999);

        // Assert - No exception thrown
        Assert.True(true);
    }

    #endregion

    #region ResetFailedLoginCountAsync Tests

    [Fact]
    public async Task ResetFailedLoginCountAsync_ResetsCountToZero()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            FailedLoginCount = 3
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        // Act
        await repository.ResetFailedLoginCountAsync(userId);

        // Assert
        var updatedUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(0, updatedUser.FailedLoginCount);
        Assert.Null(updatedUser.LockedUntilAt);
    }

    [Fact]
    public async Task ResetFailedLoginCountAsync_ClearsLockout()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            FailedLoginCount = 5,
            LockedUntilAt = DateTime.Now.AddMinutes(30)
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        // Act
        await repository.ResetFailedLoginCountAsync(userId);

        // Assert
        var updatedUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(0, updatedUser.FailedLoginCount);
        Assert.Null(updatedUser.LockedUntilAt); // Lockout cleared
    }

    [Fact]
    public async Task ResetFailedLoginCountAsync_WithAlreadyZeroCount_RemainsZero()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            FailedLoginCount = 0
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        // Act
        await repository.ResetFailedLoginCountAsync(userId);

        // Assert
        var updatedUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(0, updatedUser.FailedLoginCount);
    }

    [Fact]
    public async Task ResetFailedLoginCountAsync_WithNonExistentUser_DoesNothing()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        // Act - Should not throw exception
        await repository.ResetFailedLoginCountAsync(9999);

        // Assert - No exception thrown
        Assert.True(true);
    }

    #endregion

    #region Integration Tests - Full Login Attempt Scenarios

    [Fact]
    public async Task FullLoginScenario_SuccessfulLoginAfterFailedAttempts()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            FailedLoginCount = 0
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        // Simulate 3 failed login attempts
        await repository.IncrementFailedLoginCountAsync(userId);
        await repository.IncrementFailedLoginCountAsync(userId);
        await repository.IncrementFailedLoginCountAsync(userId);

        var userAfterFails = await context.UserMasters.FindAsync(userId);
        Assert.Equal(3, userAfterFails!.FailedLoginCount);

        // Successful login - reset count and update last login
        await repository.ResetFailedLoginCountAsync(userId);
        await repository.UpdateLastLoginAsync(userId);

        // Assert
        var finalUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(finalUser);
        Assert.Equal(0, finalUser.FailedLoginCount);
        Assert.NotNull(finalUser.LastLoginAt);
        Assert.Null(finalUser.LockedUntilAt);
    }

    [Fact]
    public async Task FullLoginScenario_AccountLocksAfterFiveFailedAttempts()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new UserRepository(context, _securitySettingsMock.Object);

        var user = new UserMasterEntity
        {
            UserName = "testuser",
            UserNameNormalized = "TESTUSER",
            Name = "Test User",
            PasswordHash = "$2a$12$hash",
            IsActive = true,
            FailedLoginCount = 0
        };

        context.UserMasters.Add(user);
        await context.SaveChangesAsync();
        var userId = user.UserId;

        // Simulate 5 failed login attempts
        for (int i = 0; i < 5; i++)
        {
            await repository.IncrementFailedLoginCountAsync(userId);
        }

        // Assert
        var lockedUser = await context.UserMasters.FindAsync(userId);
        Assert.NotNull(lockedUser);
        Assert.Equal(5, lockedUser.FailedLoginCount);
        Assert.NotNull(lockedUser.LockedUntilAt);
        Assert.True(lockedUser.LockedUntilAt > DateTime.Now);
    }

    #endregion
}
