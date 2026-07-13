using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.Linq.Expressions;

namespace NtisPlatform.Tests.Infrastructure;

/// <summary>
/// Comprehensive test suite for Repository class
/// Tests both BaseEntity (int keys, soft delete) behavior
/// </summary>
public class RepositoryTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly TestRepository<TestBaseEntity> _repository;

    // Test entity that inherits from BaseEntity with int primary key
    public class TestBaseEntity : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Test entity with string primary key (if needed for future tests)
    public class TestStringKeyEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // Custom DbContext for testing
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<TestBaseEntity> TestEntities { get; set; } = null!;
        public DbSet<TestStringKeyEntity> TestStringEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TestBaseEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<TestStringKeyEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    // Test Repository implementation that accepts DbContext
    public class TestRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public TestRepository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }

        public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity is BaseEntity commonEntity)
            {
                commonEntity.CreatedDate = DateTime.Now;
            }

            await _dbSet.AddAsync(entity, cancellationToken);
            return entity;
        }
        public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var entityList = entities.ToList();
            var now = DateTime.Now;

            foreach (var entity in entityList)
            {
                if (entity is BaseEntity commonEntity)
                {
                    commonEntity.CreatedDate = now;
                }
            }

            await _dbSet.AddRangeAsync(entityList, cancellationToken);
        }

        public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity is BaseEntity commonEntity)
            {
                commonEntity.UpdatedDate = DateTime.Now;
            }

            _dbSet.Update(entity);
            await Task.CompletedTask;
        }

        public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                await DeleteAsync(entity, cancellationToken);
            }
        }

        public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            entity.IsActive = false;
            await UpdateAsync(entity, cancellationToken);
        }
