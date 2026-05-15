using NtisPlatform.Application.DTOs.Master.UserMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Master.UserMaster;

public class UserModuleAllocationDtoTests
{
    [Fact]
    public void Read_Dto_Properties_RoundTrip()
    {
        var dto = new UserModuleAllocationDto
        {
            UserId = 1,
            DepartmentId = 2,
            ModuleId = 3,
            DepartmentName = "Dept",
            ModuleName = "Module",
            ModuleNameLocal = "ModuleLocal"
        };

        Assert.Equal(1, dto.UserId);
        Assert.Equal(2, dto.DepartmentId);
        Assert.Equal(3, dto.ModuleId);
        Assert.Equal("Dept", dto.DepartmentName);
        Assert.Equal("Module", dto.ModuleName);
        Assert.Equal("ModuleLocal", dto.ModuleNameLocal);
    }

    [Fact]
    public void Create_Dto_Properties_RoundTrip()
    {
        var dto = new UserModuleAllocationCreateDto { DepartmentId = 1, ModuleId = 2 };
        Assert.Equal(1, dto.DepartmentId);
        Assert.Equal(2, dto.ModuleId);
    }

    [Fact]
    public void Update_Dto_Properties_RoundTrip()
    {
        var dto = new UserModuleAllocationUpdateDto { DepartmentId = 4, ModuleId = 5 };
        Assert.Equal(4, dto.DepartmentId);
        Assert.Equal(5, dto.ModuleId);
    }
}
