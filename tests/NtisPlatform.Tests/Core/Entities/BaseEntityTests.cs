using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Comprehensive tests for BaseEntity to achieve 100% code coverage
/// </summary>
public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_CanBeInstantiatedThroughDerivedClass()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.NotNull(entity);
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void BaseEntity_Id_CanBeSetAndGet()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.Id = 123;

        // Assert
        Assert.Equal(123, entity.Id);
    }

    [Fact]
    public void BaseEntity_CreatedDate_CanBeSetAndGet()
    {
        // Arrange
        var entity = new TestEntity();
        var date = DateTime.Now;

        // Act
        entity.CreatedDate = date;

        // Assert
        Assert.Equal(date, entity.CreatedDate);
    }

    [Fact]
    public void BaseEntity_CreatedDate_CanBeNull()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.CreatedDate = null;

        // Assert
        Assert.Null(entity.CreatedDate);
    }

    [Fact]
    public void BaseEntity_UpdatedDate_CanBeSetAndGet()
    {
        // Arrange
        var entity = new TestEntity();
        var date = DateTime.Now;

        // Act
        entity.UpdatedDate = date;

        // Assert
        Assert.Equal(date, entity.UpdatedDate);
    }

    [Fact]
    public void BaseEntity_UpdatedDate_CanBeNull()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.UpdatedDate = null;

        // Assert
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void BaseEntity_CreatedBy_CanBeSetAndGet()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.CreatedBy = 1;

        // Assert
        Assert.Equal(1, entity.CreatedBy);
    }

    [Fact]
    public void BaseEntity_CreatedBy_CanBeNull()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.CreatedBy = null;

        // Assert
        Assert.Null(entity.CreatedBy);
    }

    [Fact]
    public void BaseEntity_UpdatedBy_CanBeSetAndGet()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.UpdatedBy = 2;

        // Assert
        Assert.Equal(2, entity.UpdatedBy);
    }

    [Fact]
    public void BaseEntity_UpdatedBy_CanBeNull()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.UpdatedBy = null;

        // Assert
        Assert.Null(entity.UpdatedBy);
    }

    [Fact]
    public void BaseEntity_IsActive_DefaultsToTrue()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void BaseEntity_IsActive_CanBeSetToFalse()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.IsActive = false;

        // Assert
        Assert.False(entity.IsActive);
    }

    [Fact]
    public void BaseEntity_AllPropertiesCanBeSet()
    {
        // Arrange
        var entity = new TestEntity();
        var createdDate = new DateTime(2024, 1, 1);
        var updatedDate = new DateTime(2024, 1, 2);

        // Act
        entity.Id = 100;
        entity.CreatedDate = createdDate;
        entity.UpdatedDate = updatedDate;
        entity.CreatedBy = 1;
        entity.UpdatedBy = 2;
        entity.IsActive = false;

        // Assert
        Assert.Equal(100, entity.Id);
        Assert.Equal(createdDate, entity.CreatedDate);
        Assert.Equal(updatedDate, entity.UpdatedDate);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.False(entity.IsActive);
    }

    #region Test Helper Class

    private class TestEntity : BaseEntity
    {
    }

    #endregion
}
