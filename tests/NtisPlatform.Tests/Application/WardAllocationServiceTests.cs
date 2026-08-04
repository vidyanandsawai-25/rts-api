using AutoMapper;
using MockQueryable;
using System.Linq;
using Moq;
using NtisPlatform.Application.DTOs.wardallocation;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class WardAllocationServiceTests
{
    private readonly Mock<IRepository<GlobalSurveyWardAllocationEntity, int>> _repoMock;
    private readonly Mock<IRepository<UserEntity, int>> _userRepoMock;
    private readonly Mock<IRepository<UserDepartmentAllocationEntity, int>> _userDeptAllocRepoMock;
    private readonly Mock<IRepository<UserModuleAllocationEntity, int>> _userModuleAllocRepoMock;
    private readonly Mock<IRepository<DepartmentMasterEntity, int>> _deptRepoMock;
    private readonly Mock<IRepository<ModuleMasterEntity, int>> _moduleRepoMock;
    private readonly Mock<IRepository<ZoneEntity, int>> _zoneRepoMock;
    private readonly Mock<IRepository<WardEntity, int>> _wardRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly WardAllocationService _service;

    public WardAllocationServiceTests()
    {
        _repoMock = new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>();
        _userRepoMock = new Mock<IRepository<UserEntity, int>>();
        _userDeptAllocRepoMock = new Mock<IRepository<UserDepartmentAllocationEntity, int>>();
        _userModuleAllocRepoMock = new Mock<IRepository<UserModuleAllocationEntity, int>>();
        _deptRepoMock = new Mock<IRepository<DepartmentMasterEntity, int>>();
        _moduleRepoMock = new Mock<IRepository<ModuleMasterEntity, int>>();
        _zoneRepoMock = new Mock<IRepository<ZoneEntity, int>>();
        _wardRepoMock = new Mock<IRepository<WardEntity, int>>();
        _uowMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();

        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new WardAllocationService(
            _repoMock.Object,
            _userRepoMock.Object,
            _userDeptAllocRepoMock.Object,
            _userModuleAllocRepoMock.Object,
            _deptRepoMock.Object,
            _moduleRepoMock.Object,
            _zoneRepoMock.Object,
            _wardRepoMock.Object,
            _uowMock.Object,
            _mapperMock.Object);
    }

  
    [Fact]
    public async Task CreateFlexibleAsync_Succeeds_CreatesAllocations()
    {
        // Arrange: valid user/module/department allocations
        var createDto = new CreateFlexibleWardAllocationDto
        {
            UserId = 10,
            DepartmentId = 20,
            ModuleId = 30,
            Allocations = new List<ZoneWardAllocationDto>
            {
                new ZoneWardAllocationDto { ZoneId = 100, WardIds = new List<int> { 1000 } },
                new ZoneWardAllocationDto { ZoneId = 200, WardIds = new List<int> { 2000 } }
            },
            IsActive = true,
            CreatedBy = 99
        };

        // user exists
        var users = new List<UserEntity> { new UserEntity { Id = 10 } };
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.ToList().BuildMock<UserEntity>());

        // department exists
        var depts = new List<DepartmentMasterEntity> { new DepartmentMasterEntity { Id = 20, IsActive = true } };
        _deptRepoMock.Setup(r => r.GetQueryable()).Returns(depts.ToList().BuildMock<DepartmentMasterEntity>());

        // module exists
        var modules = new List<ModuleMasterEntity> { new ModuleMasterEntity { Id = 30, IsActive = true } };
        _moduleRepoMock.Setup(r => r.GetQueryable()).Returns(modules.ToList().BuildMock<ModuleMasterEntity>());

        // user department allocation exists
        var uda = new List<UserDepartmentAllocationEntity> { new UserDepartmentAllocationEntity { UserId = 10, DepartmentId = 20, IsActive = true } };
        _userDeptAllocRepoMock.Setup(r => r.GetQueryable()).Returns(uda.ToList().BuildMock<UserDepartmentAllocationEntity>());

        // user module allocation exists
        var uma = new List<UserModuleAllocationEntity> { new UserModuleAllocationEntity { UserId = 10, DepartmentId = 20, ModuleId = 30, IsActive = true } };
        _userModuleAllocRepoMock.Setup(r => r.GetQueryable()).Returns(uma.ToList().BuildMock<UserModuleAllocationEntity>());

        // ward-zone relationships valid
        var wards = new List<WardEntity>
        {
            new WardEntity { Id = 1000, ZoneId = 100, IsActive = true },
            new WardEntity { Id = 2000, ZoneId = 200, IsActive = true }
        };
        _wardRepoMock.Setup(r => r.GetQueryable()).Returns(wards.ToList().BuildMock<WardEntity>());

        // repository has no existing allocations
        _repoMock.Setup(r => r.GetQueryable()).Returns(new List<GlobalSurveyWardAllocationEntity>().ToList().BuildMock<GlobalSurveyWardAllocationEntity>());

        var added = new List<GlobalSurveyWardAllocationEntity>();
        var nextId = 1;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<GlobalSurveyWardAllocationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GlobalSurveyWardAllocationEntity e, CancellationToken ct) =>
            {
                e.Id = nextId++;
                added.Add(e);
                return e;
            });

        // Ensure repository query returns the added entities when service builds result
        _repoMock.Setup(r => r.GetQueryable()).Returns(() => added.ToList().BuildMock<GlobalSurveyWardAllocationEntity>());

        // Act
        var result = await _service.CreateFlexibleAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<GlobalSurveyWardAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateFlexibleAsync_WhenExistingAllocations_ThrowsInvalidOperationException()
    {
        var createDto = new CreateFlexibleWardAllocationDto
        {
            UserId = 10,
            DepartmentId = 20,
            ModuleId = 30,
            Allocations = new List<ZoneWardAllocationDto>
            {
                new ZoneWardAllocationDto { ZoneId = 100, WardIds = new List<int> { 1000 } }
            },
            IsActive = true,
            CreatedBy = 99
        };

        // Setup minimal validations to pass
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity> { new UserEntity { Id = 10 } }.ToList().BuildMock<UserEntity>());
        _deptRepoMock.Setup(r => r.GetQueryable()).Returns(new List<DepartmentMasterEntity> { new DepartmentMasterEntity { Id = 20, IsActive = true } }.ToList().BuildMock<DepartmentMasterEntity>());
        _moduleRepoMock.Setup(r => r.GetQueryable()).Returns(new List<ModuleMasterEntity> { new ModuleMasterEntity { Id = 30, IsActive = true } }.ToList().BuildMock<ModuleMasterEntity>());
        _userDeptAllocRepoMock.Setup(r => r.GetQueryable()).Returns(new List<UserDepartmentAllocationEntity> { new UserDepartmentAllocationEntity { UserId = 10, DepartmentId = 20, IsActive = true } }.ToList().BuildMock<UserDepartmentAllocationEntity>());
        _userModuleAllocRepoMock.Setup(r => r.GetQueryable()).Returns(new List<UserModuleAllocationEntity> { new UserModuleAllocationEntity { UserId = 10, DepartmentId = 20, ModuleId = 30, IsActive = true } }.ToList().BuildMock<UserModuleAllocationEntity>());

        // ward-zone relationship ok
        _wardRepoMock.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { new WardEntity { Id = 1000, ZoneId = 100, IsActive = true } }.ToList().BuildMock<WardEntity>());

        // existing allocation present - will be detected
        var existing = new List<GlobalSurveyWardAllocationEntity>
        {
            new GlobalSurveyWardAllocationEntity { UserId = 10, DepartmentId = 20, ModuleId = 30, ZoneId = 100, WardId = 1000, IsActive = true }
        };
        _repoMock.Setup(r => r.GetQueryable()).Returns(existing.ToList().BuildMock<GlobalSurveyWardAllocationEntity>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateFlexibleAsync(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task ReplaceAllocationsAsync_UserModuleMismatch_ThrowsArgumentException()
    {
        var updateDto = new UpdateFlexibleWardAllocationDto
        {
            UserId = 11,
            DepartmentId = 21,
            ModuleId = 31,
            Allocations = new List<ZoneWardAllocationDto> { new ZoneWardAllocationDto { ZoneId = 1, WardIds = new List<int> { 1 } } },
            UpdatedBy = 5
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.ReplaceAllocationsAsync(99, 99, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task IsUserDeallocatedAsync_ReturnsTrueAndFalse()
    {
        var users = new List<UserEntity>
        {
            new UserEntity { Id = 1, MarkedForDeletion = true },
            new UserEntity { Id = 2, MarkedForDeletion = false }
        };

        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.ToList().BuildMock<UserEntity>());

        var res1 = await _service.IsUserDeallocatedAsync(1, CancellationToken.None);
        var res2 = await _service.IsUserDeallocatedAsync(2, CancellationToken.None);

        Assert.True(res1);
        Assert.False(res2);
    }
}
