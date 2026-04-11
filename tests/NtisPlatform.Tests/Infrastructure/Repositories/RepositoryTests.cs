using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Comprehensive tests for Repository<T, TKey> and Repository<T> to achieve 100% code coverage
/// </summary>
public class RepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_EntityExists_ReturnsEntity()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity, int>(context);
        var result = await repository.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("W001", result.WardNo);
    }

    [Fact]
    public async Task GetByIdAsync_EntityNotExists_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var repository = new Repository<WardEntity, int>(context);
        var result = await repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.AddRange(
            new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true },
            new WardEntity { Id = 2, WardNo = "W002", ZoneId = 1, IsActive = true }
        );
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity, int>(context);
        var result = await repository.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task AddAsync_BaseEntity_SetsCreatedDate()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true };
        var repository = new Repository<WardEntity, int>(context);

        var result = await repository.AddAsync(ward);

        Assert.NotNull(result.CreatedDate);
    }

    [Fact]
    public async Task UpdateAsync_BaseEntity_SetsUpdatedDate()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);
        await context.SaveChangesAsync();

        ward.WardNo = "W001_Updated";
        var repository = new Repository<WardEntity, int>(context);
        await repository.UpdateAsync(ward);

        Assert.NotNull(ward.UpdatedDate);
    }

    [Fact]
    public async Task DeleteAsync_BaseEntity_SoftDeletes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity, int>(context);
        await repository.DeleteAsync(1);

        var deletedWard = await context.WardMaster.FindAsync(1);
        Assert.NotNull(deletedWard);
        Assert.False(deletedWard!.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_HardDeletableEntity_MarksForDeletion()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 79,
            TaxZoneId = 10,
            IsActive = true,
            MarkedForDeletion = false
        };
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var repository = new Repository<PropertyEntity, int>(context);
        await repository.DeleteAsync(1);

        var deletedProperty = await context.PropertyMast.FindAsync(1);
        Assert.NotNull(deletedProperty);
        Assert.False(deletedProperty!.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_EntityNotFound_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var repository = new Repository<WardEntity, int>(context);
        await repository.DeleteAsync(999);

        // No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public async Task ExistsAsync_EntityExists_ReturnsTrue()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity, int>(context);
        var result = await repository.ExistsAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_EntityNotExists_ReturnsFalse()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var repository = new Repository<WardEntity, int>(context);
        var result = await repository.ExistsAsync(999);

        Assert.False(result);
    }

    [Fact]
    public void GetQueryable_ReturnsQueryable()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var repository = new Repository<WardEntity, int>(context);
        var queryable = repository.GetQueryable();

        Assert.NotNull(queryable);
        Assert.IsAssignableFrom<IQueryable<WardEntity>>(queryable);
    }

    [Fact]
    public async Task GetAsync_WithFilter_ReturnsFilteredEntities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.AddRange(
            new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true },
            new WardEntity { Id = 2, WardNo = "W002", ZoneId = 2, IsActive = true }
        );
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity, int>(context);
        var result = await repository.GetAsync(w => w.ZoneId == 1);

        Assert.Single(result);
        Assert.Equal("W001", result.First().WardNo);
    }

    [Fact]
    public async Task GetAsync_NoFilter_ReturnsAllEntities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.AddRange(
            new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true },
            new WardEntity { Id = 2, WardNo = "W002", ZoneId = 2, IsActive = true }
        );
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity, int>(context);
        var result = await repository.GetAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Repository_IntKey_BaseEntity_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity>(context);
        var result = await repository.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("W001", result.WardNo);
    }

    [Fact]
    public async Task Repository_SingleTypeParameter_InheritsFromGenericRepository()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var repository = new Repository<WardEntity>(context);

        Assert.IsAssignableFrom<Repository<WardEntity, int>>(repository);
        Assert.IsAssignableFrom<IRepository<WardEntity>>(repository);
    }

    [Fact]
    public async Task Repository_SingleTypeParameter_AddAsync_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var repository = new Repository<WardEntity>(context);
        var ward = new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true };

        var result = await repository.AddAsync(ward);
        await context.SaveChangesAsync();

        Assert.NotNull(result);
        Assert.NotNull(result.CreatedDate);
    }

    [Fact]
    public async Task Repository_SingleTypeParameter_UpdateAsync_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity>(context);
        ward.WardNo = "W001_Updated";
        await repository.UpdateAsync(ward);
        await context.SaveChangesAsync();

        var updated = await context.WardMaster.FindAsync(1);
        Assert.Equal("W001_Updated", updated!.WardNo);
    }

    [Fact]
    public async Task Repository_SingleTypeParameter_DeleteAsync_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity>(context);
        await repository.DeleteAsync(1);
        await context.SaveChangesAsync();

        var deleted = await context.WardMaster.FindAsync(1);
        Assert.NotNull(deleted);
        Assert.False(deleted!.IsActive);
    }

    [Fact]
    public async Task Repository_SingleTypeParameter_GetAllAsync_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.AddRange(
            new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true },
            new WardEntity { Id = 2, WardNo = "W002", ZoneId = 1, IsActive = true }
        );
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity>(context);
        var result = await repository.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Repository_SingleTypeParameter_ExistsAsync_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true };
        context.WardMaster.Add(ward);
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity>(context);
        
        Assert.True(await repository.ExistsAsync(1));
        Assert.False(await repository.ExistsAsync(999));
    }

    [Fact]
    public async Task Repository_SingleTypeParameter_GetQueryable_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity>(context);
        var queryable = repository.GetQueryable();

        var result = await queryable.Where(w => w.WardNo == "W001").ToListAsync();
        Assert.Single(result);
    }

    [Fact]
    public async Task Repository_SingleTypeParameter_GetAsync_WithFilter_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.AddRange(
            new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true },
            new WardEntity { Id = 2, WardNo = "W002", ZoneId = 2, IsActive = true }
        );
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity>(context);
        var result = await repository.GetAsync(w => w.ZoneId == 1);

        Assert.Single(result);
        Assert.Equal("W001", result.First().WardNo);
    }

    [Fact]
    public async Task AddAsync_WithCancellationToken_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { WardNo = "W001", ZoneId = 1, IsActive = true };
        var repository = new Repository<WardEntity, int>(context);

        var cts = new CancellationTokenSource();
        var result = await repository.AddAsync(ward, cts.Token);

        Assert.NotNull(result);
        Assert.NotNull(result.CreatedDate);
    }

    [Fact]
    public async Task GetAllAsync_WithCancellationToken_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var repository = new Repository<WardEntity, int>(context);
        var cts = new CancellationTokenSource();
        var result = await repository.GetAllAsync(cts.Token);

        Assert.Single(result);
    }
}
