using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Tests.Core.Entities;

public class PropertyDescriptionAndTypeOfUseValidationEntityTests
{
    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationEntity_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = now,
            UpdatedBy = 200,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(5, entity.PropertyTypeId);
        Assert.Equal(10, entity.TypeOfUseId);
        Assert.True(entity.IsActive);
        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationEntity_InheritsFromBaseEntity()
    {
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity();
        Assert.IsAssignableFrom<NtisPlatform.Core.Entities.BaseEntity>(entity);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationEntity_DefaultValues_SetCorrectly()
    {
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity();

        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.PropertyTypeId);
        Assert.Equal(0, entity.TypeOfUseId);
        Assert.True(entity.IsActive);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationEntity_IsActive_BothValues_WorkCorrectly()
    {
        var entity1 = new PropertyDescriptionAndTypeOfUseValidationEntity { IsActive = true };
        var entity2 = new PropertyDescriptionAndTypeOfUseValidationEntity { IsActive = false };

        Assert.True(entity1.IsActive);
        Assert.False(entity2.IsActive);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationEntity_ForeignKeyProperties_WorkCorrectly()
    {
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10
        };

        Assert.Equal(5, entity.PropertyTypeId);
        Assert.Equal(10, entity.TypeOfUseId);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationEntity_BaseEntityProperties_WorkCorrectly()
    {
        var now = DateTime.Now;
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 100,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now.AddDays(1)
        };

        Assert.Equal(100, entity.Id);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now.AddDays(1), entity.UpdatedDate);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationEntity_MultipleInstances_Independent()
    {
        var entity1 = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10
        };

        var entity2 = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 2,
            PropertyTypeId = 6,
            TypeOfUseId = 11
        };

        Assert.NotEqual(entity1.Id, entity2.Id);
        Assert.NotEqual(entity1.PropertyTypeId, entity2.PropertyTypeId);
        Assert.NotEqual(entity1.TypeOfUseId, entity2.TypeOfUseId);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationEntity_CreatedByUpdatedBy_CanBeNull()
    {
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            CreatedBy = null,
            UpdatedBy = null
        };

        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedBy);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationEntity_Dates_CanBeNull()
    {
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            CreatedDate = null,
            UpdatedDate = null
        };

        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationEntity_PropertyTypeIdTypeOfUseId_PositiveValues()
    {
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            PropertyTypeId = 999,
            TypeOfUseId = 888
        };

        Assert.Equal(999, entity.PropertyTypeId);
        Assert.Equal(888, entity.TypeOfUseId);
    }
}
