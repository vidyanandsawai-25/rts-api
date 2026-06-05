using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Comprehensive tests for PropertyPhotoTypeEntity to achieve 100% code coverage
/// </summary>
public class PropertyPhotoTypeEntityTests
{
    [Fact]
    public void PropertyPhotoTypeEntity_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange
        var now = DateTime.Now;
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = "Front facade of the property",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = now,
            UpdatedBy = 200,
            UpdatedDate = now.AddHours(1)
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal("FRONT", entity.PhotoTypeCode);
        Assert.Equal("Front View", entity.PhotoTypeName);
        Assert.Equal("Front facade of the property", entity.Description);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsActive);
        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_InheritsFromBaseEntity()
    {
        // Arrange & Act
        var entity = new PropertyPhotoTypeEntity();

        // Assert
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_DefaultValues_SetCorrectly()
    {
        // Arrange & Act
        var entity = new PropertyPhotoTypeEntity();

        // Assert
        Assert.Equal(0, entity.Id);
        Assert.Equal(string.Empty, entity.PhotoTypeCode);
        Assert.Equal(string.Empty, entity.PhotoTypeName);
        Assert.Null(entity.Description);
        Assert.Null(entity.DisplayOrder);
        Assert.True(entity.IsActive); // BaseEntity default
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_PhotoTypeCode_CanBeEmptyString()
    {
        // Arrange & Act
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = string.Empty,
            PhotoTypeName = "Test Name",
            IsActive = true
        };

        // Assert
        Assert.Equal(string.Empty, entity.PhotoTypeCode);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_PhotoTypeName_CanBeEmptyString()
    {
        // Arrange & Act
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "TEST",
            PhotoTypeName = string.Empty,
            IsActive = true
        };

        // Assert
        Assert.Equal(string.Empty, entity.PhotoTypeName);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_Description_CanBeNull()
    {
        // Arrange & Act
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test Name",
            Description = null,
            IsActive = true
        };

        // Assert
        Assert.Null(entity.Description);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_DisplayOrder_CanBeNull()
    {
        // Arrange & Act
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test Name",
            DisplayOrder = null,
            IsActive = true
        };

        // Assert
        Assert.Null(entity.DisplayOrder);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_IsActive_BothValues_WorkCorrectly()
    {
        // Arrange & Act
        var entity1 = new PropertyPhotoTypeEntity { IsActive = true };
        var entity2 = new PropertyPhotoTypeEntity { IsActive = false };

        // Assert
        Assert.True(entity1.IsActive);
        Assert.False(entity2.IsActive);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_BaseEntityProperties_WorkCorrectly()
    {
        // Arrange
        var now = DateTime.Now;

        // Act
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 100,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now.AddDays(1)
        };

        // Assert
        Assert.Equal(100, entity.Id);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now.AddDays(1), entity.UpdatedDate);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_PhotoTypeCode_MaxLength50_WorksCorrectly()
    {
        // Arrange
        var maxLengthCode = new string('A', 50);

        // Act
        var entity = new PropertyPhotoTypeEntity
        {
            PhotoTypeCode = maxLengthCode
        };

        // Assert
        Assert.Equal(maxLengthCode, entity.PhotoTypeCode);
        Assert.Equal(50, entity.PhotoTypeCode.Length);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_PhotoTypeName_MaxLength200_WorksCorrectly()
    {
        // Arrange
        var maxLengthName = new string('B', 200);

        // Act
        var entity = new PropertyPhotoTypeEntity
        {
            PhotoTypeName = maxLengthName
        };

        // Assert
        Assert.Equal(maxLengthName, entity.PhotoTypeName);
        Assert.Equal(200, entity.PhotoTypeName.Length);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_Description_MaxLength500_WorksCorrectly()
    {
        // Arrange
        var maxLengthDescription = new string('C', 500);

        // Act
        var entity = new PropertyPhotoTypeEntity
        {
            Description = maxLengthDescription
        };

        // Assert
        Assert.Equal(maxLengthDescription, entity.Description);
        Assert.Equal(500, entity.Description!.Length);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_DisplayOrder_PositiveAndNegative_WorksCorrectly()
    {
        // Arrange & Act
        var entity1 = new PropertyPhotoTypeEntity { DisplayOrder = 1 };
        var entity2 = new PropertyPhotoTypeEntity { DisplayOrder = 100 };
        var entity3 = new PropertyPhotoTypeEntity { DisplayOrder = -1 };

        // Assert
        Assert.Equal(1, entity1.DisplayOrder);
        Assert.Equal(100, entity2.DisplayOrder);
        Assert.Equal(-1, entity3.DisplayOrder);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_AllNullableFields_CanBeNull()
    {
        // Arrange & Act
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test",
            Description = null,
            DisplayOrder = null,
            CreatedBy = null,
            CreatedDate = null,
            UpdatedBy = null,
            UpdatedDate = null
        };

        // Assert
        Assert.Null(entity.Description);
        Assert.Null(entity.DisplayOrder);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void PropertyPhotoTypeEntity_MultipleInstances_IndependentState()
    {
        // Arrange & Act
        var entity1 = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            IsActive = true
        };

        var entity2 = new PropertyPhotoTypeEntity
        {
            Id = 2,
            PhotoTypeCode = "BACK",
            PhotoTypeName = "Back View",
            IsActive = false
        };

        // Assert
        Assert.NotEqual(entity1.Id, entity2.Id);
        Assert.NotEqual(entity1.PhotoTypeCode, entity2.PhotoTypeCode);
        Assert.NotEqual(entity1.PhotoTypeName, entity2.PhotoTypeName);
        Assert.NotEqual(entity1.IsActive, entity2.IsActive);
    }
}
