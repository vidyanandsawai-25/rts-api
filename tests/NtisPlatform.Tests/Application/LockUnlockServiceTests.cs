using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;

namespace NtisPlatform.Tests.Application;

public class LockUnlockServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<LockUnlockService>> _mockLogger;
    private readonly Mock<IPropertySearchService> _mockPropertySearchService;
    private readonly LockUnlockService _service;

    public LockUnlockServiceTests()
    {
        // BulkApplyAsync issues a raw T-SQL MERGE (with OPENJSON), which only a real SQL Server
        // engine can execute - the EF Core InMemory provider used here can't run that code path, so
        // tests exercising it aren't included in this class (see BulkApplyAsync Tests - Validation /
        // Error Handling below for the parts that don't require reaching that statement).
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<LockUnlockService>>();
        _mockPropertySearchService = new Mock<IPropertySearchService>();
        _service = new LockUnlockService(_context, _mockLogger.Object, _mockPropertySearchService.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Seed ModuleMaster data
        var modules = new List<ModuleMasterEntity>
        {
            new() { Id = 1, ModuleCode = "MOD001", ModuleName = "Property", ModuleNameLocal = "संपत्ति", DepartmentId = 1, IsActive = true },
            new() { Id = 2, ModuleCode = "MOD002", ModuleName = "Tax", ModuleNameLocal = "कर", DepartmentId = 1, IsActive = true },
        };
        _context.ModuleMasters.AddRange(modules);

        // Seed ScreenMaster data
        var screens = new List<ScreenMasterEntity>
        {
            new() { Id = 1, ScreenCode = "SCR001", ScreenName = "Basic Details", ScreenNameLocal = "मूल विवरण", ModuleId = 1, IsActive = true, IsPropertyLockable = true, DisplayOrder = 1 },
            new() { Id = 2, ScreenCode = "SCR002", ScreenName = "Tax Details", ScreenNameLocal = "कर विवरण", ModuleId = 2, IsActive = true, IsPropertyLockable = true, DisplayOrder = 2 },
            new() { Id = 3, ScreenCode = "SCR003", ScreenName = "Floor Details", ScreenNameLocal = "मंजिल विवरण", ModuleId = 1, IsActive = true, IsPropertyLockable = true, DisplayOrder = 3 },
            new() { Id = 4, ScreenCode = "SCR004", ScreenName = "Inactive Screen", ModuleId = 1, IsActive = false, IsPropertyLockable = true, DisplayOrder = 4 },
            new() { Id = 5, ScreenCode = "SCR005", ScreenName = "Non-Lockable Screen", ModuleId = 1, IsActive = true, IsPropertyLockable = false, DisplayOrder = 5 },
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
        _context.Dispose();
    }

    #region GetLockableScreensAsync Tests

    [Fact]
    public async Task GetLockableScreensAsync_ReturnsOnlyActiveAndLockableScreens()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(null, null, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // Only 3 screens are active AND lockable
        Assert.All(result, screen => Assert.NotEmpty(screen.ScreenCode));
    }

    [Fact]
    public async Task GetLockableScreensAsync_ReturnsScreensOrderedByDisplayOrderThenByName()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(null, null, null, CancellationToken.None);

        // Assert
        Assert.Equal("Basic Details", result[0].ScreenName);
        Assert.Equal("Tax Details", result[1].ScreenName);
        Assert.Equal("Floor Details", result[2].ScreenName);
    }

    [Fact]
    public async Task GetLockableScreensAsync_ReturnsCorrectDtoProperties()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(null, null, null, CancellationToken.None);

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
        var result = await _service.GetLockableScreensAsync(null, null, null, CancellationToken.None);

        // Assert
        Assert.DoesNotContain(result, s => s.ScreenCode == "SCR004"); // Inactive screen
    }

    [Fact]
    public async Task GetLockableScreensAsync_ExcludesNonLockableScreens()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(null, null, null, CancellationToken.None);

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
        var result = await _service.GetLockableScreensAsync(null, null, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLockableScreensAsync_FiltersScreensByScreenName()
    {
        // Act
        var result = await _service.GetLockableScreensAsync("Basic", null, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Basic Details", result[0].ScreenName);
    }

    [Fact]
    public async Task GetLockableScreensAsync_FiltersScreensByModuleName()
    {
        // Act
        var result = await _service.GetLockableScreensAsync("Tax", null, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Tax Details", result[0].ScreenName);
        Assert.Equal("Tax", result[0].ModuleName);
    }

    [Fact]
    public async Task GetLockableScreensAsync_ReturnsModuleFieldsWithScreens()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(null, null, null, CancellationToken.None);

        // Assert
        var basicDetailsScreen = result.First(s => s.ScreenCode == "SCR001");
        Assert.NotNull(basicDetailsScreen.ModuleId);
        Assert.Equal(1, basicDetailsScreen.ModuleId);
        Assert.Equal("MOD001", basicDetailsScreen.ModuleCode);
        Assert.Equal("Property", basicDetailsScreen.ModuleName);
        Assert.Equal("संपत्ति", basicDetailsScreen.ModuleNameLocal);
    }

    [Fact]
    public async Task GetLockableScreensAsync_FiltersScreensById()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(null, 1, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Basic Details", result[0].ScreenName);
    }

    [Fact]
    public async Task GetLockableScreensAsync_FiltersScreensByModuleId()
    {
        // Act
        var result = await _service.GetLockableScreensAsync(null, null, 1, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count); // SCR001 and SCR003 belong to module 1
        Assert.All(result, screen => Assert.Equal(1, screen.ModuleId));
    }

    [Fact]
    public async Task GetLockableScreensAsync_CombinesMultipleFilters()
    {
        // Act
        var result = await _service.GetLockableScreensAsync("Details", 1, 1, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Basic Details", result[0].ScreenName);
        Assert.Equal(1, result[0].ModuleId);
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
    public async Task GetPropertyLocksAsync_OrdersPropertyNoNaturally()
    {
        // Arrange
        _context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", Description = "Ward 2", IsActive = true });
        _context.PropertyMast.AddRange(
            new PropertyEntity { Id = 10, PropertyNo = "A10", WardId = 2, IsActive = true },
            new PropertyEntity { Id = 11, PropertyNo = "A2", WardId = 2, IsActive = true },
            new PropertyEntity { Id = 12, PropertyNo = "A1", WardId = 2, IsActive = true },
            new PropertyEntity { Id = 13, PropertyNo = "A3", WardId = 2, IsActive = true });
        await _context.SaveChangesAsync();

        var request = new FilterPropertyLocksRequestDto { WardId = 2, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(new[] { "A1", "A2", "A3", "A10" }, result.Items.Select(p => p.PropertyNo));
    }

    [Fact]
    public async Task GetPropertyLocksAsync_OrdersPartitionNoNaturallyWithinSamePropertyNo()
    {
        // Arrange - same PropertyNo, distinguished only by PartitionNo (matches real-world data)
        _context.WardMaster.Add(new WardEntity { Id = 3, WardNo = "W003", Description = "Ward 3", IsActive = true });
        _context.PropertyMast.AddRange(
            new PropertyEntity { Id = 20, PropertyNo = "1", PartitionNo = "A11", WardId = 3, IsActive = true },
            new PropertyEntity { Id = 21, PropertyNo = "1", PartitionNo = "A10", WardId = 3, IsActive = true },
            new PropertyEntity { Id = 22, PropertyNo = "1", PartitionNo = "A2", WardId = 3, IsActive = true },
            new PropertyEntity { Id = 23, PropertyNo = "1", PartitionNo = "A1", WardId = 3, IsActive = true },
            new PropertyEntity { Id = 24, PropertyNo = "1", PartitionNo = "", WardId = 3, IsActive = true });
        await _context.SaveChangesAsync();

        var request = new FilterPropertyLocksRequestDto { WardId = 3, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(new[] { "", "A1", "A2", "A10", "A11" }, result.Items.Select(p => p.PartitionNo));
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
    public async Task GetPropertyLocksAsync_FiltersByMultiplePartitionNumbers()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto
        {
            WardId = 1,
            PartitionNo = "A, C",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Collection(
            result.Items.OrderBy(p => p.PartitionNo),
            p => Assert.Equal("A", p.PartitionNo),
            p => Assert.Equal("C", p.PartitionNo));
    }

    [Fact]
    public async Task GetPropertyLocksAsync_IgnoresEmptyPartitionNumberEntries()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto
        {
            WardId = 1,
            PartitionNo = " , B, ",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("B", result.Items.First().PartitionNo);
    }

    [Fact]
    public async Task GetPropertyLocksAsync_IncludesBlankPartitionNumbers_WhenPartitionNoIsZero()
    {
        // Arrange
        _context.PropertyMast.AddRange(
            new PropertyEntity { Id = 4, PropertyNo = "P004", PartitionNo = string.Empty, WardId = 1, IsActive = true },
            new PropertyEntity { Id = 5, PropertyNo = "P005", PartitionNo = null, WardId = 1, IsActive = true });
        await _context.SaveChangesAsync();

        var request = new FilterPropertyLocksRequestDto
        {
            WardId = 1,
            PartitionNo = "0",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetPropertyLocksAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, p => Assert.Equal(string.Empty, p.PartitionNo));
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

    #region GetPropertyLocksByCategoryAsync Tests

    [Fact]
    public async Task GetPropertyLocksByCategoryAsync_DelegatesScopeToPropertySearchService()
    {
        // Arrange
        var request = new PropertySearchByCategoryQueryParameters
        {
            SearchCategory = PropertySearchCategory.WardWise,
            WardId = 1,
            PageNumber = 2,
            PageSize = 5,
        };
        _mockPropertySearchService
            .Setup(s => s.SearchByCategoryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PropertySearchByCategoryResponseDto>(
                new List<PropertySearchByCategoryResponseDto>(), 0, request.PageNumber, request.PageSize));

        // Act
        await _service.GetPropertyLocksByCategoryAsync(request, CancellationToken.None);

        // Assert - the exact same request instance is forwarded, so scope validation stays owned
        // by IPropertySearchService rather than being duplicated here.
        _mockPropertySearchService.Verify(
            s => s.SearchByCategoryAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPropertyLocksByCategoryAsync_PropagatesValidationException_FromPropertySearchService()
    {
        // Arrange - e.g. ZoneWise scope requested without ZoneId.
        var request = new PropertySearchByCategoryQueryParameters { SearchCategory = PropertySearchCategory.ZoneWise };
        _mockPropertySearchService
            .Setup(s => s.SearchByCategoryAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PropertyValidationException("ZoneId is required for ZoneWise search."));

        // Act & Assert
        await Assert.ThrowsAsync<PropertyValidationException>(
            () => _service.GetPropertyLocksByCategoryAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetPropertyLocksByCategoryAsync_MapsFieldsAndPassesThroughPaging()
    {
        // Arrange
        var request = new PropertySearchByCategoryQueryParameters { SearchCategory = PropertySearchCategory.WardWise, WardId = 1 };
        var searchItems = new List<PropertySearchByCategoryResponseDto>
        {
            new() { PropertyId = 1, WardId = 1, WardNo = "W001", PropertyNo = "P001", PartitionNo = "A" },
            new() { PropertyId = 2, WardId = 1, WardNo = "W001", PropertyNo = "P002", PartitionNo = "B" },
        };
        _mockPropertySearchService
            .Setup(s => s.SearchByCategoryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PropertySearchByCategoryResponseDto>(searchItems, totalCount: 42, pageNumber: 3, pageSize: 2));

        // Act
        var result = await _service.GetPropertyLocksByCategoryAsync(request, CancellationToken.None);

        // Assert - paging metadata is passed through untouched, and each item's identifying fields
        // are mapped across from the search response.
        Assert.Equal(42, result.TotalCount);
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Collection(
            result.Items,
            p => { Assert.Equal(1, p.PropertyId); Assert.Equal("W001", p.WardNo); Assert.Equal("P001", p.PropertyNo); Assert.Equal("A", p.PartitionNo); },
            p => { Assert.Equal(2, p.PropertyId); Assert.Equal("P002", p.PropertyNo); Assert.Equal("B", p.PartitionNo); });
    }

    [Fact]
    public async Task GetPropertyLocksByCategoryAsync_EnrichesEachPropertyWithItsOwnLockedScreens()
    {
        // Arrange - property 1 has an active lock, property 2 has none.
        _context.PropertyScreenLocks.Add(new PropertyScreenLockEntity
        {
            PropertyId = 1,
            LockableScreenId = 1,
            IsLocked = true,
            IsActive = true,
            LockedBy = 1,
            LockedDate = DateTime.Now,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
        });
        await _context.SaveChangesAsync();

        var request = new PropertySearchByCategoryQueryParameters { SearchCategory = PropertySearchCategory.WardWise, WardId = 1 };
        var searchItems = new List<PropertySearchByCategoryResponseDto>
        {
            new() { PropertyId = 1, WardId = 1, WardNo = "W001", PropertyNo = "P001", PartitionNo = "A" },
            new() { PropertyId = 2, WardId = 1, WardNo = "W001", PropertyNo = "P002", PartitionNo = "B" },
        };
        _mockPropertySearchService
            .Setup(s => s.SearchByCategoryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PropertySearchByCategoryResponseDto>(searchItems, searchItems.Count, 1, 10));

        // Act
        var result = await _service.GetPropertyLocksByCategoryAsync(request, CancellationToken.None);

        // Assert
        var lockedProperty = result.Items.First(p => p.PropertyId == 1);
        Assert.True(lockedProperty.IsLocked);
        Assert.Single(lockedProperty.LockedScreens);
        Assert.Equal("Basic Details", lockedProperty.LockedScreens.First().ScreenName);

        var unlockedProperty = result.Items.First(p => p.PropertyId == 2);
        Assert.False(unlockedProperty.IsLocked);
        Assert.Empty(unlockedProperty.LockedScreens);
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

    #endregion

    #region BulkApplyByCategoryAsync Tests

    private static BulkLockByCategoryRequestDto CategoryRequest(
        int wardId = 1, List<int>? screenIds = null, string action = "lock")
        => new()
        {
            Scope = new BulkLockCategoryScopeDto { SearchCategory = PropertySearchCategory.WardWise, WardId = wardId },
            ScreenIds = screenIds ?? new List<int> { 1 },
            Action = action,
        };

    [Fact]
    public async Task BulkApplyByCategoryAsync_MapsScopeAndDelegatesToPropertySearchService()
    {
        // Arrange
        var request = CategoryRequest(wardId: 7);
        request.Scope.PropertyNo = "P1";
        request.Scope.PartitionNo = "A";
        request.Scope.PropertyFrom = "P1";
        request.Scope.PropertyTo = "P9";

        Func<PropertySearchByCategoryQueryParameters, bool> matchesScope = q =>
            q.SearchCategory == request.Scope.SearchCategory &&
            q.WardId == request.Scope.WardId &&
            q.ZoneId == request.Scope.ZoneId &&
            q.PropertyNo == request.Scope.PropertyNo &&
            q.PartitionNo == request.Scope.PartitionNo &&
            q.PropertyFrom == request.Scope.PropertyFrom &&
            q.PropertyTo == request.Scope.PropertyTo;

        _mockPropertySearchService
            .Setup(s => s.ResolvePropertyIdsByCategoryAsync(It.Is<PropertySearchByCategoryQueryParameters>(q => matchesScope(q)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());

        // Act
        await _service.BulkApplyByCategoryAsync(request, 1, CancellationToken.None);

        // Assert - Scope's fields are forwarded verbatim; the extra grid-only filters on
        // PropertySearchByCategoryQueryParameters simply aren't populated (Scope has no analogue).
        // If the mapping were wrong, the Setup above wouldn't match and the awaited call would fail.
        _mockPropertySearchService.Verify(
            s => s.ResolvePropertyIdsByCategoryAsync(It.Is<PropertySearchByCategoryQueryParameters>(q => matchesScope(q)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BulkApplyByCategoryAsync_PropagatesValidationException_FromPropertySearchService()
    {
        // Arrange - e.g. ZoneWise scope requested without ZoneId.
        var request = CategoryRequest();
        request.Scope.SearchCategory = PropertySearchCategory.ZoneWise;
        request.Scope.WardId = null;

        _mockPropertySearchService
            .Setup(s => s.ResolvePropertyIdsByCategoryAsync(It.IsAny<PropertySearchByCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PropertyValidationException("ZoneId is required for ZoneWise search."));

        // Act & Assert
        await Assert.ThrowsAsync<PropertyValidationException>(
            () => _service.BulkApplyByCategoryAsync(request, 1, CancellationToken.None));
    }

    [Fact]
    public async Task BulkApplyByCategoryAsync_ReturnsZeroedResult_WhenScopeMatchesNoProperties()
    {
        // Arrange
        var request = CategoryRequest();
        _mockPropertySearchService
            .Setup(s => s.ResolvePropertyIdsByCategoryAsync(It.IsAny<PropertySearchByCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());

        // Act
        var result = await _service.BulkApplyByCategoryAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(await _context.PropertyScreenLocks.ToListAsync());
    }

    [Fact]
    public async Task BulkApplyByCategoryAsync_ReturnsError_WhenScreenNotFoundOrInactive()
    {
        // Arrange
        var request = CategoryRequest(screenIds: new List<int> { 1, 999, 4 }); // 999 missing, 4 inactive
        _mockPropertySearchService
            .Setup(s => s.ResolvePropertyIdsByCategoryAsync(It.IsAny<PropertySearchByCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1, 2 });

        // Act
        var result = await _service.BulkApplyByCategoryAsync(request, 1, CancellationToken.None);

        // Assert - only the one valid screen (Id=1) applies; TotalRequested still reflects all 3
        // requested screens x 2 properties, and the invalid ones are named in Errors.
        Assert.Equal(6, result.TotalRequested);
        Assert.Contains("Screen 999 not found or inactive.", result.Errors);
        Assert.Contains("Screen 4 not found or inactive.", result.Errors);
        Assert.Equal(2, result.SuccessCount); // 2 properties x 1 valid screen
        Assert.Equal(4, result.FailedCount);
    }

    [Fact]
    public async Task BulkApplyByCategoryAsync_DedupesDuplicateScreenIds_SoTotalRequestedMatchesDistinctPairs()
    {
        // Arrange - screen 1 requested twice; the operation only ever applies it once per property,
        // so TotalRequested/SuccessCount must be based on the distinct screen id, not the raw count.
        var request = CategoryRequest(screenIds: new List<int> { 1, 1 });
        _mockPropertySearchService
            .Setup(s => s.ResolvePropertyIdsByCategoryAsync(It.IsAny<PropertySearchByCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1, 2 });

        // Act
        var result = await _service.BulkApplyByCategoryAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalRequested); // 2 properties x 1 distinct screen
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task BulkApplyByCategoryAsync_Lock_CreatesNewLockRecordsForEveryResolvedPropertyAndScreen()
    {
        // Arrange - properties 1 and 2 have no existing PropertyScreenLock rows, so this only
        // exercises the insert phase. The update-existing-row phase uses ExecuteUpdateAsync, which
        // the EF Core InMemory provider used by these tests does not support, so it isn't covered here.
        var request = CategoryRequest(screenIds: new List<int> { 1, 2 });
        _mockPropertySearchService
            .Setup(s => s.ResolvePropertyIdsByCategoryAsync(It.IsAny<PropertySearchByCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1, 2 });

        // Act
        var result = await _service.BulkApplyByCategoryAsync(request, 5, CancellationToken.None);

        // Assert
        Assert.Equal(4, result.TotalRequested); // 2 properties x 2 screens
        Assert.Equal(4, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);

        var locks = await _context.PropertyScreenLocks.ToListAsync();
        Assert.Equal(4, locks.Count);
        Assert.All(locks, l =>
        {
            Assert.True(l.IsLocked);
            Assert.Equal(5, l.LockedBy);
            Assert.NotNull(l.LockedDate);
            Assert.Equal(5, l.CreatedBy);
        });
    }

    [Fact]
    public async Task BulkApplyByCategoryAsync_Unlock_CreatesNewUnlockRecords()
    {
        // Arrange
        var request = CategoryRequest(action: "unlock");
        _mockPropertySearchService
            .Setup(s => s.ResolvePropertyIdsByCategoryAsync(It.IsAny<PropertySearchByCategoryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1 });

        // Act
        var result = await _service.BulkApplyByCategoryAsync(request, 1, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        var lockRecord = await _context.PropertyScreenLocks.FirstOrDefaultAsync();
        Assert.NotNull(lockRecord);
        Assert.False(lockRecord.IsLocked);
        Assert.Equal(1, lockRecord.UnlockedBy);
        Assert.NotNull(lockRecord.UnlockedDate);
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
        Assert.Null(dto.PartitionNo);
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
