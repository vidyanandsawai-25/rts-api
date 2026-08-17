using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.wardallocation;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class WardAllocationServiceTests
{
    private readonly Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>
        _repoMock;

    private readonly Mock<IRepository<UserEntity, int>>
        _userRepoMock;

    private readonly Mock<IRepository<UserDepartmentAllocationEntity, int>>
        _userDeptAllocRepoMock;

    private readonly Mock<IRepository<UserModuleAllocationEntity, int>>
        _userModuleAllocRepoMock;

    private readonly Mock<IRepository<DepartmentMasterEntity, int>>
        _deptRepoMock;

    private readonly Mock<IRepository<ModuleMasterEntity, int>>
        _moduleRepoMock;

    private readonly Mock<IRepository<ZoneEntity, int>>
        _zoneRepoMock;

    private readonly Mock<IRepository<WardEntity, int>>
        _wardRepoMock;

    private readonly Mock<IRepository<OldWardMasterEntity, int>>
        _oldWardRepoMock;

    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMapper> _mapperMock;

    private readonly WardAllocationService _service;

    public WardAllocationServiceTests()
    {
        _repoMock =
            new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>();

        _userRepoMock =
            new Mock<IRepository<UserEntity, int>>();

        _userDeptAllocRepoMock =
            new Mock<IRepository<UserDepartmentAllocationEntity, int>>();

        _userModuleAllocRepoMock =
            new Mock<IRepository<UserModuleAllocationEntity, int>>();

        _deptRepoMock =
            new Mock<IRepository<DepartmentMasterEntity, int>>();

        _moduleRepoMock =
            new Mock<IRepository<ModuleMasterEntity, int>>();

        _zoneRepoMock =
            new Mock<IRepository<ZoneEntity, int>>();

        _wardRepoMock =
            new Mock<IRepository<WardEntity, int>>();

        _oldWardRepoMock =
            new Mock<IRepository<OldWardMasterEntity, int>>();

        _uowMock =
            new Mock<IUnitOfWork>();

        _mapperMock =
            new Mock<IMapper>();

        _uowMock
            .Setup(x =>
                x.BeginTransactionAsync(
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x =>
                x.CommitTransactionAsync(
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x =>
                x.RollbackTransactionAsync(
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new WardAllocationService(
            _repoMock.Object,
            _userRepoMock.Object,
            _userDeptAllocRepoMock.Object,
            _userModuleAllocRepoMock.Object,
            _deptRepoMock.Object,
            _moduleRepoMock.Object,
            _zoneRepoMock.Object,
            _wardRepoMock.Object,
            _oldWardRepoMock.Object,
            _uowMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task CreateFlexibleAsync_Succeeds_CreatesAllocations()
    {
        // Arrange
        var createDto = new CreateFlexibleWardAllocationDto
        {
            UserId = 10,
            DepartmentId = 20,
            ModuleId = 30,
            Allocations = new List<ZoneWardAllocationDto>
            {
                new()
                {
                    ZoneId = 100,
                    WardIds = new List<int>
                    {
                        1000
                    }
                },
                new()
                {
                    ZoneId = 200,
                    WardIds = new List<int>
                    {
                        2000
                    }
                }
            },
            IsActive = true,
            CreatedBy = 99
        };

        SetupValidUserDepartmentModule(
            userId: 10,
            departmentId: 20,
            moduleId: 30);

        var wards = new List<WardEntity>
        {
            new()
            {
                Id = 1000,
                ZoneId = 100,
                IsActive = true
            },
            new()
            {
                Id = 2000,
                ZoneId = 200,
                IsActive = true
            }
        };

        _wardRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                wards.BuildMock<WardEntity>());

        var oldWards =
            new List<OldWardMasterEntity>();

        _oldWardRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                oldWards.BuildMock<OldWardMasterEntity>());

        var added =
            new List<GlobalSurveyWardAllocationEntity>();

        var nextId = 1;

        _repoMock
            .Setup(x => x.GetQueryable())
            .Returns(() =>
                added.BuildMock<GlobalSurveyWardAllocationEntity>());

        _repoMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<GlobalSurveyWardAllocationEntity>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (
                    GlobalSurveyWardAllocationEntity entity,
                    CancellationToken _) =>
                {
                    entity.Id = nextId++;
                    added.Add(entity);

                    return entity;
                });

        // Act
        var result =
            await _service.CreateFlexibleAsync(
                createDto,
                CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2, added.Count);

        Assert.Contains(
            added,
            x =>
                x.ZoneId == 100 &&
                x.WardId == 1000);

        Assert.Contains(
            added,
            x =>
                x.ZoneId == 200 &&
                x.WardId == 2000);

        _repoMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<GlobalSurveyWardAllocationEntity>(),
                    It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _uowMock.Verify(
            x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateFlexibleAsync_WhenExistingAllocations_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateFlexibleWardAllocationDto
        {
            UserId = 10,
            DepartmentId = 20,
            ModuleId = 30,
            Allocations = new List<ZoneWardAllocationDto>
            {
                new()
                {
                    ZoneId = 100,
                    WardIds = new List<int>
                    {
                        1000
                    }
                }
            },
            IsActive = true,
            CreatedBy = 99
        };

        SetupValidUserDepartmentModule(
            userId: 10,
            departmentId: 20,
            moduleId: 30);

        var wards = new List<WardEntity>
        {
            new()
            {
                Id = 1000,
                ZoneId = 100,
                IsActive = true
            }
        };

        _wardRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                wards.BuildMock<WardEntity>());

        var oldWards =
            new List<OldWardMasterEntity>();

        _oldWardRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                oldWards.BuildMock<OldWardMasterEntity>());

        var existingAllocations =
            new List<GlobalSurveyWardAllocationEntity>
            {
                new()
                {
                    Id = 1,
                    UserId = 10,
                    DepartmentId = 20,
                    ModuleId = 30,
                    ZoneId = 100,
                    WardId = 1000,
                    IsActive = true
                }
            };

        _repoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                existingAllocations
                    .BuildMock<GlobalSurveyWardAllocationEntity>());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                _service.CreateFlexibleAsync(
                    createDto,
                    CancellationToken.None));

        _repoMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<GlobalSurveyWardAllocationEntity>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceAllocationsAsync_UserModuleMismatch_ThrowsArgumentException()
    {
        // Arrange
        var updateDto =
            new UpdateFlexibleWardAllocationDto
            {
                UserId = 11,
                DepartmentId = 21,
                ModuleId = 31,
                Allocations = new List<ZoneWardAllocationDto>
                {
                    new()
                    {
                        ZoneId = 1,
                        WardIds = new List<int>
                        {
                            1
                        }
                    }
                },
                UpdatedBy = 5
            };

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                _service.ReplaceAllocationsAsync(
                    99,
                    99,
                    updateDto,
                    CancellationToken.None));
    }

    [Fact]
    public async Task IsUserDeallocatedAsync_ReturnsTrueAndFalse()
    {
        // Arrange
        var users = new List<UserEntity>
        {
            new()
            {
                Id = 1,
                MarkedForDeletion = true
            },
            new()
            {
                Id = 2,
                MarkedForDeletion = false
            }
        };

        _userRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                users.BuildMock<UserEntity>());

        // Act
        var result1 =
            await _service.IsUserDeallocatedAsync(
                1,
                CancellationToken.None);

        var result2 =
            await _service.IsUserDeallocatedAsync(
                2,
                CancellationToken.None);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
    }

    [Fact]
    public async Task CreateFlexibleAsync_WithValidOldWard_SavesOldWardId()
    {
        // Arrange
        var createDto = new CreateFlexibleWardAllocationDto
        {
            UserId = 10,
            DepartmentId = 20,
            ModuleId = 30,
            Allocations = new List<ZoneWardAllocationDto>
            {
                new()
                {
                    ZoneId = 100,
                    WardIds = new List<int>
                    {
                        1000
                    },
                    OldWardId = 500
                }
            },
            IsActive = true,
            CreatedBy = 99
        };

        SetupValidUserDepartmentModule(
            userId: 10,
            departmentId: 20,
            moduleId: 30);

        var wards = new List<WardEntity>
        {
            new()
            {
                Id = 1000,
                ZoneId = 100,
                IsActive = true
            }
        };

        _wardRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                wards.BuildMock<WardEntity>());

        var oldWards =
            new List<OldWardMasterEntity>
            {
                new()
                {
                    Id = 500,
                    OldWardNo = "10",
                    OldZoneName = "Old Zone A",
                    IsActive = true
                }
            };

        _oldWardRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                oldWards.BuildMock<OldWardMasterEntity>());

        var added =
            new List<GlobalSurveyWardAllocationEntity>();

        _repoMock
            .Setup(x => x.GetQueryable())
            .Returns(() =>
                added.BuildMock<GlobalSurveyWardAllocationEntity>());

        _repoMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<GlobalSurveyWardAllocationEntity>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (
                    GlobalSurveyWardAllocationEntity entity,
                    CancellationToken _) =>
                {
                    entity.Id = 1;
                    added.Add(entity);

                    return entity;
                });

        // Act
        var result =
            await _service.CreateFlexibleAsync(
                createDto,
                CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        Assert.Single(added);

        Assert.Equal(
            500,
            added[0].OldWardId);

        Assert.Equal(
            100,
            added[0].ZoneId);

        Assert.Equal(
            1000,
            added[0].WardId);
    }

    [Fact]
    public async Task CreateFlexibleAsync_WithInvalidOldWard_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new CreateFlexibleWardAllocationDto
        {
            UserId = 10,
            DepartmentId = 20,
            ModuleId = 30,
            Allocations = new List<ZoneWardAllocationDto>
            {
                new()
                {
                    ZoneId = 100,
                    WardIds = new List<int>
                    {
                        1000
                    },
                    OldWardId = 999
                }
            },
            IsActive = true,
            CreatedBy = 99
        };

        SetupValidUserDepartmentModule(
            userId: 10,
            departmentId: 20,
            moduleId: 30);

        var wards = new List<WardEntity>
        {
            new()
            {
                Id = 1000,
                ZoneId = 100,
                IsActive = true
            }
        };

        _wardRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                wards.BuildMock<WardEntity>());

        var oldWards =
            new List<OldWardMasterEntity>();

        _oldWardRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                oldWards.BuildMock<OldWardMasterEntity>());

        _repoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                new List<GlobalSurveyWardAllocationEntity>()
                    .BuildMock<GlobalSurveyWardAllocationEntity>());

        // Act
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    _service.CreateFlexibleAsync(
                        createDto,
                        CancellationToken.None));

        // Assert
        Assert.Contains(
            "999",
            exception.Message);

        _repoMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<GlobalSurveyWardAllocationEntity>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateFlexibleAsync_WithInactiveOldWard_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new CreateFlexibleWardAllocationDto
        {
            UserId = 10,
            DepartmentId = 20,
            ModuleId = 30,
            Allocations = new List<ZoneWardAllocationDto>
            {
                new()
                {
                    ZoneId = 100,
                    WardIds = new List<int>
                    {
                        1000
                    },
                    OldWardId = 500
                }
            },
            IsActive = true,
            CreatedBy = 99
        };

        SetupValidUserDepartmentModule(
            userId: 10,
            departmentId: 20,
            moduleId: 30);

        var wards = new List<WardEntity>
        {
            new()
            {
                Id = 1000,
                ZoneId = 100,
                IsActive = true
            }
        };

        _wardRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                wards.BuildMock<WardEntity>());

        var oldWards =
            new List<OldWardMasterEntity>
            {
                new()
                {
                    Id = 500,
                    OldWardNo = "10",
                    OldZoneName = "Old Zone A",
                    IsActive = false
                }
            };

        _oldWardRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                oldWards.BuildMock<OldWardMasterEntity>());

        _repoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                new List<GlobalSurveyWardAllocationEntity>()
                    .BuildMock<GlobalSurveyWardAllocationEntity>());

        // Act
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    _service.CreateFlexibleAsync(
                        createDto,
                        CancellationToken.None));

        // Assert
        Assert.Contains(
            "500",
            exception.Message);

        _repoMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<GlobalSurveyWardAllocationEntity>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #region Helpers

    private void SetupValidUserDepartmentModule(
        int userId,
        int departmentId,
        int moduleId)
    {
        var users = new List<UserEntity>
        {
            new()
            {
                Id = userId
            }
        };

        _userRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                users.BuildMock<UserEntity>());

        var departments =
            new List<DepartmentMasterEntity>
            {
                new()
                {
                    Id = departmentId,
                    IsActive = true
                }
            };

        _deptRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                departments.BuildMock<DepartmentMasterEntity>());

        var modules =
            new List<ModuleMasterEntity>
            {
                new()
                {
                    Id = moduleId,
                    IsActive = true
                }
            };

        _moduleRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                modules.BuildMock<ModuleMasterEntity>());

        var departmentAllocations =
            new List<UserDepartmentAllocationEntity>
            {
                new()
                {
                    UserId = userId,
                    DepartmentId = departmentId,
                    IsActive = true
                }
            };

        _userDeptAllocRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                departmentAllocations
                    .BuildMock<UserDepartmentAllocationEntity>());

        var moduleAllocations =
            new List<UserModuleAllocationEntity>
            {
                new()
                {
                    UserId = userId,
                    DepartmentId = departmentId,
                    ModuleId = moduleId,
                    IsActive = true
                }
            };

        _userModuleAllocRepoMock
            .Setup(x => x.GetQueryable())
            .Returns(
                moduleAllocations
                    .BuildMock<UserModuleAllocationEntity>());
    }

    #endregion
}