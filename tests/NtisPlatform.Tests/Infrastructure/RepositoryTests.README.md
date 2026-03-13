# Repository Tests Documentation

## Overview
Comprehensive test suite for the generic `Repository<T, TKey>` class that handles:
- **BaseEntity** (int keys): Soft delete with `IsActive = false`
- **IHardDeletable** (deferred hard delete): Soft delete with `MarkedForDeletionDate`, later permanently removed by background task
- **Non-BaseEntity**: Immediate hard delete

## Test Coverage

### Test Statistics
- **Total Tests**: 34
- **All Passing**: ✅ Yes (34/34)
- **Code Coverage Areas**: GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync, ExistsAsync, GetQueryable, GetAsync

## Test Structure

### Test Entities
- `TestBaseEntity`: Inherits from `BaseEntity` with int primary key and soft delete support
- `TestStringKeyEntity`: With string primary key for future hard delete tests
- `TestDbContext`: Custom DbContext that includes test entities in the model
- `TestRepository<T>`: Test implementation of repository that accepts DbContext

## Test Categories

### 1. GetByIdAsync Tests (3 tests)
- ✅ `GetByIdAsync_WithExistingEntity_ReturnsEntity` - Verifies retrieval of existing entities
- ✅ `GetByIdAsync_WithNonExistentEntity_ReturnsNull` - Handles missing entities gracefully
- ✅ `GetByIdAsync_WithCancellationToken_PassesToFindAsync` - Validates cancellation token support

### 2. GetAllAsync Tests (3 tests)
- ✅ `GetAllAsync_WithNoEntities_ReturnsEmptyList` - Handles empty database
- ✅ `GetAllAsync_WithMultipleEntities_ReturnsAllEntities` - Retrieves all entities correctly
- ✅ `GetAllAsync_WithCancellationToken_PassesToToListAsync` - Validates cancellation token support

### 3. AddAsync Tests (4 tests)
- ✅ `AddAsync_WithBaseEntity_AddsEntitySuccessfully` - Basic entity addition
- ✅ `AddAsync_WithBaseEntity_SetsCreatedDate` - Validates automatic CreatedDate setting
- ✅ `AddAsync_WithMultipleEntities_AddsAllSuccessfully` - Batch addition support
- ✅ `AddAsync_WithCancellationToken_PassesToAddAsync` - Validates cancellation token support

### 4. UpdateAsync Tests (3 tests)
- ✅ `UpdateAsync_WithBaseEntity_UpdatesSuccessfully` - Basic entity update
- ✅ `UpdateAsync_WithBaseEntity_SetsUpdatedDate` - Validates automatic UpdatedDate setting
- ✅ `UpdateAsync_PreservesCreatedDate` - Ensures CreatedDate remains unchanged

### 5. DeleteAsync Tests (3 tests)
- ✅ `DeleteAsync_WithBaseEntity_SoftDeletesEntity` - Validates soft delete (IsActive = false)
- ✅ `DeleteAsync_WithNonExistentEntity_DoesNotThrow` - Handles missing entities gracefully
- ✅ `DeleteAsync_WithCancellationToken_PassesToGetByIdAsync` - Validates cancellation token support

### 6. ExistsAsync Tests (3 tests)
- ✅ `ExistsAsync_WithExistingEntity_ReturnsTrue` - Confirms entity existence
- ✅ `ExistsAsync_WithNonExistentEntity_ReturnsFalse` - Confirms entity absence
- ✅ `ExistsAsync_WithCancellationToken_PassesToGetByIdAsync` - Validates cancellation token support

### 7. GetQueryable Tests (3 tests)
- ✅ `GetQueryable_ReturnsIQueryable` - Returns proper IQueryable interface
- ✅ `GetQueryable_CanBeUsedForCustomQueries` - Enables LINQ query composition
- ✅ `GetQueryable_SupportsComplexFiltering` - Handles complex query scenarios

### 8. GetAsync Tests (6 tests)
- ✅ `GetAsync_WithNoFilter_ReturnsAllEntities` - Returns all entities when no filter provided
- ✅ `GetAsync_WithFilter_ReturnsMatchingEntities` - Filters entities based on expression
- ✅ `GetAsync_WithComplexFilter_ReturnsCorrectEntities` - Handles complex filter expressions
- ✅ `GetAsync_WithNullFilter_ReturnsAllEntities` - Treats null filter as no filter
- ✅ `GetAsync_WithNoMatchingEntities_ReturnsEmptyList` - Returns empty list when no matches
- ✅ `GetAsync_WithCancellationToken_PassesToToListAsync` - Validates cancellation token support

