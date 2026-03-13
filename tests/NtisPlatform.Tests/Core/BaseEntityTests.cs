using NtisPlatform.Core.Entities;

namespace NtisPlatform.Tests.Core;

/// <summary>
/// Unit tests for BaseEntity
/// </summary>
public class BaseEntityTests
{
    private class TestEntity : BaseEntity
    {
        public int Id { get; set; }
        public string TestProperty { get; set; } = string.Empty;
    }

    [Fact]
    public void BaseEntity_HasDefaultValues()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.Equal(0, entity.Id);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedDate);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedBy);
        Assert.True(entity.IsActive); // Default is now true (entities are active by default)
    }

    [Fact]
    public void BaseEntity_CanSetProperties()
    {
        // Arrange
        var entity = new TestEntity();
        var now = DateTime.Now;

        // Act
        entity.Id = 1;
        entity.CreatedDate = now;
        entity.UpdatedDate = now;
        entity.CreatedBy = 1;
        entity.UpdatedBy = 1;
        entity.IsActive = true;

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(now, entity.UpdatedDate);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(1, entity.UpdatedBy);
        Assert.True(entity.IsActive);
    }
}
