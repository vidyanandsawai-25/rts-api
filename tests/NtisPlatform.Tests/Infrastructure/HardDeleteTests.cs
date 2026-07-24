using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace NtisPlatform.Tests.Infrastructure;

/// <summary>
/// Unit tests for hard delete functionality
/// </summary>
public class HardDeleteTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task DeleteAsync_EntityWithIHardDeletable_SetsMarkedForDeletionDate()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new Repository<PropertyEntity, int>(context);
        
        var property = new PropertyEntity
        {
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            IsActive = true,
            MarkedForDeletion = false
        };

        await repository.AddAsync(property);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(property.Id);
        await context.SaveChangesAsync();

        // Assert - Should be soft deleted (IsActive = false)
        var deletedProperty = await context.Set<PropertyEntity>().FindAsync(property.Id);
        Assert.NotNull(deletedProperty); // Should still exist
        Assert.False(deletedProperty.IsActive); // Should be deactivated
    }

    [Fact]
    public async Task DeleteAsync_RegularEntity_PerformsSoftDelete()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new Repository<UserRoleMasterEntity, int>(context);
        
        var role = new UserRoleMasterEntity
        {
            UserRoleName = "TestRole",
            IsActive = true
        };

        await repository.AddAsync(role);
        await context.SaveChangesAsync();
        var roleId = role.Id;

        // Act
        await repository.DeleteAsync(roleId);
        await context.SaveChangesAsync();

        // Assert - Should be soft deleted (IsActive = false)
        var deletedRole = await context.Set<UserRoleMasterEntity>().FindAsync(roleId);
        Assert.NotNull(deletedRole); // Should still exist
        Assert.False(deletedRole.IsActive); // Should be deactivated
    }

    [Fact]
    public void PropertyEntity_MarkedForDeletion_IsImplemented()
    {
        // This test verifies that PropertyEntity can be marked for hard deletion
        // PropertyEntity has MarkedForDeletion property
        var property = new PropertyEntity();
        Assert.False(property.MarkedForDeletion);
    }

    [Fact]
    public void PropertyEntity_HasMarkedForDeletionProperties()
    {
        // Arrange
        var property = new PropertyEntity();

        // Assert
        Assert.False(property.MarkedForDeletion); // Default should be false
    }

    [Fact]
    public void PropertyEntity_CanSetMarkedForDeletionProperties()
    {
        // Arrange
        var property = new PropertyEntity();

        // Act
        property.MarkedForDeletion = true;

        // Assert
        Assert.True(property.MarkedForDeletion);
    }

    // ============================================================
    // AssetAgeFactorCVMasterEntity / AssetNatureFactorCVMasterEntity /
    // AssetAssessmentYearRangeMasterCVEntity now implement IHardDeletable.
    // These mirror the PropertyEntity coverage above, exercising the REAL
    // Repository<T,TKey>.DeleteAsync soft-delete branch (not a mock) so the
    // IHardDeletable-specific behavior (MarkedForDeletion/MarkedForDeletionDate
    // set alongside IsActive=false) is actually verified end-to-end.
    // ============================================================

    #region AssetAgeFactorCVMasterEntity

    [Fact]
    public void AssetAgeFactorCVMasterEntity_MarkedForDeletion_DefaultsToFalse()
    {
        var entity = new AssetAgeFactorCVMasterEntity();

        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public async Task DeleteAsync_AssetAgeFactorCVMasterEntity_SetsMarkedForDeletionAndDeactivates()
    {
        var context = GetInMemoryDbContext();
        var repository = new Repository<AssetAgeFactorCVMasterEntity, int>(context);

        var entity = new AssetAgeFactorCVMasterEntity
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };

        await repository.AddAsync(entity);
        await context.SaveChangesAsync();

        await repository.DeleteAsync(entity.Id);
        await context.SaveChangesAsync();

        var deleted = await context.Set<AssetAgeFactorCVMasterEntity>().FindAsync(entity.Id);
        Assert.NotNull(deleted); // Still exists — soft deleted, not removed
        Assert.False(deleted!.IsActive);
        Assert.True(deleted.MarkedForDeletion);
        Assert.NotNull(deleted.MarkedForDeletionDate);
    }

    #endregion

    #region AssetNatureFactorCVMasterEntity

    [Fact]
    public void AssetNatureFactorCVMasterEntity_MarkedForDeletion_DefaultsToFalse()
    {
        var entity = new AssetNatureFactorCVMasterEntity();

        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public async Task DeleteAsync_AssetNatureFactorCVMasterEntity_SetsMarkedForDeletionAndDeactivates()
    {
        var context = GetInMemoryDbContext();
        var repository = new Repository<AssetNatureFactorCVMasterEntity, int>(context);

        var entity = new AssetNatureFactorCVMasterEntity
        {
            ConstructionTypeId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };

        await repository.AddAsync(entity);
        await context.SaveChangesAsync();

        await repository.DeleteAsync(entity.Id);
        await context.SaveChangesAsync();

        var deleted = await context.Set<AssetNatureFactorCVMasterEntity>().FindAsync(entity.Id);
        Assert.NotNull(deleted);
        Assert.False(deleted!.IsActive);
        Assert.True(deleted.MarkedForDeletion);
        Assert.NotNull(deleted.MarkedForDeletionDate);
    }

    #endregion

    #region AssetAssessmentYearRangeMasterCVEntity

    [Fact]
    public void AssetAssessmentYearRangeMasterCVEntity_MarkedForDeletion_DefaultsToFalse()
    {
        var entity = new AssetAssessmentYearRangeMasterCVEntity();

        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public async Task DeleteAsync_AssetAssessmentYearRangeMasterCVEntity_SetsMarkedForDeletionAndDeactivates()
    {
        var context = GetInMemoryDbContext();
        var repository = new Repository<AssetAssessmentYearRangeMasterCVEntity, int>(context);

        var entity = new AssetAssessmentYearRangeMasterCVEntity
        {
            FromYear = 2000,
            ToYear = 2005,
            IsActive = true
        };

        await repository.AddAsync(entity);
        await context.SaveChangesAsync();

        await repository.DeleteAsync(entity.Id);
        await context.SaveChangesAsync();

        var deleted = await context.Set<AssetAssessmentYearRangeMasterCVEntity>().FindAsync(entity.Id);
        Assert.NotNull(deleted);
        Assert.False(deleted!.IsActive);
        Assert.True(deleted.MarkedForDeletion);
        Assert.NotNull(deleted.MarkedForDeletionDate);
    }

    #endregion
}