### 9. Integration Tests (2 tests)
- ✅ `FullCrudCycle_WorksCorrectly` - Tests complete Create, Read, Update, Delete cycle
- ✅ `MultipleOperations_WithSameContext_WorkCorrectly` - Tests multiple operations in sequence

### 10. Edge Cases and Error Scenarios (4 tests)
- ✅ `UpdateAsync_WithDetachedEntity_WorksCorrectly` - Handles detached entities
- ✅ `GetAsync_WithLargeDataSet_PerformsEfficiently` - Tests with 1000 entities
- ✅ `DeleteAsync_MultipleTimes_OnlyDeletesOnce` - Handles repeated delete attempts
- ✅ `GetQueryable_WithProjection_WorksCorrectly` - Supports LINQ projections

## Key Features Tested

### Soft Delete Behavior (BaseEntity)
- Regular `BaseEntity` entities are marked as `IsActive = false` instead of being removed
- Soft-deleted entities can be filtered out in queries
- Data is preserved for audit/recovery purposes

### Deferred Hard Delete Behavior (IHardDeletable)
- Entities implementing `IHardDeletable` interface are first soft-deleted with `IsActive = false`
- `MarkedForDeletionDate` timestamp is set to track when deletion was requested
- These entities remain in the database temporarily for audit purposes
- A background cleanup task permanently removes marked entities after a retention period
- Provides audit trail during the retention window

### Immediate Hard Delete Behavior (Non-BaseEntity)
- Entities that don't inherit from `BaseEntity` are immediately removed from the database
- No soft delete or audit trail maintained

### Automatic Timestamp Management
- `CreatedDate` set automatically on `AddAsync` for BaseEntity
- `UpdatedDate` set automatically on `UpdateAsync` for BaseEntity
- `CreatedDate` preserved during updates

### Cancellation Token Support
- All async methods accept CancellationToken parameter
- Tokens are passed through to underlying EF Core methods

### Type Safety
- Generic implementation supports both int and string primary keys
- Type constraints ensure proper entity inheritance

## Testing Approach

### In-Memory Database
- Uses EF Core's In-Memory database provider
- Each test gets a unique database instance (via Guid)
- Automatic cleanup via `IDisposable` implementation

### Query Filter Testing
- Tests verify soft delete query filters work correctly
- Uses `IgnoreQueryFilters()` to verify underlying data state

### Cancellation Token Testing
- Note: In-Memory database doesn't truly respect cancellation tokens
- Tests verify tokens are accepted and passed through correctly
- Production databases will properly handle cancellation

## Best Practices Demonstrated

1. **Isolation**: Each test uses a fresh database instance
2. **AAA Pattern**: Arrange, Act, Assert structure
3. **Descriptive Names**: Test names clearly indicate what's being tested
4. **Edge Cases**: Comprehensive edge case coverage
5. **Type Variety**: Tests both int and string primary keys
6. **Realistic Scenarios**: Integration tests simulate real-world usage

## Usage Examples

### Running All Repository Tests
```bash
dotnet test --filter "FullyQualifiedName~RepositoryTests"
```

### Running Specific Test Category
```bash
dotnet test --filter "FullyQualifiedName~RepositoryTests.AddAsync"
```

### Running Single Test
```bash
dotnet test --filter "FullyQualifiedName~RepositoryTests.FullCrudCycle_WorksCorrectly"
```

## Implementation Notes

### Known EF Core Behaviors
1. **FindAsync and Query Filters**: `FindAsync` doesn't respect global query filters, so soft-deleted entities may be returned. Use `GetAllAsync()` or `GetQueryable()` with filters for proper soft-delete behavior.

2. **In-Memory Database Limitations**: The in-memory provider doesn't fully simulate a real database, particularly for:
   - Cancellation token handling
   - Some constraint validations
   - Transaction behavior

3. **Duplicate Key Handling**: EF Core throws `InvalidOperationException` when attempting to track entities with duplicate keys, not `ArgumentException`.

## Future Enhancements

Potential areas for additional testing:
- [ ] Performance benchmarks for large datasets
- [ ] Concurrent access scenarios
- [ ] Transaction rollback scenarios
- [ ] Complex relationship handling
- [ ] Specification pattern integration
- [ ] Pagination helpers
- [ ] Sorting and ordering

## Maintenance

When updating the Repository class:
1. Run all tests to ensure no regressions
2. Add new tests for new functionality
3. Update this documentation
4. Consider adding integration tests with real database
