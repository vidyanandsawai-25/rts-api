using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Tests for PropertyAssessmentStatusEntity
/// Ensures entity properties and inheritance work correctly
/// </summary>
public class PropertyAssessmentStatusEntityTests
{
    #region Constructor and Property Tests

    [Fact]
    public void PropertyAssessmentStatusEntity_CanBeInstantiated()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity();

        // Assert
        Assert.NotNull(entity);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_StatusName_DefaultValue_IsEmptyString()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity();

        // Assert
        Assert.Equal(string.Empty, entity.StatusName);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_CanSetAndGetStatusName()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity();

        // Act
        entity.StatusName = "Pending Assessment";

        // Assert
        Assert.Equal("Pending Assessment", entity.StatusName);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_InheritsFromBaseEntity()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity();

        // Assert
        Assert.IsAssignableFrom<NtisPlatform.Core.Entities.BaseEntity>(entity);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_HasIdProperty()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity { Id = 1 };

        // Assert
        Assert.Equal(1, entity.Id);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_HasIsActiveProperty()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity { IsActive = true };

        // Assert
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_HasCreatedByProperty()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity { CreatedBy = 123 };

        // Assert
        Assert.Equal(123, entity.CreatedBy);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_HasCreatedDateProperty()
    {
        // Arrange
        var date = DateTime.UtcNow;
        var entity = new PropertyAssessmentStatusEntity();

        // Act
        entity.CreatedDate = date;

        // Assert
        Assert.Equal(date, entity.CreatedDate);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_HasUpdatedByProperty()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity { UpdatedBy = 456 };

        // Assert
        Assert.Equal(456, entity.UpdatedBy);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_HasUpdatedDateProperty()
    {
        // Arrange
        var date = DateTime.UtcNow;
        var entity = new PropertyAssessmentStatusEntity();

        // Act
        entity.UpdatedDate = date;

        // Assert
        Assert.Equal(date, entity.UpdatedDate);
    }

    #endregion

    #region Property Validation Tests

    [Fact]
    public void PropertyAssessmentStatusEntity_StatusName_ShouldNotBeNull()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity();

        // Assert
        Assert.NotNull(entity.StatusName);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_StatusName_CanBeEmptyString()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity();

        // Act
        entity.StatusName = string.Empty;

        // Assert
        Assert.Equal(string.Empty, entity.StatusName);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_StatusName_CanContainSpaces()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity();

        // Act
        entity.StatusName = "Status With Spaces";

        // Assert
        Assert.Equal("Status With Spaces", entity.StatusName);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_StatusName_CanBeMaxLength()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity();
        var longName = new string('A', 30); // Max length is 30

        // Act
        entity.StatusName = longName;

        // Assert
        Assert.Equal(longName, entity.StatusName);
        Assert.Equal(30, entity.StatusName.Length);
    }

    #endregion

    #region State Tests

    [Fact]
    public void PropertyAssessmentStatusEntity_CanBeActive()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity
        {
            StatusName = "Active Status",
            IsActive = true
        };

        // Assert
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_CanBeInactive()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity
        {
            StatusName = "Inactive Status",
            IsActive = false
        };

        // Assert
        Assert.False(entity.IsActive);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_CanToggleActiveState()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity { IsActive = true };

        // Act
        entity.IsActive = false;

        // Assert
        Assert.False(entity.IsActive);
    }

    #endregion

    #region Object Initialization Tests

    [Fact]
    public void PropertyAssessmentStatusEntity_CanBeInitializedWithObjectInitializer()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity
        {
            Id = 1,
            StatusName = "Approved",
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = DateTime.UtcNow,
            UpdatedBy = 200,
            UpdatedDate = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal("Approved", entity.StatusName);
        Assert.True(entity.IsActive);
        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_MultipleInstances_AreIndependent()
    {
        // Arrange & Act
        var entity1 = new PropertyAssessmentStatusEntity { StatusName = "Status 1" };
        var entity2 = new PropertyAssessmentStatusEntity { StatusName = "Status 2" };

        // Assert
        Assert.NotEqual(entity1.StatusName, entity2.StatusName);
    }

    #endregion

    #region Audit Fields Tests

    [Fact]
    public void PropertyAssessmentStatusEntity_AuditFields_CanBeNull()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity
        {
            CreatedBy = null,
            CreatedDate = null,
            UpdatedBy = null,
            UpdatedDate = null
        };

        // Assert
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_AuditFields_CanBeSet()
    {
        // Arrange
        var createdDate = DateTime.UtcNow.AddDays(-1);
        var updatedDate = DateTime.UtcNow;

        // Act
        var entity = new PropertyAssessmentStatusEntity
        {
            CreatedBy = 1,
            CreatedDate = createdDate,
            UpdatedBy = 2,
            UpdatedDate = updatedDate
        };

        // Assert
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(createdDate, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(updatedDate, entity.UpdatedDate);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_CreatedDate_CanBeBeforeUpdatedDate()
    {
        // Arrange
        var createdDate = DateTime.UtcNow.AddDays(-5);
        var updatedDate = DateTime.UtcNow;

        // Act
        var entity = new PropertyAssessmentStatusEntity
        {
            CreatedDate = createdDate,
            UpdatedDate = updatedDate
        };

        // Assert
        Assert.True(entity.CreatedDate < entity.UpdatedDate);
    }

    #endregion

    #region Type Tests

    [Fact]
    public void PropertyAssessmentStatusEntity_IsReferenceType()
    {
        // Arrange
        var entity1 = new PropertyAssessmentStatusEntity { StatusName = "Test" };
        var entity2 = entity1;

        // Act
        entity2.StatusName = "Modified";

        // Assert
        Assert.Equal("Modified", entity1.StatusName);
        Assert.Same(entity1, entity2);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_DefaultId_IsZero()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity();

        // Assert
        Assert.Equal(0, entity.Id);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_DefaultIsActive_IsTrue()
    {
        // Arrange & Act
        var entity = new PropertyAssessmentStatusEntity();

        // Assert
        Assert.True(entity.IsActive);
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void PropertyAssessmentStatusEntity_CanBeAddedToList()
    {
        // Arrange
        var list = new List<PropertyAssessmentStatusEntity>();
        var entity = new PropertyAssessmentStatusEntity { StatusName = "Test" };

        // Act
        list.Add(entity);

        // Assert
        Assert.Single(list);
        Assert.Contains(entity, list);
    }

    [Fact]
    public void PropertyAssessmentStatusEntity_CanBeComparedById()
    {
        // Arrange
        var entity1 = new PropertyAssessmentStatusEntity { Id = 1 };
        var entity2 = new PropertyAssessmentStatusEntity { Id = 1 };
        var entity3 = new PropertyAssessmentStatusEntity { Id = 2 };

        // Assert
        Assert.Equal(entity1.Id, entity2.Id);
        Assert.NotEqual(entity1.Id, entity3.Id);
    }

    #endregion
}
