using NtisPlatform.Application.DTOs.Master.UserMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Master.UserMaster;

public class UserDepartmentAllocationDtoTests
{
    [Fact]
    public void Read_Dto_Properties_RoundTrip()
    {
        var dto = new UserDepartmentAllocationDto
        {
            UserId = 1,
            DepartmentId = 2,
            DepartmentName = "Dept",
            DepartmentNameLocal = "DeptLocal"
        };

        Assert.Equal(1, dto.UserId);
        Assert.Equal(2, dto.DepartmentId);
        Assert.Equal("Dept", dto.DepartmentName);
        Assert.Equal("DeptLocal", dto.DepartmentNameLocal);
    }

    [Fact]
    public void Create_Dto_Properties_RoundTrip()
    {
        var dto = new UserDepartmentAllocationCreateDto { DepartmentId = 7 };
        Assert.Equal(7, dto.DepartmentId);
    }

    [Fact]
    public void Update_Dto_Properties_RoundTrip()
    {
        var dto = new UserDepartmentAllocationUpdateDto { DepartmentId = 9 };
        Assert.Equal(9, dto.DepartmentId);
    }
}
