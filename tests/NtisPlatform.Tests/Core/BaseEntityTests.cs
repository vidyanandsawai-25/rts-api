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
        Assert.Equal(default(DateTime), entity.CreatedDate);
        Assert.Null(entity.UpdatedDate);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedBy);
        //Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void BaseEntity_CanSetProperties()
    {
        // Arrange
        var entity = new TestEntity();
        var now = DateTime.UtcNow;

        // Act
        entity.Id = 1;
        entity.CreatedDate = now;
        entity.UpdatedDate = now;
        entity.CreatedBy = "admin";
        entity.UpdatedBy = "admin";
        //entity.IsDeleted = true;

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(now, entity.UpdatedDate);
        Assert.Equal("admin", entity.CreatedBy);
        Assert.Equal("admin", entity.UpdatedBy);
        //Assert.True(entity.IsDeleted);
    }
}
