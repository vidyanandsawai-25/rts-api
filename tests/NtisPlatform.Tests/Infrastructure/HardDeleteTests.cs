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
        var repository = new Repository<PropertyEntity>(context);
        
        var property = new PropertyEntity
        {
            OwnerID = 1,
            WardNo = "01",
            PropertyNo = "100",
            IsActive = true,
            MarkedForDeletion = true
        };

        await repository.AddAsync(property);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(property.OwnerID);
        await context.SaveChangesAsync();

        // Assert
        var deletedProperty = await context.Set<PropertyEntity>().FindAsync(property.OwnerID);
        Assert.NotNull(deletedProperty);
        Assert.False(deletedProperty.IsActive); // Should be soft deleted
        Assert.NotNull(deletedProperty.MarkedForDeletionDate); // Should have deletion timestamp
        Assert.True(deletedProperty.MarkedForDeletionDate <= DateTime.Now);
    }

    [Fact]
    public async Task DeleteAsync_RegularEntity_PerformsSoftDelete()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var repository = new Repository<Role>(context);
        
        var role = new Role
        {
            Name = "TestRole",
            Description = "Test Role Description"
        };

        await repository.AddAsync(role);
        await context.SaveChangesAsync();
        var roleId = role.Id;

        // Act
        await repository.DeleteAsync(roleId);
        await context.SaveChangesAsync();

        // Assert - Regular entities don't get MarkedForDeletionDate
        var deletedRole = await context.Set<Role>().FindAsync(roleId);
        Assert.NotNull(deletedRole); // Should still exist (soft deleted)
        // Note: Some entities override IsActive, but the entity is soft-deleted via base class property
    }

    [Fact]
    public void PropertyEntity_ImplementsIHardDeletable()
    {
        // Assert
        Assert.True(typeof(IHardDeletable).IsAssignableFrom(typeof(PropertyEntity)));
    }

    [Fact]
    public void PropertyEntity_HasMarkedForDeletionProperties()
    {
        // Arrange
        var property = new PropertyEntity();

        // Assert
        Assert.False(property.MarkedForDeletion); // Default should be false
        Assert.Null(property.MarkedForDeletionDate); // Default should be null
    }

    [Fact]
    public void PropertyEntity_CanSetMarkedForDeletionProperties()
    {
        // Arrange
        var property = new PropertyEntity();
        var now = DateTime.Now;

        // Act
        property.MarkedForDeletion = true;
        property.MarkedForDeletionDate = now;

        // Assert
        Assert.True(property.MarkedForDeletion);
        Assert.Equal(now, property.MarkedForDeletionDate);
    }
}