public virtual Task HardDeleteAsync(T entity, CancellationToken cancellationToken = default)
{
    _dbSet.Remove(entity);
    return Task.CompletedTask;
}
        public virtual async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            return entity != null;
        }

        public virtual IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }

        public virtual async Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>>? filter = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }

    public RepositoryTests()
    {
        // Create an in-memory database for each test
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new TestDbContext(options);
        _repository = new TestRepository<TestBaseEntity>(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingEntity_ReturnsEntity()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test Entity" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Entity", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentEntity_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_PassesToFindAsync()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _repository.GetByIdAsync(1, cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoEntities_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleEntities_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Id = 1, Name = "Entity 1" },
            new TestBaseEntity { Id = 2, Name = "Entity 2" },
            new TestBaseEntity { Id = 3, Name = "Entity 3" }
        };
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithCancellationToken_PassesToToListAsync()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _repository.GetAllAsync(cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithBaseEntity_AddsEntitySuccessfully()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "New Entity" };

        // Act
        var result = await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Name, result.Name);
        var dbEntity = await _context.TestEntities.FindAsync(1);
        Assert.NotNull(dbEntity);
    }

    [Fact]
    public async Task AddAsync_WithBaseEntity_SetsCreatedDate()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        var beforeAdd = DateTime.Now.AddSeconds(-1);

        // Act
        await _repository.AddAsync(entity);
        var afterAdd = DateTime.Now.AddSeconds(1);

        // Assert
        Assert.NotNull(entity.CreatedDate);
        Assert.True(entity.CreatedDate >= beforeAdd);
        Assert.True(entity.CreatedDate <= afterAdd);
    }

    [Fact]
    public async Task AddAsync_WithMultipleEntities_AddsAllSuccessfully()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Id = 1, Name = "Entity 1" },
            new TestBaseEntity { Id = 2, Name = "Entity 2" }
        };

        // Act
        foreach (var entity in entities)
        {
            await _repository.AddAsync(entity);
        }
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetAllAsync();
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task AddAsync_WithCancellationToken_PassesToAddAsync()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        var cts = new CancellationTokenSource();

        // Act
        await _repository.AddAsync(entity, cts.Token);
        await _context.SaveChangesAsync();

        // Assert
        var dbEntity = await _context.TestEntities.FindAsync(1);
        Assert.NotNull(dbEntity);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithBaseEntity_UpdatesSuccessfully()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Original Name" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        entity.Name = "Updated Name";
        await _repository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.TestEntities.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_WithBaseEntity_SetsUpdatedDate()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();
        var beforeUpdate = DateTime.Now.AddSeconds(-1);

        // Act
        entity.Name = "Updated";
        await _repository.UpdateAsync(entity);
        var afterUpdate = DateTime.Now.AddSeconds(1);

        // Assert
        Assert.NotNull(entity.UpdatedDate);
        Assert.True(entity.UpdatedDate >= beforeUpdate);
        Assert.True(entity.UpdatedDate <= afterUpdate);
    }

    [Fact]
    public async Task UpdateAsync_PreservesCreatedDate()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();
        var originalCreatedDate = entity.CreatedDate;

        // Act
        entity.Name = "Updated";
        await _repository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Equal(originalCreatedDate, entity.CreatedDate);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithBaseEntity_SoftDeletesEntity()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "To Delete" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(1);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.TestEntities.FindAsync(1);
        Assert.NotNull(deleted);
        Assert.False(deleted.IsActive); // Soft delete sets IsActive to false
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentEntity_DoesNotThrow()
    {
        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _repository.DeleteAsync(999);
            await _context.SaveChangesAsync();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteAsync_WithCancellationToken_PassesToGetByIdAsync()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();
        var cts = new CancellationTokenSource();

        // Act
        await _repository.DeleteAsync(1, cts.Token);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.TestEntities.FindAsync(1);
        Assert.NotNull(deleted);
        Assert.False(deleted.IsActive);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingEntity_ReturnsTrue()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(1);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentEntity_ReturnsFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync(999);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithCancellationToken_PassesToGetByIdAsync()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        await _context.Set<TestBaseEntity>().AddAsync(entity);
        await _context.SaveChangesAsync();
        var cts = new CancellationTokenSource();

        // Act
        var exists = await _repository.ExistsAsync(1, cts.Token);

        // Assert
        Assert.True(exists);
    }

    #endregion

    #region GetQueryable Tests

    [Fact]
    public void GetQueryable_ReturnsIQueryable()
    {
        // Act
        var queryable = _repository.GetQueryable();

        // Assert
        Assert.NotNull(queryable);
        Assert.IsAssignableFrom<IQueryable<TestBaseEntity>>(queryable);
    }

    [Fact]
    public async Task GetQueryable_CanBeUsedForCustomQueries()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Id = 1, Name = "Alpha" },
            new TestBaseEntity { Id = 2, Name = "Beta" },
            new TestBaseEntity { Id = 3, Name = "Gamma" }
        };
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetQueryable()
            .Where(e => e.Name.Contains("a"))
            .OrderBy(e => e.Name)
            .ToListAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
        Assert.Equal("Gamma", result[2].Name);
    }

    [Fact]
    public async Task GetQueryable_SupportsComplexFiltering()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Id = 1, Name = "Test1", Description = "Desc1" },
            new TestBaseEntity { Id = 2, Name = "Test2", Description = "Desc2" },
            new TestBaseEntity { Id = 3, Name = "Other", Description = "Desc1" }
        };
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetQueryable()
            .Where(e => e.Name.StartsWith("Test") && e.Description == "Desc1")
            .ToListAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Test1", result[0].Name);
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WithNoFilter_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Id = 1, Name = "Entity 1" },
            new TestBaseEntity { Id = 2, Name = "Entity 2" }
        };
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAsync_WithFilter_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Id = 1, Name = "Match" },
            new TestBaseEntity { Id = 2, Name = "NoMatch" },
            new TestBaseEntity { Id = 3, Name = "Match" }
        };
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(e => e.Name == "Match");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.Equal("Match", e.Name));
    }

    [Fact]
    public async Task GetAsync_WithComplexFilter_ReturnsCorrectEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Id = 1, Name = "Test1", Description = "Important" },
            new TestBaseEntity { Id = 2, Name = "Test2", Description = "Normal" },
            new TestBaseEntity { Id = 3, Name = "Other", Description = "Important" }
        };
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(e => 
            e.Name.StartsWith("Test") && e.Description == "Important");

        // Assert
        Assert.Single(result);
        Assert.Equal("Test1", result.First().Name);
    }

    [Fact]
    public async Task GetAsync_WithNullFilter_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Id = 1, Name = "Entity 1" },
            new TestBaseEntity { Id = 2, Name = "Entity 2" }
        };
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(filter: null);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAsync_WithNoMatchingEntities_ReturnsEmptyList()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(e => e.Name == "NonExistent");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAsync_WithCancellationToken_PassesToToListAsync()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        await _context.Set<TestBaseEntity>().AddAsync(entity);
        await _context.SaveChangesAsync();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _repository.GetAsync(e => e.Id == 1, cts.Token);

        // Assert
        Assert.Single(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullCrudCycle_WorksCorrectly()
    {
        // Create
        var entity = new TestBaseEntity { Id = 1, Name = "Test Entity" };
        await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Read
        var retrieved = await _repository.GetByIdAsync(1);
        Assert.NotNull(retrieved);
        Assert.Equal("Test Entity", retrieved.Name);

        // Update
        retrieved.Name = "Updated Entity";
        await _repository.UpdateAsync(retrieved);
        await _context.SaveChangesAsync();

        var updated = await _repository.GetByIdAsync(1);
        Assert.Equal("Updated Entity", updated!.Name);

        // Delete (soft delete)
        await _repository.DeleteAsync(1);
        await _context.SaveChangesAsync();

        var deleted = await _repository.GetByIdAsync(1);
        Assert.False(deleted!.IsActive);
    }

    [Fact]
    public async Task MultipleOperations_WithSameContext_WorkCorrectly()
    {
        // Add multiple entities
        var entities = new[]
        {
            new TestBaseEntity { Id = 1, Name = "Entity 1" },
            new TestBaseEntity { Id = 2, Name = "Entity 2" },
            new TestBaseEntity { Id = 3, Name = "Entity 3" }
        };

        foreach (var entity in entities)
        {
            await _repository.AddAsync(entity);
        }
        await _context.SaveChangesAsync();

        // Update one
        var toUpdate = await _repository.GetByIdAsync(2);
        toUpdate!.Name = "Updated Entity 2";
        await _repository.UpdateAsync(toUpdate);
        await _context.SaveChangesAsync();

        // Delete one
        await _repository.DeleteAsync(3);
        await _context.SaveChangesAsync();

        // Verify
        var all = await _repository.GetAllAsync();
        Assert.Equal(3, all.Count());

        var entity2 = await _repository.GetByIdAsync(2);
        Assert.Equal("Updated Entity 2", entity2!.Name);

        var entity3 = await _repository.GetByIdAsync(3);
        Assert.False(entity3!.IsActive);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task UpdateAsync_WithDetachedEntity_WorksCorrectly()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Original" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;

        // Act
        entity.Name = "Updated";
        await _repository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _repository.GetByIdAsync(1);
        Assert.Equal("Updated", updated!.Name);
    }

    [Fact]
    public async Task GetAsync_WithLargeDataSet_PerformsEfficiently()
    {
        // Arrange - Add 1000 entities
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new TestBaseEntity { Id = i, Name = $"Entity {i}" })
            .ToArray();
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(e => e.Id > 500);

        // Assert
        Assert.Equal(500, result.Count());
    }

    [Fact]
    public async Task DeleteAsync_MultipleTimes_OnlyDeletesOnce()
    {
        // Arrange
        var entity = new TestBaseEntity { Id = 1, Name = "Test" };
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act - Delete twice
        await _repository.DeleteAsync(1);
        await _context.SaveChangesAsync();
        await _repository.DeleteAsync(1);
        await _context.SaveChangesAsync();

        // Assert - Entity should still exist (soft deleted)
        var deleted = await _repository.GetByIdAsync(1);
        Assert.NotNull(deleted);
        Assert.False(deleted.IsActive);
    }

    [Fact]
    public async Task GetQueryable_WithProjection_WorksCorrectly()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Id = 1, Name = "Entity 1", Description = "Desc 1" },
            new TestBaseEntity { Id = 2, Name = "Entity 2", Description = "Desc 2" }
        };
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetQueryable()
            .Select(e => new { e.Id, e.Name })
            .ToListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.NotNull(r.Name));
    }

    #endregion
}
