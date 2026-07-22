using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master.UserRoleMaster;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Interfaces;
using Xunit;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Tests.Application;

public class UserRoleServiceTests
{
    private readonly Mock<IRepository<UserRoleMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly UserRoleService _service;

    public UserRoleServiceTests()
    {
        _mockRepository = new Mock<IRepository<UserRoleMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        // Setup SaveChangesAsync
        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Optional transaction setups
        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new UserRoleService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new UserRoleMasterEntity
        {
            Id = 1,
            UserRoleName = "Administrator",
            IsActive = true,
            DepartmentId = 1,
            Department = new DepartmentMasterEntity { Id = 1, DepartmentName = "IT" }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()))
            .Returns(new UserRoleMasterDto
            {
                Id = 1,
                UserRoleName = "Administrator",
                IsActive = true,
                DepartmentId = 1,
                DepartmentName = "IT"
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Administrator", result.UserRoleName);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<UserRoleMasterDto>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRoleMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRoleMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<UserRoleMasterEntity>
        {
            new() 
            { 
                Id = 1, 
                UserRoleName = "Admin", 
                IsActive = true, 
                DepartmentId = 1,
                Department = new DepartmentMasterEntity { Id = 1, DepartmentName = "IT" }
            },
            new() 
            { 
                Id = 2, 
                UserRoleName = "User", 
                IsActive = true, 
                DepartmentId = 1,
                Department = new DepartmentMasterEntity { Id = 1, DepartmentName = "IT" }
            },
            new() 
            { 
                Id = 3, 
                UserRoleName = "Manager", 
                IsActive = false, 
                DepartmentId = 1,
                Department = new DepartmentMasterEntity { Id = 1, DepartmentName = "IT" }
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<UserRoleMasterEntity, UserRoleMasterDto>()
               .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : string.Empty));
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new UserRoleService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new UserRoleMasterQueryParameterDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(items, x => x.Id == 1 && x.UserRoleName == "Admin");
        Assert.Contains(items, x => x.Id == 2 && x.UserRoleName == "User");
        Assert.Contains(items, x => x.Id == 3 && x.UserRoleName == "Manager");
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<UserRoleMasterEntity>
        {
            new() { Id = 1, UserRoleName = "Admin", IsActive = true, DepartmentId = 1 },
            new() { Id = 2, UserRoleName = "User", IsActive = true, DepartmentId = 1 },
            new() { Id = 3, UserRoleName = "Manager", IsActive = false, DepartmentId = 1 }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<UserRoleMasterEntity, UserRoleMasterDto>()
               .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : string.Empty));
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new UserRoleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new UserRoleMasterQueryParameterDto
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_WithUserRoleNameFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<UserRoleMasterEntity>
        {
            new() { Id = 1, UserRoleName = "Admin", IsActive = true, DepartmentId = 1 },
            new() { Id = 2, UserRoleName = "User", IsActive = true, DepartmentId = 1 },
            new() { Id = 3, UserRoleName = "Administrator", IsActive = true, DepartmentId = 1 }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<UserRoleMasterEntity, UserRoleMasterDto>()
               .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : string.Empty));
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new UserRoleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new UserRoleMasterQueryParameterDto
        {
            PageNumber = 1,
            PageSize = 10,
            UserRoleName = "Admin"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => 
            Assert.Contains("Admin", item.UserRoleName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<UserRoleMasterEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<UserRoleMasterEntity, UserRoleMasterDto>()
               .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : string.Empty));
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new UserRoleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new UserRoleMasterQueryParameterDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25)
            .Select(i => new UserRoleMasterEntity
            {
                Id = i,
                UserRoleName = $"Role{i}",
                IsActive = true,
                DepartmentId = 1
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<UserRoleMasterEntity, UserRoleMasterDto>()
               .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : string.Empty));
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new UserRoleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new UserRoleMasterQueryParameterDto
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateUserRoleMasterDto
        {
            UserRoleName = "New Role",
            IsActive = true,
            DepartmentId = 1
        };

        _mockMapper
            .Setup(m => m.Map<UserRoleMasterEntity>(It.IsAny<CreateUserRoleMasterDto>()))
            .Returns((CreateUserRoleMasterDto dto) => new UserRoleMasterEntity
            {
                UserRoleName = dto.UserRoleName,
                IsActive = dto.IsActive,
                DepartmentId = dto.DepartmentId
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<UserRoleMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRoleMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()))
            .Returns((UserRoleMasterEntity e) => new UserRoleMasterDto
            {
                Id = e.Id,
                UserRoleName = e.UserRoleName,
                IsActive = e.IsActive,
                DepartmentId = e.DepartmentId,
                DepartmentName = "IT"
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("New Role", result.UserRoleName);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<UserRoleMasterEntity>(e => e.UserRoleName == "New Role"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveRole_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateUserRoleMasterDto
        {
            UserRoleName = "Inactive Role",
            IsActive = false,
            DepartmentId = 1
        };

        _mockMapper
            .Setup(m => m.Map<UserRoleMasterEntity>(It.IsAny<CreateUserRoleMasterDto>()))
            .Returns(new UserRoleMasterEntity
            {
                Id = 0,
                UserRoleName = "Inactive Role",
                IsActive = false,
                DepartmentId = 1
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<UserRoleMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRoleMasterEntity e, CancellationToken _) =>
            {
                e.Id = 2;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()))
            .Returns(new UserRoleMasterDto
            {
                Id = 2,
                UserRoleName = "Inactive Role",
                IsActive = false,
                DepartmentId = 1,
                DepartmentName = "IT"
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateRoleName_ThrowsException()
    {
        // Arrange
        var createDto = new CreateUserRoleMasterDto
        {
            UserRoleName = "Admin",
            IsActive = true,
            DepartmentId = 1
        };

        _mockMapper
            .Setup(m => m.Map<UserRoleMasterEntity>(It.IsAny<CreateUserRoleMasterDto>()))
            .Returns(new UserRoleMasterEntity { UserRoleName = "Admin", DepartmentId = 1 });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<UserRoleMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate UserRoleName: 'Admin' already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateUserRoleMasterDto
        {
            UserRoleName = "Updated Role Name",
            IsActive = true,
            DepartmentId = 1
        };

        var existingEntity = new UserRoleMasterEntity
        {
            Id = 1,
            UserRoleName = "Old Role Name",
            IsActive = true,
            DepartmentId = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UserRoleMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateUserRoleMasterDto>(), It.IsAny<UserRoleMasterEntity>()))
            .Callback((UpdateUserRoleMasterDto src, UserRoleMasterEntity dest) =>
            {
                dest.UserRoleName = src.UserRoleName;
                dest.IsActive = src.IsActive;
                dest.DepartmentId = src.DepartmentId;
            });

        _mockMapper
            .Setup(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()))
            .Returns((UserRoleMasterEntity e) => new UserRoleMasterDto
            {
                Id = e.Id,
                UserRoleName = e.UserRoleName,
                IsActive = e.IsActive,
                DepartmentId = e.DepartmentId,
                DepartmentName = "IT"
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Role Name", result.UserRoleName);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ChangeIsActive_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateUserRoleMasterDto
        {
            UserRoleName = "Test Role",
            IsActive = false,
            DepartmentId = 1
        };

        var existingEntity = new UserRoleMasterEntity
        {
            Id = 1,
            UserRoleName = "Test Role",
            IsActive = true,
            DepartmentId = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UserRoleMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateUserRoleMasterDto>(), It.IsAny<UserRoleMasterEntity>()))
            .Callback((UpdateUserRoleMasterDto src, UserRoleMasterEntity dest) =>
            {
                dest.IsActive = src.IsActive;
                dest.DepartmentId = src.DepartmentId;
            });

        _mockMapper
            .Setup(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()))
            .Returns(new UserRoleMasterDto
            {
                Id = 1,
                UserRoleName = "Test Role",
                IsActive = false,
                DepartmentId = 1,
                DepartmentName = "IT"
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()  //  Correct
    {
        // Arrange
        var updateDto = new UpdateUserRoleMasterDto
        {
            UserRoleName = "Updated Role",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRoleMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);
        
        // Assert
        Assert.Null(result);  //  Expects null, not exception
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new UserRoleMasterEntity
        {
            Id = idToDelete,
            UserRoleName = "Role To Delete",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<UserRoleMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<UserRoleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()  //  Correct
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRoleMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);
        
        Assert.False(result);  //  Expects false, not exception
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task DeleteAsync_InvalidId_ReturnsFalse(int invalidId)  //  Correct
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRoleMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(invalidId, CancellationToken.None);
        
        Assert.False(result);  // Expects false, not exception
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var createDto = new CreateUserRoleMasterDto
        {
            UserRoleName = "Test Role",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<UserRoleMasterEntity>(It.IsAny<CreateUserRoleMasterDto>()))
            .Returns(new UserRoleMasterEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<UserRoleMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRoleMasterEntity { Id = 1 });

        _mockMapper
            .Setup(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()))
            .Returns(new UserRoleMasterDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var existingEntity = new UserRoleMasterEntity
        {
            Id = 1,
            UserRoleName = "Old Name"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserRoleMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()))
            .Returns(new UserRoleMasterDto());

        var updateDto = new UpdateUserRoleMasterDto { UserRoleName = "New Name" };

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Mapper Tests

    [Fact]
    public async Task GetByIdAsync_VerifiesMapperCalledOnce()
    {
        // Arrange
        var entity = new UserRoleMasterEntity
        {
            Id = 1,
            UserRoleName = "Test"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()))
            .Returns(new UserRoleMasterDto());

        // Act
        await _service.GetByIdAsync(1);

        // Assert
        _mockMapper.Verify(m => m.Map<UserRoleMasterDto>(entity), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_VerifiesMapperCalledTwice()
    {
        // Arrange
        var createDto = new CreateUserRoleMasterDto { UserRoleName = "Test" };

        _mockMapper.Setup(m => m.Map<UserRoleMasterEntity>(It.IsAny<CreateUserRoleMasterDto>()))
            .Returns(new UserRoleMasterEntity());

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<UserRoleMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRoleMasterEntity { Id = 1 });

        _mockMapper.Setup(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()))
            .Returns(new UserRoleMasterDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockMapper.Verify(m => m.Map<UserRoleMasterEntity>(createDto), Times.Once);
        _mockMapper.Verify(m => m.Map<UserRoleMasterDto>(It.IsAny<UserRoleMasterEntity>()), Times.Once);
    }

    #endregion
}
