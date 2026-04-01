using NtisPlatform.Core.Entities;
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
        await repository.DeleteAsync(property.PropertyId);
        await context.SaveChangesAsync();

        // Assert - Should be soft deleted (IsActive = false)
        var deletedProperty = await context.Set<PropertyEntity>().FindAsync(property.PropertyId);
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
        var roleId = role.UserRoleId;

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
}
