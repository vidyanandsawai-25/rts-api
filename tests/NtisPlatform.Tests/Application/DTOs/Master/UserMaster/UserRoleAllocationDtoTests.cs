using NtisPlatform.Application.DTOs.Master.UserMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Master.UserMaster;

public class UserRoleAllocationDtoTests
{
    [Fact]
    public void Read_Dto_Properties_RoundTrip()
    {
        var dto = new UserRoleAllocationDto
        {
            UserId = 1,
            DepartmentId = 2,
            UserRoleId = 3,
            DepartmentName = "Dept",
            UserRoleName = "Role"
        };

        Assert.Equal(1, dto.UserId);
        Assert.Equal(2, dto.DepartmentId);
        Assert.Equal(3, dto.UserRoleId);
        Assert.Equal("Dept", dto.DepartmentName);
        Assert.Equal("Role", dto.UserRoleName);
    }

    [Fact]
    public void Create_Dto_Properties_RoundTrip()
    {
        var now = DateTime.Now;
        var dto = new UserRoleAllocationCreateDto { DepartmentId = 4, UserRoleId = 5, CreatedDate = now };

        Assert.Equal(4, dto.DepartmentId);
        Assert.Equal(5, dto.UserRoleId);
        Assert.Equal(now, dto.CreatedDate);
    }

    [Fact]
    public void Update_Dto_Properties_RoundTrip()
    {
        var now = DateTime.Now;
        var dto = new UserRoleAllocationUpdateDto { DepartmentId = 6, UserRoleId = 7, UpdatedDate = now };

        Assert.Equal(6, dto.DepartmentId);
        Assert.Equal(7, dto.UserRoleId);
        Assert.Equal(now, dto.UpdatedDate);
    }
}
