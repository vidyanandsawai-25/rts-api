using NtisPlatform.Application.DTOs.UserScreenAccess;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class UserScreenAccessDtoTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var dto = new UserScreenAccessDto
        {
            DepartmentId = 1,
            DepartmentName = "Dept",
            ModuleId = 2,
            ModuleName = "Module",
            UserId = 3,
            UserRoleId = 4,
            ScreenCode = "SCR",
            ScreenName = "Screen",
            ScreenNameLocal = "ScreenLocal",
            ScreenIcon = "icon",
            RoutePath = "/path",
            IsMenu = true,
            CanView = true,
            CanEdit = true,
            CanDelete = true,
            HaveFullAccess = true,
            HaveNoAccess = false,
            ScreenGroupName = "Group"
        };

        Assert.Equal(1, dto.DepartmentId);
        Assert.Equal("Dept", dto.DepartmentName);
        Assert.Equal(2, dto.ModuleId);
        Assert.Equal("Module", dto.ModuleName);
        Assert.Equal(3, dto.UserId);
        Assert.Equal(4, dto.UserRoleId);
        Assert.Equal("SCR", dto.ScreenCode);
        Assert.Equal("Screen", dto.ScreenName);
        Assert.Equal("ScreenLocal", dto.ScreenNameLocal);
        Assert.Equal("icon", dto.ScreenIcon);
        Assert.Equal("/path", dto.RoutePath);
        Assert.True(dto.IsMenu);
        Assert.True(dto.CanView);
        Assert.True(dto.CanEdit);
        Assert.True(dto.CanDelete);
        Assert.True(dto.HaveFullAccess);
        Assert.False(dto.HaveNoAccess);
        Assert.Equal("Group", dto.ScreenGroupName);
    }
}
