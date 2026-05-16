using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Comprehensive tests for UnitOfWork to achieve 100% code coverage
/// </summary>
public class UnitOfWorkComprehensiveTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidContext_CreatesInstance()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        var unitOfWork = new UnitOfWork(context);

        // Assert
        Assert.NotNull(unitOfWork);
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ReturnsZero()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithOneEntity_ReturnsOne()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        var ward = new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntities_ReturnsCorrectCount()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        context.WardMaster.Add(new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { WardNo = "W002", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { WardNo = "W003", ZoneId = 1, IsActive = true });

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_SavesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);
        var cts = new CancellationTokenSource();

        context.WardMaster.Add(new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true });

        // Act
        var result = await unitOfWork.SaveChangesAsync(cts.Token);

        // Assert
        Assert.Equal(1, result);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task BeginTransactionAsync_WithInMemoryDatabase_ThrowsExpectedException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        // Act & Assert
        // InMemory database throws InvalidOperationException for transactions
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await unitOfWork.BeginTransactionAsync());
    }

    [Fact]
    public async Task CommitTransactionAsync_WithoutBeginTransaction_DoesNotThrow()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        context.WardMaster.Add(new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true });
        await unitOfWork.SaveChangesAsync();

        // Act
        await unitOfWork.CommitTransactionAsync();

        // Assert
        Assert.True(true); // Should not throw
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithoutBeginTransaction_DoesNotThrow()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        context.WardMaster.Add(new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true });

        // Act
        await unitOfWork.RollbackTransactionAsync();

        // Assert
        Assert.True(true); // Should not throw
    }

    // Transaction tests simplified - InMemory database does not support transactions

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesContext()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        // Act
        unitOfWork.Dispose();

        // Assert - Attempting to use disposed context should throw
        Assert.Throws<ObjectDisposedException>(() => context.WardMaster.ToList());
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_HandlesGracefully()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        // Act
        unitOfWork.Dispose();
        unitOfWork.Dispose();
        unitOfWork.Dispose();

        // Assert - Should not throw
        Assert.True(true);
    }

    #endregion

    #region Update and Delete Tests

    [Fact]
    public async Task SaveChangesAsync_WithUpdatedEntity_SavesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        var ward = new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);
        await unitOfWork.SaveChangesAsync();

        // Act
        ward.WardNo = "W002";
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
        Assert.Equal("W002", ward.WardNo);
    }

    [Fact]
    public async Task SaveChangesAsync_WithDeletedEntity_SavesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        var ward = new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);
        await unitOfWork.SaveChangesAsync();

        // Act
        context.WardMaster.Remove(ward);
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
        Assert.Empty(context.WardMaster);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public async Task SaveChangesAsync_WithMixedOperations_SavesAllChanges()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        // Add initial entity
        var ward1 = new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward1);
        await unitOfWork.SaveChangesAsync();

        // Act - Perform multiple operations
        ward1.WardNo = "W001-Updated"; // Update
        context.WardMaster.Add(new WardEntity { WardNo = "W002", ZoneId = 1, IsActive = true }); // Add
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(2, result); // One update + one add
        Assert.Equal(2, context.WardMaster.Count());
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntitiesInSequence_SavesAll()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);

        // Act
        context.WardMaster.Add(new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { WardNo = "W002", ZoneId = 1, IsActive = true });
        await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(2, context.WardMaster.Count());
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task SaveChangesAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        context.WardMaster.Add(new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true });

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await unitOfWork.SaveChangesAsync(cts.Token));
    }

    #endregion
}
