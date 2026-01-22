using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure;

#region Test Entities

public class TestBaseEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class TestCommonBaseEntity : CommonBaseEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

#endregion

#region Test Context

public class TestApplicationDbContext : ApplicationDbContext
{
    public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<TestBaseEntity> TestBaseEntities { get; set; } = null!;
    public DbSet<TestCommonBaseEntity> TestCommonBaseEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestBaseEntity>(entity =>
        {
            entity.ToTable("TestBaseEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<TestCommonBaseEntity>(entity =>
        {
            entity.ToTable("TestCommonBaseEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Value).HasMaxLength(500);
        });
    }
}

#endregion

public class RepositoryTests : IDisposable
{
    private readonly TestApplicationDbContext _context;
    private readonly Repository<TestBaseEntity> _baseRepository;
    private readonly Repository<TestCommonBaseEntity, string> _commonRepository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestApplicationDbContext(options);
        _baseRepository = new Repository<TestBaseEntity>(_context);
        _commonRepository = new Repository<TestCommonBaseEntity, string>(_context);
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
        var entity = new TestBaseEntity
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Test Description",
            CreatedAt = DateTime.Now
        };
        _context.Set<TestBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _baseRepository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Entity", result.Name);
        Assert.Equal("Test Description", result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentEntity_ReturnsNull()
    {
        // Act
        var result = await _baseRepository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithStringKey_ReturnsEntity()
    {
        // Arrange
        var entity = new TestCommonBaseEntity
        {
            Id = "test-id-1",
            Name = "Common Entity",
            Value = "Test Value",
            CreatedDate = DateTime.Now
        };
        _context.Set<TestCommonBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _commonRepository.GetByIdAsync("test-id-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id-1", result.Id);
        Assert.Equal("Common Entity", result.Name);
        Assert.Equal("Test Value", result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_PassesToFindAsync()
    {
        // Arrange
        var entity = new TestBaseEntity
        {
            Name = "Test Entity",
            Description = "Test",
            CreatedAt = DateTime.Now
        };
        _context.Set<TestBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        var cts = new CancellationTokenSource();

        // Act - Simply verify the method accepts and doesn't throw with valid token
        var result = await _baseRepository.GetByIdAsync(entity.Id, cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoEntities_ReturnsEmptyList()
    {
        // Act
        var result = await _baseRepository.GetAllAsync();

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
            new TestBaseEntity { Id = 1, Name = "Entity 1", Description = "Desc 1", CreatedAt = DateTime.Now },
            new TestBaseEntity { Id = 2, Name = "Entity 2", Description = "Desc 2", CreatedAt = DateTime.Now },
            new TestBaseEntity { Id = 3, Name = "Entity 3", Description = "Desc 3", CreatedAt = DateTime.Now }
        };
        _context.Set<TestBaseEntity>().AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _baseRepository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithCancellationToken_PassesToToListAsync()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act - Simply verify the method accepts and doesn't throw with valid token
        var result = await _baseRepository.GetAllAsync(cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithBaseEntity_AddsEntitySuccessfully()
    {
        // Arrange
        var entity = new TestBaseEntity
        {
            Name = "New Entity",
            Description = "New Description"
        };

        // Act
        var result = await _baseRepository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Entity", result.Name);
        Assert.Equal("New Description", result.Description);

        var saved = await _context.Set<TestBaseEntity>().FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("New Entity", saved.Name);
    }

    [Fact]
    public async Task AddAsync_WithCommonBaseEntity_SetsCreatedDate()
    {
        // Arrange
        var beforeAdd = DateTime.Now;
        var entity = new TestCommonBaseEntity
        {
            Id = "test-id",
            Name = "Common Entity",
            Value = "Test Value"
        };

        // Act
        var result = await _commonRepository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CreatedDate);
        Assert.True(result.CreatedDate >= beforeAdd);
        Assert.True(result.CreatedDate <= DateTime.Now.AddSeconds(1));
    }

    [Fact]
    public async Task AddAsync_WithMultipleEntities_AddsAllSuccessfully()
    {
        // Arrange
        var entity1 = new TestBaseEntity { Name = "Entity 1", Description = "Desc 1" };
        var entity2 = new TestBaseEntity { Name = "Entity 2", Description = "Desc 2" };

        // Act
        await _baseRepository.AddAsync(entity1);
        await _baseRepository.AddAsync(entity2);
        await _context.SaveChangesAsync();

        // Assert
        var all = await _baseRepository.GetAllAsync();
        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task AddAsync_WithCancellationToken_PassesToAddAsync()
    {
        // Arrange
        var entity = new TestBaseEntity { Name = "Test", Description = "Test" };
        var cts = new CancellationTokenSource();

        // Act - Simply verify the method accepts and doesn't throw with valid token
        var result = await _baseRepository.AddAsync(entity, cts.Token);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithBaseEntity_UpdatesSuccessfully()
    {
        // Arrange
        var entity = new TestBaseEntity
        {
            Name = "Original Name",
            Description = "Original Description",
            CreatedAt = DateTime.Now
        };
        _context.Set<TestBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        entity.Name = "Updated Name";
        entity.Description = "Updated Description";
        await _baseRepository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _baseRepository.GetByIdAsync(entity.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("Updated Description", updated.Description);
    }

    [Fact]
    public async Task UpdateAsync_WithCommonBaseEntity_SetsUpdatedDate()
    {
        // Arrange
        var entity = new TestCommonBaseEntity
        {
            Id = "test-id",
            Name = "Original Name",
            Value = "Original Value",
            CreatedDate = DateTime.Now.AddDays(-1)
        };
        _context.Set<TestCommonBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        var beforeUpdate = DateTime.Now;

        // Act
        entity.Name = "Updated Name";
        await _commonRepository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _commonRepository.GetByIdAsync("test-id");
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.NotNull(updated.UpdatedDate);
        Assert.True(updated.UpdatedDate >= beforeUpdate);
        Assert.True(updated.UpdatedDate <= DateTime.Now.AddSeconds(1));
    }

    [Fact]
    public async Task UpdateAsync_PreservesCreatedDate()
    {
        // Arrange
        var createdDate = DateTime.Now.AddDays(-5);
        var entity = new TestCommonBaseEntity
        {
            Id = "test-id",
            Name = "Original Name",
            Value = "Original Value",
            CreatedDate = createdDate
        };
        _context.Set<TestCommonBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        entity.Name = "Updated Name";
        await _commonRepository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _commonRepository.GetByIdAsync("test-id");
        Assert.NotNull(updated);
        Assert.Equal(createdDate, updated.CreatedDate);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithBaseEntity_SoftDeletesEntity()
    {
        // Arrange
        var entity = new TestBaseEntity
        {
            Name = "To Delete",
            Description = "Will be soft deleted",
            CreatedAt = DateTime.Now,
            IsDeleted = false
        };
        _context.Set<TestBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();
        var entityId = entity.Id;

        // Act
        await _baseRepository.DeleteAsync(entityId);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Set<TestBaseEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entityId);
        
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
        Assert.Equal("To Delete", deleted.Name); // Data still exists
    }

    [Fact]
    public async Task DeleteAsync_WithCommonBaseEntity_HardDeletesEntity()
    {
        // Arrange
        var entity = new TestCommonBaseEntity
        {
            Id = "to-delete",
            Name = "To Delete",
            Value = "Will be hard deleted",
            CreatedDate = DateTime.Now
        };
        _context.Set<TestCommonBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        await _commonRepository.DeleteAsync("to-delete");
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _commonRepository.GetByIdAsync("to-delete");
        Assert.Null(deleted); // Entity is completely removed
        
        var allEntities = await _context.Set<TestCommonBaseEntity>().ToListAsync();
        Assert.DoesNotContain(allEntities, e => e.Id == "to-delete");
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentEntity_DoesNotThrow()
    {
        // Act
        var exception = await Record.ExceptionAsync(async () =>
            await _baseRepository.DeleteAsync(999));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteAsync_WithCancellationToken_PassesToGetByIdAsync()
    {
        // Arrange
        var entity = new TestBaseEntity { Name = "Test", Description = "Test" };
        _context.Set<TestBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        var cts = new CancellationTokenSource();

        // Act - Simply verify the method accepts and doesn't throw with valid token
        await _baseRepository.DeleteAsync(entity.Id, cts.Token);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Set<TestBaseEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entity.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingEntity_ReturnsTrue()
    {
        // Arrange
        var entity = new TestBaseEntity
        {
            Name = "Existing Entity",
            Description = "Exists",
            CreatedAt = DateTime.Now
        };
        _context.Set<TestBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _baseRepository.ExistsAsync(entity.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentEntity_ReturnsFalse()
    {
        // Act
        var exists = await _baseRepository.ExistsAsync(999);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithStringKey_ReturnsCorrectResult()
    {
        // Arrange
        var entity = new TestCommonBaseEntity
        {
            Id = "exists-id",
            Name = "Existing",
            Value = "Value",
            CreatedDate = DateTime.Now
        };
        _context.Set<TestCommonBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _commonRepository.ExistsAsync("exists-id");
        var notExists = await _commonRepository.ExistsAsync("not-exists-id");

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }

    [Fact]
    public async Task ExistsAsync_WithCancellationToken_PassesToGetByIdAsync()
    {
        // Arrange
        var entity = new TestBaseEntity { Name = "Test", Description = "Test" };
        _context.Set<TestBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        var cts = new CancellationTokenSource();

        // Act - Simply verify the method accepts and doesn't throw with valid token
        var exists = await _baseRepository.ExistsAsync(entity.Id, cts.Token);

        // Assert
        Assert.True(exists);
    }

    #endregion

    #region GetQueryable Tests

    [Fact]
    public void GetQueryable_ReturnsIQueryable()
    {
        // Act
        var queryable = _baseRepository.GetQueryable();

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
            new TestBaseEntity { Name = "Alpha", Description = "First", CreatedAt = DateTime.Now },
            new TestBaseEntity { Name = "Beta", Description = "Second", CreatedAt = DateTime.Now },
            new TestBaseEntity { Name = "Gamma", Description = "Third", CreatedAt = DateTime.Now }
        };
        _context.Set<TestBaseEntity>().AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var query = _baseRepository.GetQueryable()
            .Where(e => e.Name.StartsWith("B"))
            .OrderBy(e => e.Name);
        var results = await query.ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("Beta", results.First().Name);
    }

    [Fact]
    public async Task GetQueryable_SupportsComplexFiltering()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Name = "Active 1", Description = "Active", CreatedAt = DateTime.Now.AddDays(-5) },
            new TestBaseEntity { Name = "Active 2", Description = "Active", CreatedAt = DateTime.Now.AddDays(-3) },
            new TestBaseEntity { Name = "Inactive", Description = "Inactive", CreatedAt = DateTime.Now.AddDays(-1) }
        };
        _context.Set<TestBaseEntity>().AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var query = _baseRepository.GetQueryable()
            .Where(e => e.Description == "Active")
            .OrderByDescending(e => e.CreatedAt);
        var results = await query.ToListAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Active 2", results.First().Name);
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WithNoFilter_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Name = "Entity 1", Description = "Desc 1", CreatedAt = DateTime.Now },
            new TestBaseEntity { Name = "Entity 2", Description = "Desc 2", CreatedAt = DateTime.Now }
        };
        _context.Set<TestBaseEntity>().AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _baseRepository.GetAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAsync_WithFilter_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Name = "Match 1", Description = "Active", CreatedAt = DateTime.Now },
            new TestBaseEntity { Name = "No Match", Description = "Inactive", CreatedAt = DateTime.Now },
            new TestBaseEntity { Name = "Match 2", Description = "Active", CreatedAt = DateTime.Now }
        };
        _context.Set<TestBaseEntity>().AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _baseRepository.GetAsync(e => e.Description == "Active");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.Equal("Active", e.Description));
    }

    [Fact]
    public async Task GetAsync_WithComplexFilter_ReturnsCorrectEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Name = "Alpha", Description = "Test A", CreatedAt = DateTime.Now.AddDays(-5) },
            new TestBaseEntity { Name = "Beta", Description = "Test B", CreatedAt = DateTime.Now.AddDays(-3) },
            new TestBaseEntity { Name = "Alpha Test", Description = "Test C", CreatedAt = DateTime.Now.AddDays(-1) }
        };
        _context.Set<TestBaseEntity>().AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _baseRepository.GetAsync(
            e => e.Name.Contains("Alpha") && e.Description.StartsWith("Test"));

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.Contains("Alpha", e.Name));
    }

    [Fact]
    public async Task GetAsync_WithNullFilter_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Name = "Entity 1", Description = "Desc 1", CreatedAt = DateTime.Now },
            new TestBaseEntity { Name = "Entity 2", Description = "Desc 2", CreatedAt = DateTime.Now }
        };
        _context.Set<TestBaseEntity>().AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _baseRepository.GetAsync(filter: null);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAsync_WithNoMatchingEntities_ReturnsEmptyList()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Name = "Entity 1", Description = "Desc 1", CreatedAt = DateTime.Now }
        };
        _context.Set<TestBaseEntity>().AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _baseRepository.GetAsync(e => e.Name == "NonExistent");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAsync_WithCancellationToken_PassesToToListAsync()
    {
        // Arrange
        var entity = new TestBaseEntity { Name = "Test", Description = "Test", CreatedAt = DateTime.Now };
        _context.Set<TestBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        var cts = new CancellationTokenSource();

        // Act - Simply verify the method accepts and doesn't throw with valid token
        var result = await _baseRepository.GetAsync(e => e.Name == "Test", cts.Token);

        // Assert
        Assert.Single(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullCrudCycle_WorksCorrectly()
    {
        // Create
        var entity = new TestBaseEntity
        {
            Name = "CRUD Test",
            Description = "Full cycle test",
            CreatedAt = DateTime.Now
        };

        var added = await _baseRepository.AddAsync(entity);
        await _context.SaveChangesAsync();
        Assert.NotNull(added);
        Assert.True(added.Id > 0);

        // Read
        var retrieved = await _baseRepository.GetByIdAsync(added.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("CRUD Test", retrieved.Name);

        // Update
        retrieved.Name = "Updated CRUD Test";
        await _baseRepository.UpdateAsync(retrieved);
        await _context.SaveChangesAsync();

        var updated = await _baseRepository.GetByIdAsync(added.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated CRUD Test", updated.Name);

        // Delete (Soft delete for BaseEntity)
        await _baseRepository.DeleteAsync(added.Id);
        await _context.SaveChangesAsync();

        // Verify soft delete - entity still exists in DB but IsDeleted is true
        var softDeleted = await _context.Set<TestBaseEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == added.Id);
        
        Assert.NotNull(softDeleted); // Still exists in database
        Assert.True(softDeleted.IsDeleted); // Marked as deleted
        
        // Note: FindAsync doesn't respect query filters, so we check using GetAllAsync instead
        var allNonDeleted = await _baseRepository.GetAllAsync();
        Assert.DoesNotContain(allNonDeleted, e => e.Id == added.Id);
    }

    [Fact]
    public async Task MultipleOperations_WithSameContext_WorkCorrectly()
    {
        // Add multiple entities
        var entity1 = new TestBaseEntity { Name = "Entity 1", Description = "First", CreatedAt = DateTime.Now };
        var entity2 = new TestBaseEntity { Name = "Entity 2", Description = "Second", CreatedAt = DateTime.Now };
        var entity3 = new TestBaseEntity { Name = "Entity 3", Description = "Third", CreatedAt = DateTime.Now };

        await _baseRepository.AddAsync(entity1);
        await _baseRepository.AddAsync(entity2);
        await _baseRepository.AddAsync(entity3);
        await _context.SaveChangesAsync();

        // Verify all added
        var all = await _baseRepository.GetAllAsync();
        Assert.Equal(3, all.Count());

        // Update one
        entity2.Description = "Second Updated";
        await _baseRepository.UpdateAsync(entity2);
        await _context.SaveChangesAsync();

        // Delete one (soft delete)
        await _baseRepository.DeleteAsync(entity3.Id);
        await _context.SaveChangesAsync();

        // Verify final state - only non-deleted entities
        var remaining = await _baseRepository.GetAllAsync();
        Assert.Equal(2, remaining.Count());

        var updated = await _baseRepository.GetByIdAsync(entity2.Id);
        Assert.NotNull(updated);
        Assert.Equal("Second Updated", updated.Description);

        // Verify entity3 is soft deleted (not in normal queries)
        var allNonDeleted = await _baseRepository.GetAllAsync();
        Assert.DoesNotContain(allNonDeleted, e => e.Id == entity3.Id);
        
        // But still exists in database with IsDeleted = true
        var softDeleted = await _context.Set<TestBaseEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entity3.Id);
        Assert.NotNull(softDeleted);
        Assert.True(softDeleted.IsDeleted);
    }

    [Fact]
    public async Task CommonBaseEntity_FullCycle_HardDeletesCorrectly()
    {
        // Create
        var entity = new TestCommonBaseEntity
        {
            Id = "common-test",
            Name = "Common Entity",
            Value = "Test Value",
            IsActive = true
        };

        var added = await _commonRepository.AddAsync(entity);
        await _context.SaveChangesAsync();
        Assert.NotNull(added.CreatedDate);

        // Update
        added.Value = "Updated Value";
        await _commonRepository.UpdateAsync(added);
        await _context.SaveChangesAsync();

        var updated = await _commonRepository.GetByIdAsync("common-test");
        Assert.NotNull(updated);
        Assert.Equal("Updated Value", updated.Value);
        Assert.NotNull(updated.UpdatedDate);

        // Delete (Hard delete for CommonBaseEntity)
        await _commonRepository.DeleteAsync("common-test");
        await _context.SaveChangesAsync();

        var deleted = await _commonRepository.GetByIdAsync("common-test");
        Assert.Null(deleted); // Completely removed
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsException()
    {
        // Arrange
        var entity1 = new TestCommonBaseEntity
        {
            Id = "duplicate-id",
            Name = "First",
            Value = "Value1"
        };
        var entity2 = new TestCommonBaseEntity
        {
            Id = "duplicate-id",
            Name = "Second",
            Value = "Value2"
        };

        await _commonRepository.AddAsync(entity1);
        await _context.SaveChangesAsync();

        // Act & Assert - EF Core throws InvalidOperationException when tracking duplicate keys
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _commonRepository.AddAsync(entity2);
        });
    }

    [Fact]
    public async Task UpdateAsync_WithDetachedEntity_WorksCorrectly()
    {
        // Arrange
        var entity = new TestBaseEntity
        {
            Name = "Original",
            Description = "Original Desc",
            CreatedAt = DateTime.Now
        };
        _context.Set<TestBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();

        // Detach the entity
        _context.Entry(entity).State = EntityState.Detached;

        // Modify the entity
        entity.Name = "Modified";
        entity.Description = "Modified Desc";

        // Act
        await _baseRepository.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _baseRepository.GetByIdAsync(entity.Id);
        Assert.NotNull(updated);
        Assert.Equal("Modified", updated.Name);
        Assert.Equal("Modified Desc", updated.Description);
    }

    [Fact]
    public async Task GetAsync_WithLargeDataSet_PerformsEfficiently()
    {
        // Arrange
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new TestBaseEntity
            {
                Name = $"Entity {i}",
                Description = i % 2 == 0 ? "Even" : "Odd",
                CreatedAt = DateTime.Now
            });
        _context.Set<TestBaseEntity>().AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _baseRepository.GetAsync(e => e.Description == "Even");

        // Assert
        Assert.Equal(500, result.Count());
    }

    [Fact]
    public async Task DeleteAsync_MultipleTimes_OnlyDeletesOnce()
    {
        // Arrange
        var entity = new TestBaseEntity
        {
            Name = "To Delete",
            Description = "Test",
            CreatedAt = DateTime.Now
        };
        _context.Set<TestBaseEntity>().Add(entity);
        await _context.SaveChangesAsync();
        var entityId = entity.Id;

        // Act
        await _baseRepository.DeleteAsync(entityId);
        await _context.SaveChangesAsync();

        await _baseRepository.DeleteAsync(entityId); // Second delete
        await _context.SaveChangesAsync();

        // Assert - Should not throw, just do nothing
        var deleted = await _context.Set<TestBaseEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entityId);
        
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
    }

    [Fact]
    public async Task Repository_HandlesEmptyStrings_Correctly()
    {
        // Arrange & Act
        var entity = new TestBaseEntity
        {
            Name = "",
            Description = "",
            CreatedAt = DateTime.Now
        };
        await _baseRepository.AddAsync(entity);
        await _context.SaveChangesAsync();

        var result = await _baseRepository.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("", result.Name);
        Assert.Equal("", result.Description);
    }

    [Fact]
    public async Task GetQueryable_WithProjection_WorksCorrectly()
    {
        // Arrange
        var entities = new[]
        {
            new TestBaseEntity { Name = "Entity 1", Description = "Desc 1", CreatedAt = DateTime.Now },
            new TestBaseEntity { Name = "Entity 2", Description = "Desc 2", CreatedAt = DateTime.Now }
        };
        _context.Set<TestBaseEntity>().AddRange(entities);
        await _context.SaveChangesAsync();

        // Act
        var names = await _baseRepository.GetQueryable()
            .Select(e => e.Name)
            .ToListAsync();

        // Assert
        Assert.Equal(2, names.Count);
        Assert.Contains("Entity 1", names);
        Assert.Contains("Entity 2", names);
    }

    #endregion
}
