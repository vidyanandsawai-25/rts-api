using NtisPlatform.Core.Entities;

namespace NtisPlatform.Tests.Core;

/// <summary>
/// Unit tests for BaseEntity
/// </summary>
public class BaseEntityTests
{
    private class TestEntity : BaseEntity
    {
        public string TestProperty { get; set; } = string.Empty;
    }

    [Fact]
    public void BaseEntity_HasDefaultValues()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.Equal(0, entity.Id);
        Assert.Equal(default(DateTime), entity.CreatedAt);
        Assert.Null(entity.UpdatedAt);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedBy);
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void BaseEntity_CanSetProperties()
    {
        // Arrange
        var entity = new TestEntity();
        var now = DateTime.UtcNow;

        // Act
        entity.Id = 1;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        entity.CreatedBy = "admin";
        entity.UpdatedBy = "admin";
        entity.IsDeleted = true;

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal(now, entity.CreatedAt);
        Assert.Equal(now, entity.UpdatedAt);
        Assert.Equal("admin", entity.CreatedBy);
        Assert.Equal("admin", entity.UpdatedBy);
        Assert.True(entity.IsDeleted);
    }
}
