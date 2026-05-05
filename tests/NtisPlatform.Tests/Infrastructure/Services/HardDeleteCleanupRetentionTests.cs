using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Tests for HardDeleteCleanupService to verify immediate hard-delete behavior.
/// Ensures <c>ForceHardDeleteAsync</c> works correctly and returns appropriate responses.
/// </summary>
public class HardDeleteCleanupForceHardDeleteTests
{
    [Fact]
    public async Task ForceHardDeleteAsync_ExistingEntity_DeletesAndReturnsTrue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var mockLogger = new Mock<ILogger<HardDeleteCleanupService>>();
        var service = new HardDeleteCleanupService(context, Mock.Of<ILocalizationService>(), mockLogger.Object);

        var property = new PropertyEntity
        {
            Id = 1,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };
        context.Set<PropertyEntity>().Add(property);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ForceHardDeleteAsync<PropertyEntity, int>(1, CancellationToken.None);

        // Assert
        Assert.True(result);
        var deleted = await context.Set<PropertyEntity>().FindAsync(1);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task ForceHardDeleteAsync_NonExistentEntity_ReturnsFalse()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var mockLogger = new Mock<ILogger<HardDeleteCleanupService>>();
        var service = new HardDeleteCleanupService(context, Mock.Of<ILocalizationService>(), mockLogger.Object);

        // Act - Try to delete non-existent entity
        var result = await service.ForceHardDeleteAsync<PropertyEntity, int>(999, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ForceHardDeleteAsync_MultipleEntities_DeletesOnlySpecified()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var mockLogger = new Mock<ILogger<HardDeleteCleanupService>>();
        var service = new HardDeleteCleanupService(context, Mock.Of<ILocalizationService>(), mockLogger.Object);

        var entity1 = new PropertyEntity { Id = 1, IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now };
        var entity2 = new PropertyEntity { Id = 2, IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now };
        var entity3 = new PropertyEntity { Id = 3, IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now };

        context.Set<PropertyEntity>().AddRange(entity1, entity2, entity3);
        await context.SaveChangesAsync();

        // Act - Delete only entity 2
        var result = await service.ForceHardDeleteAsync<PropertyEntity, int>(2, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.NotNull(await context.Set<PropertyEntity>().FindAsync(1)); // Still exists
        Assert.Null(await context.Set<PropertyEntity>().FindAsync(2)); // Deleted
        Assert.NotNull(await context.Set<PropertyEntity>().FindAsync(3)); // Still exists
    }

    [Fact]
    public async Task ForceHardDeleteAsync_LogsInformation_OnSuccess()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var mockLogger = new Mock<ILogger<HardDeleteCleanupService>>();
        var service = new HardDeleteCleanupService(context, Mock.Of<ILocalizationService>(), mockLogger.Object);

        var entity = new PropertyEntity { Id = 5, IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now };
        context.Set<PropertyEntity>().Add(entity);
        await context.SaveChangesAsync();

        // Act
        await service.ForceHardDeleteAsync<PropertyEntity, int>(5, CancellationToken.None);

        // Assert - Verify information log was called
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Force hard delete completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ForceHardDeleteAsync_LogsWarning_OnNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var mockLogger = new Mock<ILogger<HardDeleteCleanupService>>();
        var service = new HardDeleteCleanupService(context, Mock.Of<ILocalizationService>(), mockLogger.Object);

        // Act - Try to delete non-existent entity
        await service.ForceHardDeleteAsync<PropertyEntity, int>(999, CancellationToken.None);

        // Assert - Verify warning log was called
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}