using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Tests.Core.Entities.Master;

/// <summary>
/// Unit tests for RoleWiseScreenAccessMasterEntity to ensure 100% code coverage
/// </summary>
public class RoleWiseScreenAccessMasterEntityTests
{
    [Fact]
    public void RoleWiseScreenAccessMasterEntity_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new RoleWiseScreenAccessMasterEntity
        {
            Id = 1,
            UserRoleId = 5,
            ScreenId = 10,
            CanView = true,
            CanEdit = true,
            CanDelete = false,
            HaveFullAccess = true,
            HaveNoAccess = false,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(5, entity.UserRoleId);
        Assert.Equal(10, entity.ScreenId);
        Assert.True(entity.CanView);
        Assert.True(entity.CanEdit);
        Assert.False(entity.CanDelete);
        Assert.True(entity.HaveFullAccess);
        Assert.False(entity.HaveNoAccess);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now, entity.UpdatedDate);
    }

    [Fact]
    public void RoleWiseScreenAccessMasterEntity_InheritsFromBaseEntity()
    {
        var entity = new RoleWiseScreenAccessMasterEntity();
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void RoleWiseScreenAccessMasterEntity_DefaultValues_SetCorrectly()
    {
        var entity = new RoleWiseScreenAccessMasterEntity();

        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.UserRoleId);
        Assert.Equal(0, entity.ScreenId);
        Assert.False(entity.CanView);
        Assert.False(entity.CanEdit);
        Assert.False(entity.CanDelete);
        Assert.False(entity.HaveFullAccess);
        Assert.False(entity.HaveNoAccess);
        Assert.True(entity.IsActive);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void RoleWiseScreenAccessMasterEntity_NavigationProperties_GetSet_WorksCorrectly()
    {
        var userRole = new UserRoleMasterEntity
        {
            Id = 5,
            UserRoleName = "Admin"
        };

        var screen = new ScreenMasterEntity
        {
            Id = 10,
            ScreenName = "Dashboard"
        };

        var entity = new RoleWiseScreenAccessMasterEntity
        {
            Id = 1,
            UserRoleId = 5,
            ScreenId = 10,
            UserRole = userRole,
            Screen = screen
        };

        Assert.NotNull(entity.UserRole);
        Assert.Equal(5, entity.UserRole.Id);
        Assert.Equal("Admin", entity.UserRole.UserRoleName);

        Assert.NotNull(entity.Screen);
        Assert.Equal(10, entity.Screen.Id);
        Assert.Equal("Dashboard", entity.Screen.ScreenName);
    }

    [Fact]
    public void RoleWiseScreenAccessMasterEntity_NavigationProperties_CanBeNull()
    {
        var entity = new RoleWiseScreenAccessMasterEntity
        {
            Id = 1,
            UserRoleId = 5,
            ScreenId = 10,
            UserRole = null,
            Screen = null
        };

        Assert.Null(entity.UserRole);
        Assert.Null(entity.Screen);
    }

    [Fact]
    public void RoleWiseScreenAccessMasterEntity_PermissionFlags_AllTrue_WorksCorrectly()
    {
        var entity = new RoleWiseScreenAccessMasterEntity
        {
            CanView = true,
            CanEdit = true,
            CanDelete = true,
            HaveFullAccess = true,
            HaveNoAccess = true
        };

        Assert.True(entity.CanView);
        Assert.True(entity.CanEdit);
        Assert.True(entity.CanDelete);
        Assert.True(entity.HaveFullAccess);
        Assert.True(entity.HaveNoAccess);
    }

    [Fact]
    public void RoleWiseScreenAccessMasterEntity_PermissionFlags_AllFalse_WorksCorrectly()
    {
        var entity = new RoleWiseScreenAccessMasterEntity
        {
            CanView = false,
            CanEdit = false,
            CanDelete = false,
            HaveFullAccess = false,
            HaveNoAccess = false
        };

        Assert.False(entity.CanView);
        Assert.False(entity.CanEdit);
        Assert.False(entity.CanDelete);
        Assert.False(entity.HaveFullAccess);
        Assert.False(entity.HaveNoAccess);
    }

    [Fact]
    public void RoleWiseScreenAccessMasterEntity_PartialPermissions_WorksCorrectly()
    {
        var entity = new RoleWiseScreenAccessMasterEntity
        {
            CanView = true,
            CanEdit = false,
            CanDelete = false,
            HaveFullAccess = false,
            HaveNoAccess = false
        };

        Assert.True(entity.CanView);
        Assert.False(entity.CanEdit);
        Assert.False(entity.CanDelete);
        Assert.False(entity.HaveFullAccess);
        Assert.False(entity.HaveNoAccess);
    }

    [Fact]
    public void RoleWiseScreenAccessMasterEntity_BaseEntityProperties_WorkCorrectly()
    {
        var now = DateTime.Now;
        var entity = new RoleWiseScreenAccessMasterEntity
        {
            Id = 100,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(100, entity.Id);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.Equal(now.AddHours(1), entity.UpdatedDate);
    }

    [Fact]
    public void RoleWiseScreenAccessMasterEntity_ForeignKeys_GetSet_WorksCorrectly()
    {
        var entity = new RoleWiseScreenAccessMasterEntity
        {
            UserRoleId = 100,
            ScreenId = 200
        };

        Assert.Equal(100, entity.UserRoleId);
        Assert.Equal(200, entity.ScreenId);
    }
}
