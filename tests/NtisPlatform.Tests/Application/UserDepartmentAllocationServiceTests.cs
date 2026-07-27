using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class UserDepartmentAllocationServiceTests
{
    private sealed class TestData
    {
        public List<UserDepartmentAllocationEntity> DepartmentAllocations { get; init; } = new();
    }

    private static UserDepartmentAllocationService BuildService(TestData data)
    {
        var deptAllocRepoMock = new Mock<IRepository<UserDepartmentAllocationEntity, int>>();
        var loggerMock = new Mock<ILogger<UserDepartmentAllocationService>>();

        deptAllocRepoMock.Setup(r => r.GetQueryable()).Returns(data.DepartmentAllocations.BuildMock());

        return new UserDepartmentAllocationService(
            deptAllocRepoMock.Object,
            loggerMock.Object
        );
    }

    [Fact]
    public async Task GetMyAllocatedDepartmentsAsync_ReturnsOnlyActiveAllocationsAndMasters()
    {
        // Arrange
        var userId = 1;
        var dept1 = new DepartmentMasterEntity { Id = 10, DepartmentCode = "PTIS", DepartmentName = "Property Tax", IsActive = true };
        var dept2 = new DepartmentMasterEntity { Id = 20, DepartmentCode = "TL", DepartmentName = "Trade License", IsActive = false }; // Inactive department
        var dept3 = new DepartmentMasterEntity { Id = 30, DepartmentCode = "AM", DepartmentName = "Asset Management", IsActive = true };

        var data = new TestData
        {
            DepartmentAllocations = new List<UserDepartmentAllocationEntity>
            {
                new UserDepartmentAllocationEntity { Id = 1, UserId = userId, DepartmentId = 10, Department = dept1, IsActive = true },
                new UserDepartmentAllocationEntity { Id = 2, UserId = userId, DepartmentId = 20, Department = dept2, IsActive = true }, // Active allocation but inactive master
                new UserDepartmentAllocationEntity { Id = 3, UserId = userId, DepartmentId = 30, Department = dept3, IsActive = false } // Inactive allocation
            }
        };

        var service = BuildService(data);

        // Act
        var result = (await service.GetMyAllocatedDepartmentsAsync(userId)).ToList();

        // Assert
        Assert.Single(result); // Only dept1 matches all conditions (active allocation and active master)
        var deptResult = result.First();
        Assert.Equal(10, deptResult.DepartmentId);
        Assert.Equal("PTIS", deptResult.DepartmentCode);
        Assert.Equal("Property Tax", deptResult.DepartmentName);
    }

    [Fact]
    public async Task GetMyAllocatedDepartmentsAsync_ReturnsEmptyWhenNoAllocationsExist()
    {
        // Arrange
        var data = new TestData();
        var service = BuildService(data);

        // Act
        var result = await service.GetMyAllocatedDepartmentsAsync(1);

        // Assert
        Assert.Empty(result);
    }
}
