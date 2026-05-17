using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.UserScreenAccess;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class UserScreenAccessServiceTests
{
    private sealed class TestData
    {
        public List<DepartmentMasterEntity> Departments { get; init; } = new();
        public List<ModuleMasterEntity> Modules { get; init; } = new();
        public List<ScreenMasterEntity> Screens { get; init; } = new();
        public List<ScreenGroupMasterEntity> ScreenGroups { get; init; } = new();
        public List<RoleWiseScreenAccessMasterEntity> RoleAccess { get; init; } = new();
        public List<UserRoleAllocationEntity> UserRoleAllocations { get; init; } = new();
        public List<UserEntity> Users { get; init; } = new();
    }

    private static TestData SeedDefault()
    {
        return new TestData
        {
            Departments = new()
            {
                new DepartmentMasterEntity { Id = 10, DepartmentName = "Property", IsActive = true }
            },
            Modules = new()
            {
                new ModuleMasterEntity { Id = 20, DepartmentId = 10, ModuleName = "Tax", IsActive = true }
            },
            Screens = new()
            {
                new ScreenMasterEntity
                {
                    Id = 30, ModuleId = 20, ScreenGroupId = 40,
                    ScreenCode = "S1", ScreenName = "Property Tax", ScreenNameLocal = "PT",
                    ScreenIcon = "icon", RoutePath = "/tax", IsMenu = true, IsActive = true
                }
            },
            ScreenGroups = new()
            {
                new ScreenGroupMasterEntity { Id = 40, ScreenGroupName = "Main", IsActive = true }
            },
            RoleAccess = new()
            {
                new RoleWiseScreenAccessMasterEntity
                {
                    Id = 50, UserRoleId = 60, ScreenId = 30,
                    CanView = true, CanEdit = false, CanDelete = false,
                    HaveFullAccess = false, HaveNoAccess = false, IsActive = true
                }
            },
            UserRoleAllocations = new()
            {
                new UserRoleAllocationEntity { Id = 70, UserId = 80, DepartmentId = 10, UserRoleId = 60, IsActive = true }
            },
            Users = new()
            {
                new UserEntity { Id = 80, UserName = "alice" }
            }
        };
    }

    private static UserScreenAccessService Build(TestData data)
    {
        var departmentRepo = new Mock<IRepository<DepartmentMasterEntity, int>>();
        var moduleRepo = new Mock<IRepository<ModuleMasterEntity, int>>();
        var screenRepo = new Mock<IRepository<ScreenMasterEntity, int>>();
        var roleAccessRepo = new Mock<IRepository<RoleWiseScreenAccessMasterEntity, int>>();
        var userRepo = new Mock<IRepository<UserEntity, int>>();
        var userRoleAllocRepo = new Mock<IRepository<UserRoleAllocationEntity, int>>();
        var screenGroupRepo = new Mock<IRepository<ScreenGroupMasterEntity, int>>();
        var logger = new Mock<ILogger<UserScreenAccessService>>();

        departmentRepo.Setup(r => r.GetQueryable()).Returns(data.Departments.BuildMock());
        moduleRepo.Setup(r => r.GetQueryable()).Returns(data.Modules.BuildMock());
        screenRepo.Setup(r => r.GetQueryable()).Returns(data.Screens.BuildMock());
        roleAccessRepo.Setup(r => r.GetQueryable()).Returns(data.RoleAccess.BuildMock());
        userRepo.Setup(r => r.GetQueryable()).Returns(data.Users.BuildMock());
        userRoleAllocRepo.Setup(r => r.GetQueryable()).Returns(data.UserRoleAllocations.BuildMock());
        screenGroupRepo.Setup(r => r.GetQueryable()).Returns(data.ScreenGroups.BuildMock());

        return new UserScreenAccessService(
            departmentRepo.Object,
            moduleRepo.Object,
            screenRepo.Object,
            roleAccessRepo.Object,
            userRepo.Object,
            userRoleAllocRepo.Object,
            screenGroupRepo.Object,
            logger.Object);
    }

    [Fact]
    public async Task GetUserScreenAccessAsync_ReturnsResultsFromJoin()
    {
        var service = Build(SeedDefault());
        var query = new UserScreenAccessQueryParameters { PageNumber = 1, PageSize = 10 };

        var result = await service.GetUserScreenAccessAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        var item = result.Items.Single();
        Assert.Equal(80, item.UserId);
        Assert.Equal("Property Tax", item.ScreenName);
        Assert.True(item.CanView);
    }

    [Fact]
    public async Task GetUserScreenAccessAsync_FiltersByUserId()
    {
        var data = SeedDefault();
        data.UserRoleAllocations.Add(new UserRoleAllocationEntity { Id = 71, UserId = 999, DepartmentId = 10, UserRoleId = 60, IsActive = true });
        data.Users.Add(new UserEntity { Id = 999, UserName = "bob" });
        var service = Build(data);

        var result = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters { UserId = 80, PageSize = 100 });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(80, result.Items.Single().UserId);
    }

    [Fact]
    public async Task GetUserScreenAccessAsync_FiltersByUserRoleId()
    {
        var data = SeedDefault();
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity { Id = 51, UserRoleId = 61, ScreenId = 30, CanView = true });
        data.UserRoleAllocations.Add(new UserRoleAllocationEntity { Id = 71, UserId = 81, DepartmentId = 10, UserRoleId = 61 });
        data.Users.Add(new UserEntity { Id = 81, UserName = "carol" });
        var service = Build(data);

        var result = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters { UserRoleId = 61, PageSize = 100 });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(61, result.Items.Single().UserRoleId);
    }

    [Fact]
    public async Task GetUserScreenAccessAsync_FiltersByDepartmentId()
    {
        var data = SeedDefault();
        data.Departments.Add(new DepartmentMasterEntity { Id = 11, DepartmentName = "Other" });
        data.Modules.Add(new ModuleMasterEntity { Id = 21, DepartmentId = 11, ModuleName = "X" });
        data.Screens.Add(new ScreenMasterEntity { Id = 31, ModuleId = 21, ScreenGroupId = 40, ScreenName = "Other" });
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity { Id = 51, UserRoleId = 60, ScreenId = 31, CanView = true });
        data.UserRoleAllocations.Add(new UserRoleAllocationEntity { Id = 71, UserId = 80, DepartmentId = 11, UserRoleId = 60 });
        var service = Build(data);

        var result = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters { DepartmentId = 11, PageSize = 100 });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(11, result.Items.Single().DepartmentId);
    }

    [Fact]
    public async Task GetUserScreenAccessAsync_FiltersByModuleId()
    {
        var data = SeedDefault();
        data.Modules.Add(new ModuleMasterEntity { Id = 22, DepartmentId = 10, ModuleName = "Y" });
        data.Screens.Add(new ScreenMasterEntity { Id = 31, ModuleId = 22, ScreenGroupId = 40, ScreenName = "Y" });
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity { Id = 51, UserRoleId = 60, ScreenId = 31, CanView = true });
        var service = Build(data);

        var result = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters { ModuleId = 22, PageSize = 100 });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(22, result.Items.Single().ModuleId);
    }

    [Fact]
    public async Task GetUserScreenAccessAsync_AppliesSearchTerm()
    {
        var service = Build(SeedDefault());

        var result = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters { SearchTerm = "Property", PageSize = 100 });
        Assert.Equal(1, result.TotalCount);

        var empty = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters { SearchTerm = "NoMatch", PageSize = 100 });
        Assert.Equal(0, empty.TotalCount);
    }

    [Theory]
    [InlineData("departmentname", "asc")]
    [InlineData("departmentname", "desc")]
    [InlineData("modulename", "asc")]
    [InlineData("modulename", "desc")]
    [InlineData("screenname", "asc")]
    [InlineData("screenname", "desc")]
    [InlineData(null, null)]
    public async Task GetUserScreenAccessAsync_AppliesSort(string? sortBy, string? sortOrder)
    {
        var service = Build(SeedDefault());

        var result = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters
        {
            SortBy = sortBy,
            SortOrder = sortOrder,
            PageSize = 100
        });

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetUserScreenAccessAsync_AppliesPagination()
    {
        var data = SeedDefault();
        for (int i = 0; i < 5; i++)
        {
            data.Screens.Add(new ScreenMasterEntity { Id = 100 + i, ModuleId = 20, ScreenGroupId = 40, ScreenName = $"S{i}" });
            data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity { Id = 100 + i, UserRoleId = 60, ScreenId = 100 + i, CanView = true });
        }
        var service = Build(data);

        var page1 = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters { PageNumber = 1, PageSize = 3 });
        var page2 = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters { PageNumber = 2, PageSize = 3 });

        Assert.Equal(6, page1.TotalCount);
        Assert.Equal(3, page1.Items.Count());
        Assert.Equal(3, page2.Items.Count());
    }

    [Fact]
    public async Task GetUserScreensByUserIdAsync_ReturnsAccessibleScreens()
    {
        var service = Build(SeedDefault());

        var result = (await service.GetUserScreensByUserIdAsync(80)).ToList();

        Assert.Single(result);
        Assert.Equal("Property Tax", result.Single().ScreenName);
        Assert.True(result.Single().CanView);
    }

    [Fact]
    public async Task GetUserScreensByUserIdAsync_ExcludesScreensWithoutPermission()
    {
        var data = SeedDefault();
        // Replace existing role-access record so user has NO grant on the screen
        data.RoleAccess.Clear();
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity
        {
            Id = 51, UserRoleId = 60, ScreenId = 30,
            CanView = false, CanEdit = false, CanDelete = false,
            HaveFullAccess = false, HaveNoAccess = false
        });
        var service = Build(data);

        var result = (await service.GetUserScreensByUserIdAsync(80)).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserScreensByUserIdAsync_ExcludesHaveNoAccess()
    {
        var data = SeedDefault();
        data.RoleAccess.Single().HaveNoAccess = true;
        var service = Build(data);

        var result = (await service.GetUserScreensByUserIdAsync(80)).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserScreensByUserIdAsync_DeduplicatesAcrossRoles()
    {
        var data = SeedDefault();
        // Two roles granting access to the same screen
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity
        {
            Id = 51, UserRoleId = 61, ScreenId = 30,
            CanView = true, CanEdit = true, CanDelete = false
        });
        data.UserRoleAllocations.Add(new UserRoleAllocationEntity { Id = 72, UserId = 80, DepartmentId = 10, UserRoleId = 61 });
        var service = Build(data);

        var result = (await service.GetUserScreensByUserIdAsync(80)).ToList();

        Assert.Single(result);
        Assert.True(result[0].CanView);
        Assert.True(result[0].CanEdit);
    }

    [Fact]
    public async Task GetUserScreenAccessAsync_PropagatesException()
    {
        var data = SeedDefault();
        var departmentRepo = new Mock<IRepository<DepartmentMasterEntity, int>>();
        departmentRepo.Setup(r => r.GetQueryable()).Throws(new InvalidOperationException("db down"));
        var service = new UserScreenAccessService(
            departmentRepo.Object,
            new Mock<IRepository<ModuleMasterEntity, int>>().Object,
            new Mock<IRepository<ScreenMasterEntity, int>>().Object,
            new Mock<IRepository<RoleWiseScreenAccessMasterEntity, int>>().Object,
            new Mock<IRepository<UserEntity, int>>().Object,
            new Mock<IRepository<UserRoleAllocationEntity, int>>().Object,
            new Mock<IRepository<ScreenGroupMasterEntity, int>>().Object,
            new Mock<ILogger<UserScreenAccessService>>().Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters()));
    }

    [Fact]
    public async Task GetUserScreenAccessAsync_ExcludesInactiveScreens()
    {
        var data = SeedDefault();
        // Add an inactive screen with valid permissions and allocations
        data.Screens.Add(new ScreenMasterEntity
        {
            Id = 31,
            ModuleId = 20,
            ScreenGroupId = 40,
            ScreenCode = "S2",
            ScreenName = "Inactive Tax Screen",
            ScreenNameLocal = "ITS",
            ScreenIcon = "icon",
            RoutePath = "/inactive",
            IsMenu = true,
            IsActive = false // Inactive!
        });
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity
        {
            Id = 51,
            UserRoleId = 60,
            ScreenId = 31,
            CanView = true,
            CanEdit = false,
            CanDelete = false,
            HaveFullAccess = false,
            HaveNoAccess = false,
            IsActive = true
        });
        var service = Build(data);

        var result = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters { PageSize = 100 });

        // Should only return the active screen, not the inactive one
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Property Tax", result.Items.Single().ScreenName);
        Assert.DoesNotContain(result.Items, item => item.ScreenName == "Inactive Tax Screen");
    }

    [Fact]
    public async Task GetUserScreensByUserIdAsync_ExcludesInactiveScreens()
    {
        var data = SeedDefault();
        // Add an inactive screen with valid permissions
        data.Screens.Add(new ScreenMasterEntity
        {
            Id = 31,
            ModuleId = 20,
            ScreenGroupId = 40,
            ScreenCode = "S2",
            ScreenName = "Inactive Menu",
            ScreenNameLocal = "IM",
            ScreenIcon = "icon",
            RoutePath = "/inactive-menu",
            IsMenu = true,
            IsActive = false // Inactive - should be excluded from user's accessible screens
        });
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity
        {
            Id = 51,
            UserRoleId = 60,
            ScreenId = 31,
            CanView = true,
            CanEdit = true,
            CanDelete = false,
            HaveFullAccess = false,
            HaveNoAccess = false,
            IsActive = true
        });
        var service = Build(data);

        var result = (await service.GetUserScreensByUserIdAsync(80)).ToList();

        // User should only see active screens in their menu
        Assert.Single(result);
        Assert.Equal("Property Tax", result.Single().ScreenName);
        Assert.DoesNotContain(result, item => item.ScreenName == "Inactive Menu");
    }

    [Fact]
    public async Task GetUserScreenAccessAsync_IncludesActiveScreensWhenBothActiveAndInactiveExist()
    {
        var data = SeedDefault();
        // Add both active and inactive screens to verify filtering works correctly
        data.Screens.Add(new ScreenMasterEntity
        {
            Id = 31,
            ModuleId = 20,
            ScreenGroupId = 40,
            ScreenCode = "S2",
            ScreenName = "Active Screen 2",
            IsActive = true
        });
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity
        {
            Id = 51,
            UserRoleId = 60,
            ScreenId = 31,
            CanView = true
        });

        data.Screens.Add(new ScreenMasterEntity
        {
            Id = 32,
            ModuleId = 20,
            ScreenGroupId = 40,
            ScreenCode = "S3",
            ScreenName = "Inactive Screen",
            IsActive = false
        });
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity
        {
            Id = 52,
            UserRoleId = 60,
            ScreenId = 32,
            CanView = true
        });

        var service = Build(data);

        var result = await service.GetUserScreenAccessAsync(new UserScreenAccessQueryParameters { PageSize = 100 });

        // Should return only the 2 active screens
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Contains(result.Items, item => item.ScreenName == "Property Tax");
        Assert.Contains(result.Items, item => item.ScreenName == "Active Screen 2");
        Assert.DoesNotContain(result.Items, item => item.ScreenName == "Inactive Screen");
    }

    [Fact]
    public async Task GetUserScreensByUserIdAsync_IncludesOnlyActiveScreensInMenu()
    {
        var data = SeedDefault();
        // Add multiple screens: some active, some inactive
        data.Screens.Add(new ScreenMasterEntity
        {
            Id = 31,
            ModuleId = 20,
            ScreenGroupId = 40,
            ScreenCode = "S2",
            ScreenName = "Active Menu Item",
            IsActive = true,
            IsMenu = true
        });
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity
        {
            Id = 51,
            UserRoleId = 60,
            ScreenId = 31,
            CanView = true,
            HaveFullAccess = true
        });

        data.Screens.Add(new ScreenMasterEntity
        {
            Id = 32,
            ModuleId = 20,
            ScreenGroupId = 40,
            ScreenCode = "S3",
            ScreenName = "Deprecated Menu Item",
            IsActive = false,
            IsMenu = true
        });
        data.RoleAccess.Add(new RoleWiseScreenAccessMasterEntity
        {
            Id = 52,
            UserRoleId = 60,
            ScreenId = 32,
            HaveFullAccess = true
        });

        var service = Build(data);

        var result = (await service.GetUserScreensByUserIdAsync(80)).ToList();

        // User menu should contain only active screens
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.ScreenName == "Property Tax");
        Assert.Contains(result, item => item.ScreenName == "Active Menu Item");
        Assert.DoesNotContain(result, item => item.ScreenName == "Deprecated Menu Item");
    }
}
