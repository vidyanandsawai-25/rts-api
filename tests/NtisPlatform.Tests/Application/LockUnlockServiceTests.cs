using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;

namespace NtisPlatform.Tests.Application;

public class LockUnlockServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<LockUnlockService>> _mockLogger;
    private readonly LockUnlockService _service;

    public LockUnlockServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<LockUnlockService>>();
        _service = new LockUnlockService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Seed ScreenMaster data
        var screens = new List<ScreenMasterEntity>
        {
            new() { Id = 1, ScreenCode = "SCR001", ScreenName = "Basic Details", ScreenNameLocal = "मूल विवरण", IsActive = true, IsPropertyLockable = true, DisplayOrder = 1 },
            new() { Id = 2, ScreenCode = "SCR002", ScreenName = "Tax Details", ScreenNameLocal = "कर विवरण", IsActive = true, IsPropertyLockable = true, DisplayOrder = 2 },
            new() { Id = 3, ScreenCode = "SCR003", ScreenName = "Floor Details", ScreenNameLocal = "मंजिल विवरण", IsActive = true, IsPropertyLockable = true, DisplayOrder = 3 },
            new() { Id = 4, ScreenCode = "SCR004", ScreenName = "Inactive Screen", IsActive = false, IsPropertyLockable = true, DisplayOrder = 4 },
            new() { Id = 5, ScreenCode = "SCR005", ScreenName = "Non-Lockable Screen", IsActive = true, IsPropertyLockable = false, DisplayOrder = 5 },
        };
        _context.ScreenMaster.AddRange(screens);

        // Seed Ward data
        var ward = new WardEntity { Id = 1, WardNo = "W001", Description = "Ward 1", IsActive = true };
        _context.WardMaster.Add(ward);

        // Seed PropertyMast data
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, PropertyNo = "P001", PartitionNo = "A", WardId = 1, IsActive = true },
            new() { Id = 2, PropertyNo = "P002", PartitionNo = "B", WardId = 1, IsActive = true },
            new() { Id = 3, PropertyNo = "P003", PartitionNo = "C", WardId = 1, IsActive = true },
        };
        _context.PropertyMast.AddRange(properties);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetLockableScreensAsync Tests

    [Fact]
    public async Task GetLockableScreensAsync_ReturnsOnlyActiveAndLockableScreens()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // Only 3 screens are active AND lockable
        Assert.All(result, screen => Assert.NotEmpty(screen.ScreenCode));
    }

    [Fact]
    public async Task GetLockableScreensAsync_ReturnsScreensOrderedByDisplayOrderThenByName()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(CancellationToken.None);

        // Assert
        Assert.Equal("Basic Details", result[0].ScreenName);
        Assert.Equal("Tax Details", result[1].ScreenName);
        Assert.Equal("Floor Details", result[2].ScreenName);
    }

    [Fact]
    public async Task GetLockableScreensAsync_ReturnsCorrectDtoProperties()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(CancellationToken.None);

        // Assert
        var firstScreen = result.First();
        Assert.Equal(1, firstScreen.Id);
        Assert.Equal("SCR001", firstScreen.ScreenCode);
        Assert.Equal("Basic Details", firstScreen.ScreenName);
        Assert.Equal("मूल विवरण", firstScreen.ScreenNameLocal);
        Assert.Equal(1, firstScreen.DisplayOrder);
    }

    [Fact]
    public async Task GetLockableScreensAsync_ExcludesInactiveScreens()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(CancellationToken.None);

        // Assert
        Assert.DoesNotContain(result, s => s.ScreenCode == "SCR004"); // Inactive screen
    }

    [Fact]
    public async Task GetLockableScreensAsync_ExcludesNonLockableScreens()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(CancellationToken.None);

        // Assert
        Assert.DoesNotContain(result, s => s.ScreenCode == "SCR005"); // Non-lockable screen
    }

    [Fact]
    public async Task GetLockableScreensAsync_ReturnsEmptyList_WhenNoLockableScreensExist()
    {
        // Arrange - Remove all lockable screens
        _context.ScreenMaster.RemoveRange(_context.ScreenMaster);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLockableScreensAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetPropertyLocksAsync Tests

    [Fact]
    public async Task GetPropertyLocksAsync_ThrowsArgumentException_WhenWardIdIsZero()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 0 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetPropertyLocksAsync(request, CancellationToken.None));
        Assert.Equal("WardId is required.", exception.Message);
    }

    [Fact]
    public async Task GetPropertyLocksAsync_ThrowsArgumentException_WhenWardIdIsNegative()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = -1 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetPropertyLocksAsync(request, CancellationToken.None));
        Assert.Equal("WardId is required.", exception.Message);
    }

    [Fact]
    public async Task GetPropertyLocksAsync_ReturnsPropertiesForWard()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 1, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task GetPropertyLocksAsync_FiltersByFromPropertyNo()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 1, FromPropertyNo = "P002", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, p => Assert.True(string.Compare(p.PropertyNo, "P002") >= 0));
    }

    [Fact]
    public async Task GetPropertyLocksAsync_FiltersByToPropertyNo()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 1, ToPropertyNo = "P002", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, p => Assert.True(string.Compare(p.PropertyNo, "P002") <= 0));
    }

    [Fact]
    public async Task GetPropertyLocksAsync_FiltersByPropertyRange()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto 
        { 
            WardId = 1, 
            FromPropertyNo = "P001", 
            ToPropertyNo = "P002", 
            PageNumber = 1, 
            PageSize = 10 
        };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetPropertyLocksAsync_FiltersBySearchTerm()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 1, Search = "P001", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("P001", result.Items.First().PropertyNo);
    }

    [Fact]
    public async Task GetPropertyLocksAsync_ReturnsPaginatedResults()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 1, PageNumber = 1, PageSize = 2 };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetPropertyLocksAsync_ReturnsAllResults_WhenPageSizeIsMinusOne()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 1, PageNumber = 1, PageSize = -1 };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task GetPropertyLocksAsync_ReturnsLockedScreensForProperty()
    {
        // Arrange - Add a lock
        _context.PropertyScreenLocks.Add(new PropertyScreenLockEntity
        {
            PropertyId = 1,
            LockableScreenId = 1,
            IsLocked = true,
            IsActive = true,
            LockedBy = 1,
            LockedDate = DateTime.Now,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        });
        await _context.SaveChangesAsync();

        var request = new FilterPropertyLocksRequestDto { WardId = 1, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        var lockedProperty = result.Items.First(p => p.PropertyId == 1);
        Assert.True(lockedProperty.IsLocked);
        Assert.Single(lockedProperty.LockedScreens);
        Assert.Equal("Basic Details", lockedProperty.LockedScreens.First().ScreenName);
    }

    [Fact]
    public async Task GetPropertyLocksAsync_ExcludesMarkedForDeletionLocks()
    {
        // Arrange - Add a lock marked for deletion
        _context.PropertyScreenLocks.Add(new PropertyScreenLockEntity
        {
            PropertyId = 1,
            LockableScreenId = 1,
            IsLocked = true,
            IsActive = true,
            MarkedForDeletion = true,
            LockedBy = 1,
            LockedDate = DateTime.Now,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        });
        await _context.SaveChangesAsync();

        var request = new FilterPropertyLocksRequestDto { WardId = 1, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        var property = result.Items.First(p => p.PropertyId == 1);
        Assert.False(property.IsLocked);
        Assert.Empty(property.LockedScreens);
    }

    #endregion

    #region BulkApplyAsync Tests - Validation

    [Fact]
    public async Task BulkApplyAsync_ThrowsArgumentException_WhenActionIsInvalid()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "invalid"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkApplyAsync(request, 1, CancellationToken.None));
        Assert.Equal("Action must be 'lock' or 'unlock'.", exception.Message);
    }

    [Fact]
    public async Task BulkApplyAsync_ThrowsArgumentException_WhenActionIsEmpty()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = ""
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkApplyAsync(request, 1, CancellationToken.None));
        Assert.Equal("Action must be 'lock' or 'unlock'.", exception.Message);
    }

    [Fact]
    public async Task BulkApplyAsync_ThrowsArgumentException_WhenPropertyIdsIsNull()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = null!,
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkApplyAsync(request, 1, CancellationToken.None));
        Assert.Equal("At least one property must be selected.", exception.Message);
    }

    [Fact]
    public async Task BulkApplyAsync_ThrowsArgumentException_WhenPropertyIdsIsEmpty()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int>(),
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkApplyAsync(request, 1, CancellationToken.None));
        Assert.Equal("At least one property must be selected.", exception.Message);
    }

    [Fact]
    public async Task BulkApplyAsync_ThrowsArgumentException_WhenScreenIdsIsNull()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = null!,
            Action = "lock"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkApplyAsync(request, 1, CancellationToken.None));
        Assert.Equal("At least one screen must be selected.", exception.Message);
    }

    [Fact]
    public async Task BulkApplyAsync_ThrowsArgumentException_WhenScreenIdsIsEmpty()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int>(),
            Action = "lock"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkApplyAsync(request, 1, CancellationToken.None));
        Assert.Equal("At least one screen must be selected.", exception.Message);
    }

    [Theory]
    [InlineData("lock")]
    [InlineData("Lock")]
    [InlineData("LOCK")]
    [InlineData(" lock ")]
    public async Task BulkApplyAsync_AcceptsLockActionCaseInsensitive(string action)
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = action
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);
    }

    [Theory]
    [InlineData("unlock")]
    [InlineData("Unlock")]
    [InlineData("UNLOCK")]
    [InlineData(" unlock ")]
    public async Task BulkApplyAsync_AcceptsUnlockActionCaseInsensitive(string action)
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = action
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);
    }

    #endregion

    #region BulkApplyAsync Tests - Lock Operations

    [Fact]
    public async Task BulkApplyAsync_Lock_CreatesNewLockRecord()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);

        var lockRecord = await _context.PropertyScreenLocks.FirstOrDefaultAsync(l => l.PropertyId == 1 && l.LockableScreenId == 1);
        Assert.NotNull(lockRecord);
        Assert.True(lockRecord.IsLocked);
        Assert.Equal(1, lockRecord.LockedBy);
        Assert.NotNull(lockRecord.LockedDate);
        Assert.Equal(1, lockRecord.CreatedBy);
        Assert.NotNull(lockRecord.CreatedDate);
    }

    [Fact]
    public async Task BulkApplyAsync_Lock_SetsCorrectAuditFields()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };
        var userId = 123;

        // Act
        await _service.BulkApplyAsync(request, userId, CancellationToken.None);

        // Assert
        var lockRecord = await _context.PropertyScreenLocks.FirstOrDefaultAsync();
        Assert.NotNull(lockRecord);
        Assert.Equal(userId, lockRecord.CreatedBy);
        Assert.Equal(userId, lockRecord.LockedBy);
        Assert.True(lockRecord.IsActive);
        Assert.False(lockRecord.MarkedForDeletion);
    }

    [Fact]
    public async Task BulkApplyAsync_Lock_UpdatesExistingUnlockedRecord()
    {
        // Arrange - Create an existing unlocked record
        var existingLock = new PropertyScreenLockEntity
        {
            PropertyId = 1,
            LockableScreenId = 1,
            IsLocked = false,
            IsActive = true,
            UnlockedBy = 1,
            UnlockedDate = DateTime.Now.AddDays(-1),
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-1)
        };
        _context.PropertyScreenLocks.Add(existingLock);
        await _context.SaveChangesAsync();

        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 2, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);

        var lockRecord = await _context.PropertyScreenLocks.FirstOrDefaultAsync(l => l.PropertyId == 1 && l.LockableScreenId == 1);
        Assert.NotNull(lockRecord);
        Assert.True(lockRecord.IsLocked);
        Assert.Equal(2, lockRecord.LockedBy);
        Assert.Equal(2, lockRecord.UpdatedBy);
        Assert.NotNull(lockRecord.LockedDate);
        Assert.NotNull(lockRecord.UpdatedDate);
    }

    [Fact]
    public async Task BulkApplyAsync_Lock_HandlesMultiplePropertiesAndScreens()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1, 2, 3 },
            ScreenIds = new List<int> { 1, 2 },
            Action = "lock"
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(6, result.TotalRequested); // 3 properties x 2 screens
        Assert.Equal(6, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);

        var lockCount = await _context.PropertyScreenLocks.CountAsync();
        Assert.Equal(6, lockCount);
    }

    #endregion

    #region BulkApplyAsync Tests - Unlock Operations

    [Fact]
    public async Task BulkApplyAsync_Unlock_CreatesNewUnlockRecord()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "unlock"
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);

        var lockRecord = await _context.PropertyScreenLocks.FirstOrDefaultAsync();
        Assert.NotNull(lockRecord);
        Assert.False(lockRecord.IsLocked);
        Assert.Equal(1, lockRecord.UnlockedBy);
        Assert.NotNull(lockRecord.UnlockedDate);
    }

    [Fact]
    public async Task BulkApplyAsync_Unlock_UpdatesExistingLockedRecord()
    {
        // Arrange - Create an existing locked record
        var existingLock = new PropertyScreenLockEntity
        {
            PropertyId = 1,
            LockableScreenId = 1,
            IsLocked = true,
            IsActive = true,
            LockedBy = 1,
            LockedDate = DateTime.Now.AddDays(-1),
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-1)
        };
        _context.PropertyScreenLocks.Add(existingLock);
        await _context.SaveChangesAsync();

        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "unlock"
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 2, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);

        var lockRecord = await _context.PropertyScreenLocks.FirstOrDefaultAsync();
        Assert.NotNull(lockRecord);
        Assert.False(lockRecord.IsLocked);
        Assert.Equal(2, lockRecord.UnlockedBy);
        Assert.Equal(2, lockRecord.UpdatedBy);
        Assert.NotNull(lockRecord.UnlockedDate);
        Assert.NotNull(lockRecord.UpdatedDate);
    }

    #endregion

    #region BulkApplyAsync Tests - Error Handling

    [Fact]
    public async Task BulkApplyAsync_ReturnsError_WhenPropertyNotFound()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 999 }, // Non-existent property
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("Property 999 not found or inactive.", result.Errors);
    }

    [Fact]
    public async Task BulkApplyAsync_ReturnsError_WhenScreenNotFound()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 999 }, // Non-existent screen
            Action = "lock"
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("Screen 999 not found or inactive.", result.Errors);
    }

    [Fact]
    public async Task BulkApplyAsync_ReturnsError_WhenScreenIsInactive()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 4 }, // Inactive screen
            Action = "lock"
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("Screen 4 not found or inactive.", result.Errors);
    }

    [Fact]
    public async Task BulkApplyAsync_HandlesPartialSuccess()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1, 999 }, // One valid, one invalid
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalRequested);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task BulkApplyAsync_HandlesExistingUnlockedRecordAndRelocks()
    {
        // Arrange - Create an existing unlocked record (simulating a previously unlocked property)
        var existingLock = new PropertyScreenLockEntity
        {
            PropertyId = 1,
            LockableScreenId = 1,
            IsLocked = false,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-10),
            UnlockedBy = 1,
            UnlockedDate = DateTime.Now.AddDays(-1)
        };
        _context.PropertyScreenLocks.Add(existingLock);
        await _context.SaveChangesAsync();

        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act
        var result = await _service.BulkApplyAsync(request, 2, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);

        var lockRecord = await _context.PropertyScreenLocks.FirstOrDefaultAsync();
        Assert.NotNull(lockRecord);
        Assert.True(lockRecord.IsLocked);
        Assert.Equal(2, lockRecord.LockedBy);
        Assert.Equal(2, lockRecord.UpdatedBy);
        Assert.NotNull(lockRecord.LockedDate);
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void BulkLockRequestDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new BulkLockRequestDto();

        // Assert
        Assert.NotNull(dto.PropertyIds);
        Assert.Empty(dto.PropertyIds);
        Assert.NotNull(dto.ScreenIds);
        Assert.Empty(dto.ScreenIds);
        Assert.Equal("lock", dto.Action);
    }

    [Fact]
    public void BulkLockResultDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new BulkLockResultDto();

        // Assert
        Assert.Equal(0, dto.TotalRequested);
        Assert.Equal(0, dto.SuccessCount);
        Assert.Equal(0, dto.FailedCount);
        Assert.NotNull(dto.Errors);
        Assert.Empty(dto.Errors);
    }

    [Fact]
    public void LockableScreenDto_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var dto = new LockableScreenDto
        {
            Id = 1,
            ScreenCode = "SCR001",
            ScreenName = "Test Screen",
            ScreenNameLocal = "टेस्ट स्क्रीन",
            DisplayOrder = 5
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("SCR001", dto.ScreenCode);
        Assert.Equal("Test Screen", dto.ScreenName);
        Assert.Equal("टेस्ट स्क्रीन", dto.ScreenNameLocal);
        Assert.Equal(5, dto.DisplayOrder);
    }

    [Fact]
    public void PropertyLockRowDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PropertyLockRowDto();

        // Assert
        Assert.Equal(0, dto.PropertyId);
        Assert.Equal(0, dto.WardId);
        Assert.Equal(string.Empty, dto.WardNo);
        Assert.Equal(string.Empty, dto.PropertyNo);
        Assert.Equal(string.Empty, dto.PartitionNo);
        Assert.False(dto.IsLocked);
        Assert.NotNull(dto.LockedScreens);
        Assert.Empty(dto.LockedScreens);
    }

    [Fact]
    public void FilterPropertyLocksRequestDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new FilterPropertyLocksRequestDto();

        // Assert
        Assert.Equal(0, dto.WardId);
        Assert.Equal(string.Empty, dto.FromPropertyNo);
        Assert.Equal(string.Empty, dto.ToPropertyNo);
        Assert.Null(dto.Search);
    }

    #endregion

    #region Entity Tests

    [Fact]
    public void PropertyScreenLockEntity_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var entity = new PropertyScreenLockEntity();

        // Assert
        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.PropertyId);
        Assert.Equal(0, entity.LockableScreenId);
        Assert.False(entity.IsLocked); // Default is false (C# default for bool)
        Assert.Null(entity.LockedBy);
        Assert.Null(entity.LockedDate);
        Assert.Null(entity.UnlockedBy);
        Assert.Null(entity.UnlockedDate);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyScreenLockEntity_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var now = DateTime.Now;
        var entity = new PropertyScreenLockEntity
        {
            Id = 1,
            PropertyId = 100,
            LockableScreenId = 200,
            IsLocked = true,
            LockedBy = 10,
            LockedDate = now,
            UnlockedBy = 20,
            UnlockedDate = now.AddHours(1),
            MarkedForDeletion = false,
            MarkedForDeletionDate = null,
            IsActive = true,
            CreatedBy = 10,
            CreatedDate = now,
            UpdatedBy = 20,
            UpdatedDate = now.AddHours(1)
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal(100, entity.PropertyId);
        Assert.Equal(200, entity.LockableScreenId);
        Assert.True(entity.IsLocked);
        Assert.Equal(10, entity.LockedBy);
        Assert.Equal(now, entity.LockedDate);
        Assert.Equal(20, entity.UnlockedBy);
        Assert.Equal(now.AddHours(1), entity.UnlockedDate);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
    }

    #endregion
}
