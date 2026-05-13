using AutoMapper;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Email;
using NtisPlatform.Application.DTOs.Master.UserMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for UserService
/// Tests CRUD operations, password handling, and user allocations management
/// Updated to use UserEntity and UserDto (not UserEntity/UserMasterDto)
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IRepository<UserEntity, int>> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IRepository<UserDepartmentAllocationEntity, int>> _departmentMapRepositoryMock;
    private readonly Mock<IRepository<UserModuleAllocationEntity, int>> _moduleAccessRepositoryMock;
    private readonly Mock<IRepository<UserRoleAllocationEntity, int>> _roleAllocationRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IPasswordGeneratorService> _passwordGeneratorMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock;
    private readonly Mock<IEmailSettingsProvider> _emailSettingsProviderMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IRepository<UserEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _departmentMapRepositoryMock = new Mock<IRepository<UserDepartmentAllocationEntity, int>>();
        _moduleAccessRepositoryMock = new Mock<IRepository<UserModuleAllocationEntity, int>>();
        _roleAllocationRepositoryMock = new Mock<IRepository<UserRoleAllocationEntity, int>>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _passwordGeneratorMock = new Mock<IPasswordGeneratorService>();
        _emailServiceMock = new Mock<IEmailService>();
        _emailTemplateServiceMock = new Mock<IEmailTemplateService>();
        _emailSettingsProviderMock = new Mock<IEmailSettingsProvider>();

        _userService = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _departmentMapRepositoryMock.Object,
            _moduleAccessRepositoryMock.Object,
            _roleAllocationRepositoryMock.Object,
            _passwordHasherMock.Object,
            _passwordGeneratorMock.Object,
            _emailServiceMock.Object,
            _emailTemplateServiceMock.Object,
            _emailSettingsProviderMock.Object
        );
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesUserSuccessfully()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            UserCode = "TEST001",
            Email = "test@example.com",
            MobileNo = "1234567890",
            CreatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>
            {
                new() { DepartmentId = 1, IsActive = true }
            },
            ModuleAccess = new List<UserModuleAllocationCreateDto>
            {
                new() { DepartmentId = 1, ModuleId = 1, IsActive = true }
            },
            RoleAllocations = new List<UserRoleAllocationCreateDto>
            {
                new() { DepartmentId = 1, UserRoleId = 1, IsActive = true }
            }
        };

        var userEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            UserCode = "TEST001",
            PasswordHash = "$2a$12$hashedpassword",
            MustChangePassword = true,
            IsActive = true
        };

        var userDto = new UserDto
        {
            Id = 1,
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            UserCode = "TEST001",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        // Setup mocks
        _passwordGeneratorMock.Setup(p => p.Generate()).Returns("TempPass123!");
        _passwordHasherMock.Setup(p => p.HashPassword("TempPass123!")).Returns("$2a$12$hashedpassword");
        _mapperMock.Setup(m => m.Map<UserEntity>(createDto)).Returns(userEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(userEntity)).Returns(userDto);
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        // Setup mapper for CreateDto -> Entity mappings (used in Save*AllocationsAsync methods)
        _mapperMock.Setup(m => m.Map<UserDepartmentAllocationEntity>(It.IsAny<UserDepartmentAllocationCreateDto>()))
            .Returns((UserDepartmentAllocationCreateDto dto) => new UserDepartmentAllocationEntity 
            { 
                DepartmentId = dto.DepartmentId,
                IsActive = dto.IsActive 
            });
        _mapperMock.Setup(m => m.Map<UserModuleAllocationEntity>(It.IsAny<UserModuleAllocationCreateDto>()))
            .Returns((UserModuleAllocationCreateDto dto) => new UserModuleAllocationEntity 
            { 
                DepartmentId = dto.DepartmentId,
                ModuleId = dto.ModuleId,
                IsActive = dto.IsActive 
            });
        _mapperMock.Setup(m => m.Map<UserRoleAllocationEntity>(It.IsAny<UserRoleAllocationCreateDto>()))
            .Returns((UserRoleAllocationCreateDto dto) => new UserRoleAllocationEntity 
            { 
                DepartmentId = dto.DepartmentId,
                UserRoleId = dto.UserRoleId,
                IsActive = dto.IsActive 
            });

        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity e, CancellationToken ct) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        _departmentMapRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDepartmentAllocationEntity e, CancellationToken ct) => e);
        _moduleAccessRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserModuleAllocationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserModuleAllocationEntity e, CancellationToken ct) => e);
        _roleAllocationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserRoleAllocationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRoleAllocationEntity e, CancellationToken ct) => e);

        // Act
        var result = await _userService.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("testuser", result.UserName);

        // Verify password was generated and hashed
        _passwordGeneratorMock.Verify(p => p.Generate(), Times.Once);
        _passwordHasherMock.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Once);

        // Verify user was added
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify allocations were saved
        _departmentMapRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _moduleAccessRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserModuleAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _roleAllocationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserRoleAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify save was called exactly once - atomic transaction for entire aggregate
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "existinguser",
            CreatedBy = 1
        };

        var existingUsers = new List<UserEntity>
        {
            new() { Id = 999, UserName = "existinguser" }
        }.BuildMock();

        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(existingUsers);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateAsync(createDto));
        Assert.Contains("Username 'existinguser' is already taken", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateUserCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            UserCode = "CODE001",
            CreatedBy = 1
        };

        var existingUsers = new List<UserEntity>
        {
            new() { Id = 999, UserName = "otheruser", UserCode = "CODE001" }
        }.BuildMock();

        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(existingUsers);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateAsync(createDto));
        Assert.Contains("UserCode 'CODE001' already exists", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithoutAllocations_CreatesUserWithoutAllocations()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            CreatedBy = 1,
            Departments = null,
            ModuleAccess = null,
            RoleAllocations = null
        };

        var userEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$12$hashedpassword"
        };

        var userDto = new UserDto
        {
            Id = 1,
            UserName = "testuser",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _passwordGeneratorMock.Setup(p => p.Generate()).Returns("TempPass123!");
        _passwordHasherMock.Setup(p => p.HashPassword("TempPass123!")).Returns("$2a$12$hashedpassword");
        _mapperMock.Setup(m => m.Map<UserEntity>(createDto)).Returns(userEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(userEntity)).Returns(userDto);
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity e, CancellationToken ct) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        // Act
        var result = await _userService.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);

        // Verify no allocations were added
        _departmentMapRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _moduleAccessRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserModuleAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _roleAllocationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserRoleAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesUserSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            UserName = "updateduser",
            FirstName = "Updated",
            LastName = "User",
            UserCode = "UPD001",
            UpdatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>(),
            ModuleAccess = new List<UserModuleAllocationCreateDto>(),
            RoleAllocations = new List<UserRoleAllocationCreateDto>()
        };

        var existingEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-10)
        };

        var updatedDto = new UserDto
        {
            Id = 1,
            UserName = "updateduser",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity)).Returns(existingEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(existingEntity)).Returns(updatedDto);
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Setup allocation repositories
        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        // Act
        var result = await _userService.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("updateduser", result.UserName);

        // Verify update was called
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            UserName = "nonexistent",
            UpdatedBy = 1
        };

        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _userRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        // Act
        var result = await _userService.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            UserName = "existinguser",
            UpdatedBy = 1
        };

        var existingEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-10)
        };

        var otherUser = new UserEntity
        {
            Id = 2,
            UserName = "existinguser"
        };

        var users = new List<UserEntity> { existingEntity, otherUser }.BuildMock();

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(users);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.UpdateAsync(1, updateDto));
        Assert.Contains("Username 'existinguser' is already taken", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_PreservesAuditFields()
    {
        // Arrange
        var originalCreatedBy = 99;
        var originalCreatedDate = DateTime.Now.AddMonths(-6);

        var updateDto = new UpdateUserDto
        {
            UserName = "updateduser",
            UpdatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>(),
            ModuleAccess = new List<UserModuleAllocationCreateDto>(),
            RoleAllocations = new List<UserRoleAllocationCreateDto>()
        };

        var existingEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            CreatedBy = originalCreatedBy,
            CreatedDate = originalCreatedDate
        };

        var updatedDto = new UserDto
        {
            Id = 1,
            UserName = "updateduser",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity)).Returns(existingEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(existingEntity)).Returns(updatedDto);
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        // Act
        await _userService.UpdateAsync(1, updateDto);

        // Verify audit fields are preserved
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<UserEntity>(e => 
            e.CreatedBy == originalCreatedBy && 
            e.CreatedDate == originalCreatedDate &&
            e.UpdatedBy == 1
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_PreservesMustChangePassword_WhenTrue()
    {
        // Arrange - User forced to change password (e.g., after reset)
        var updateDto = new UpdateUserDto
        {
            UserName = "updateduser",
            FirstName = "Updated",
            UpdatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>(),
            ModuleAccess = new List<UserModuleAllocationCreateDto>(),
            RoleAllocations = new List<UserRoleAllocationCreateDto>()
        };

        var existingEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            MustChangePassword = true, // Flag set by password reset
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-10)
        };

        var updatedDto = new UserDto
        {
            Id = 1,
            UserName = "updateduser",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity)).Returns(existingEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(existingEntity)).Returns(updatedDto);
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        // Act
        await _userService.UpdateAsync(1, updateDto);

        // Assert - CRITICAL: MustChangePassword flag must be preserved
        // This validates the security fix that prevents bypassing forced password changes
        Assert.True(existingEntity.MustChangePassword);

        // Verify the entity was updated with the flag still set
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<UserEntity>(e => 
            e.MustChangePassword == true
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_PreservesMustChangePassword_WhenFalse()
    {
        // Arrange - User who has already changed their password
        var updateDto = new UpdateUserDto
        {
            UserName = "updateduser",
            FirstName = "Updated",
            UpdatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>(),
            ModuleAccess = new List<UserModuleAllocationCreateDto>(),
            RoleAllocations = new List<UserRoleAllocationCreateDto>()
        };

        var existingEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            MustChangePassword = false, // User already changed password
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-10)
        };

        var updatedDto = new UserDto
        {
            Id = 1,
            UserName = "updateduser",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity)).Returns(existingEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(existingEntity)).Returns(updatedDto);
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        // Act
        await _userService.UpdateAsync(1, updateDto);

        // Assert - Flag should remain false (unchanged)
        Assert.False(existingEntity.MustChangePassword);

        // Verify the entity was updated with the flag still false
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<UserEntity>(e => 
            e.MustChangePassword == false
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingUser_DeletesUserAndAllocations()
    {
        // Arrange
        var userId = 1;
        var existingEntity = new UserEntity
        {
            Id = userId,
            UserName = "testuser"
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup allocation deletions
        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>
            {
                new() { Id = 1, UserId = userId, IsActive = true }
            }.BuildMock());

        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>
            {
                new() { Id = 2, UserId = userId, IsActive = true }
            }.BuildMock());

        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>
            {
                new() { Id = 3, UserId = userId, IsActive = true }
            }.BuildMock());

        _departmentMapRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _moduleAccessRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserModuleAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roleAllocationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserRoleAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _userService.DeleteAsync(userId);

        // Assert
        Assert.True(result);

        // Verify allocations were deactivated (not deleted)
        _departmentMapRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _moduleAccessRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserModuleAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _roleAllocationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserRoleAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify user was deleted
        _userRepositoryMock.Verify(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()), Times.Once);

        // Verify save was called
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        var userId = 999;

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        // Act
        var result = await _userService.DeleteAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WithMultipleAllocations_DeletesAllAllocations()
    {
        // Arrange
        var userId = 1;
        var existingEntity = new UserEntity { Id = userId, UserName = "testuser" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup multiple allocations
        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>
            {
                new() { Id = 1, UserId = userId, IsActive = true },
                new() { Id = 2, UserId = userId, IsActive = true },
                new() { Id = 3, UserId = userId, IsActive = true }
            }.BuildMock());

        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>
            {
                new() { Id = 4, UserId = userId, IsActive = true },
                new() { Id = 5, UserId = userId, IsActive = true }
            }.BuildMock());

        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>
            {
                new() { Id = 6, UserId = userId, IsActive = true }
            }.BuildMock());

        _departmentMapRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _moduleAccessRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserModuleAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roleAllocationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserRoleAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _userService.DeleteAsync(userId);

        // Assert
        Assert.True(result);

        // Verify all department allocations were deactivated (3 times)
        _departmentMapRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

        // Verify all module allocations were deactivated (2 times)
        _moduleAccessRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserModuleAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // Verify all role allocations were deactivated (1 time)
        _roleAllocationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserRoleAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithDeleteUserDto_SetsUpdatedByInAllocationHistory()
    {
        // Arrange - This test validates the audit trail fix
        var userId = 1;
        var deletedBy = 42; // Admin user ID performing the deletion
        var deleteDto = new DeleteUserDto { UpdatedBy = deletedBy };

        var existingEntity = new UserEntity { Id = userId, UserName = "testuser" };

        var originalCreatedBy = 10;
        var originalCreatedDate = DateTime.Now.AddMonths(-3);

        var departmentAllocation = new UserDepartmentAllocationEntity 
        { 
            Id = 1, 
            UserId = userId, 
            DepartmentId = 1,
            IsActive = true, 
            CreatedBy = originalCreatedBy, 
            CreatedDate = originalCreatedDate 
        };

        var moduleAllocation = new UserModuleAllocationEntity 
        { 
            Id = 2, 
            UserId = userId, 
            DepartmentId = 1,
            ModuleId = 1,
            IsActive = true, 
            CreatedBy = originalCreatedBy, 
            CreatedDate = originalCreatedDate 
        };

        var roleAllocation = new UserRoleAllocationEntity 
        { 
            Id = 3, 
            UserId = userId, 
            DepartmentId = 1,
            UserRoleId = 1,
            IsActive = true, 
            CreatedBy = originalCreatedBy, 
            CreatedDate = originalCreatedDate 
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity> { departmentAllocation }.BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity> { moduleAllocation }.BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity> { roleAllocation }.BuildMock());

        _departmentMapRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _moduleAccessRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserModuleAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roleAllocationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserRoleAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _userService.DeleteAsync(userId, deleteDto);

        // Assert
        Assert.True(result);

        // CRITICAL: Verify UpdatedBy is set to track who performed the deletion
        Assert.Equal(deletedBy, departmentAllocation.UpdatedBy);
        Assert.Equal(deletedBy, moduleAllocation.UpdatedBy);
        Assert.Equal(deletedBy, roleAllocation.UpdatedBy);

        // Verify IsActive set to false
        Assert.False(departmentAllocation.IsActive);
        Assert.False(moduleAllocation.IsActive);
        Assert.False(roleAllocation.IsActive);

        // Verify UpdatedDate stamped
        Assert.NotNull(departmentAllocation.UpdatedDate);
        Assert.NotNull(moduleAllocation.UpdatedDate);
        Assert.NotNull(roleAllocation.UpdatedDate);

        // Verify original audit trail preserved (CreatedBy/CreatedDate unchanged)
        Assert.Equal(originalCreatedBy, departmentAllocation.CreatedBy);
        Assert.Equal(originalCreatedDate, departmentAllocation.CreatedDate);
        Assert.Equal(originalCreatedBy, moduleAllocation.CreatedBy);
        Assert.Equal(originalCreatedDate, moduleAllocation.CreatedDate);
        Assert.Equal(originalCreatedBy, roleAllocation.CreatedBy);
        Assert.Equal(originalCreatedDate, roleAllocation.CreatedDate);

        // Verify all allocations updated
        _departmentMapRepositoryMock.Verify(r => r.UpdateAsync(departmentAllocation, It.IsAny<CancellationToken>()), Times.Once);
        _moduleAccessRepositoryMock.Verify(r => r.UpdateAsync(moduleAllocation, It.IsAny<CancellationToken>()), Times.Once);
        _roleAllocationRepositoryMock.Verify(r => r.UpdateAsync(roleAllocation, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Transactional Rollback Tests

    [Fact]
    public async Task CreateAsync_WhenSaveChangesFails_RollsBackEntireAggregate()
    {
        // Arrange - Test that if SaveChanges fails, nothing is persisted (atomic transaction)
        // With the new single-SaveChanges approach, if the save fails, the entire aggregate
        // (user + allocations) is rolled back automatically by the database transaction.
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CreatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>
            {
                new() { DepartmentId = 1, IsActive = true }
            }
        };

        var userEntity = new UserEntity
        {
            Id = 0, // Not yet persisted
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "$2a$12$hashedpassword",
            MustChangePassword = true,
            IsActive = true
        };

        _passwordGeneratorMock.Setup(p => p.Generate()).Returns("TempPass123!");
        _passwordHasherMock.Setup(p => p.HashPassword("TempPass123!")).Returns("$2a$12$hashedpassword");
        _mapperMock.Setup(m => m.Map<UserEntity>(createDto)).Returns(userEntity);
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity e, CancellationToken ct) => e);

        // SaveChanges fails - simulating database constraint violation or network error
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error during save - transaction will be rolled back"));

        _mapperMock.Setup(m => m.Map<UserDepartmentAllocationEntity>(It.IsAny<UserDepartmentAllocationCreateDto>()))
            .Returns(new UserDepartmentAllocationEntity { DepartmentId = 1, IsActive = true });
        _departmentMapRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDepartmentAllocationEntity e, CancellationToken ct) => e);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateAsync(createDto));

        // Verify SaveChanges was called exactly once (atomic operation)
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // The entire aggregate is rolled back - no partial state in database
        // In a real scenario with a real DbContext, neither user nor allocations would be persisted
    }

    [Fact]
    public async Task CreateAsync_WhenMapperReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange - Test defensive coding against mapper failures
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            CreatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>
            {
                new() { DepartmentId = 1, IsActive = true }
            }
        };

        var userEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            PasswordHash = "$2a$12$hashedpassword",
            MustChangePassword = true,
            IsActive = true
        };

        _passwordGeneratorMock.Setup(p => p.Generate()).Returns("TempPass123!");
        _passwordHasherMock.Setup(p => p.HashPassword("TempPass123!")).Returns("$2a$12$hashedpassword");
        _mapperMock.Setup(m => m.Map<UserEntity>(createDto)).Returns(userEntity);
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity e, CancellationToken ct) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Mapper returns null for allocation - this should throw InvalidOperationException with clear message
        _mapperMock.Setup(m => m.Map<UserDepartmentAllocationEntity>(It.IsAny<UserDepartmentAllocationCreateDto>()))
            .Returns((UserDepartmentAllocationEntity)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateAsync(createDto));
        Assert.Contains("AutoMapper returned null", exception.Message);
        Assert.Contains("UserDepartmentAllocationEntity", exception.Message);
        Assert.Contains("Check mapping configuration", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenMapperReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange - Test defensive coding for mapper failures in update/patch path
        var updateDto = new UpdateUserDto
        {
            UserName = "updateduser",
            UpdatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>
            {
                new() { DepartmentId = 99, IsActive = true } // New department not in existing
            }
        };

        var existingEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-10)
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity)).Returns(existingEntity);
        _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Existing allocations (empty - so new one will be added)
        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        // Mapper returns null when trying to add new allocation - should throw InvalidOperationException
        _mapperMock.Setup(m => m.Map<UserDepartmentAllocationEntity>(It.IsAny<UserDepartmentAllocationCreateDto>()))
            .Returns((UserDepartmentAllocationEntity)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.UpdateAsync(1, updateDto));
        Assert.Contains("AutoMapper returned null", exception.Message);
        Assert.Contains("UserDepartmentAllocationEntity", exception.Message);
        Assert.Contains("Check mapping configuration", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenSaveChangesFails_PropagatesException()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            UserName = "updateduser",
            FirstName = "Updated",
            UpdatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>()
        };

        var existingEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-10)
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity)).Returns(existingEntity);
        _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        // SaveChanges fails
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection lost"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.UpdateAsync(1, updateDto));
    }

    #endregion

    #region Password Security Tests

    [Fact]
    public async Task CreateAsync_AutoGeneratesPasswordAndForcesChange()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "newuser",
            FirstName = "New",
            LastName = "User",
            CreatedBy = 1
        };

        var generatedPassword = "AutoGen123!";
        var hashedPassword = "$2a$12$hashedAutoGen123";

        _passwordGeneratorMock.Setup(p => p.Generate()).Returns(generatedPassword);
        _passwordHasherMock.Setup(p => p.HashPassword(generatedPassword)).Returns(hashedPassword);

        var userEntity = new UserEntity();
        _mapperMock.Setup(m => m.Map<UserEntity>(createDto)).Returns(userEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<UserEntity>())).Returns(new UserDto { Id = 1, UserName = "newuser" });
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity e, CancellationToken ct) =>
            {
                e.Id = 1;
                return e;
            });

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _userService.CreateAsync(createDto);

        // Assert
        Assert.Equal(hashedPassword, userEntity.PasswordHash);
        Assert.True(userEntity.MustChangePassword);
        _passwordGeneratorMock.Verify(p => p.Generate(), Times.Once);
        _passwordHasherMock.Verify(p => p.HashPassword(generatedPassword), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_AutoGeneratesPasswordAndForcesChange()
    {
        // Arrange
        var resetDto = new ResetPasswordDto { UpdatedBy = 1 };
        var userId = 1;

        var existingEntity = new UserEntity
        {
            Id = userId,
            UserName = "testuser",
            PasswordHash = "$2a$12$oldHash",
            MustChangePassword = false
        };

        var generatedPassword = "ResetPass123!";
        var hashedPassword = "$2a$12$newHashedPassword";

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _passwordGeneratorMock.Setup(p => p.Generate()).Returns(generatedPassword);
        _passwordHasherMock.Setup(p => p.HashPassword(generatedPassword)).Returns(hashedPassword);
        _mapperMock.Setup(m => m.Map<UserSecurityStatusDto>(It.IsAny<UserEntity>()))
            .Returns(new UserSecurityStatusDto { Id = userId, MustChangePassword = true });
        _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _userService.ResetPasswordAsync(userId, resetDto);

        // Assert
        Assert.Equal(hashedPassword, existingEntity.PasswordHash);
        Assert.True(existingEntity.MustChangePassword);
        _passwordGeneratorMock.Verify(p => p.Generate(), Times.Once);
        _passwordHasherMock.Verify(p => p.HashPassword(generatedPassword), Times.Once);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task UpdateAsync_PreservesCreatedByAndCreatedDate()
    {
        // Arrange
        var originalCreatedBy = 99;
        var originalCreatedDate = DateTime.Now.AddMonths(-6);

        var updateDto = new UpdateUserDto
        {
            UserName = "updateduser",
            UpdatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>()
        };

        var existingEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            CreatedBy = originalCreatedBy,
            CreatedDate = originalCreatedDate
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity)).Returns(existingEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<UserEntity>())).Returns(new UserDto { Id = 1 });
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _userService.UpdateAsync(1, updateDto);

        // Assert
        Assert.Equal(originalCreatedBy, existingEntity.CreatedBy);
        Assert.Equal(originalCreatedDate, existingEntity.CreatedDate);
        Assert.NotNull(existingEntity.UpdatedDate);
        Assert.Equal(1, existingEntity.UpdatedBy);
    }

    [Fact]
    public async Task DeleteAsync_DeactivatesAllAllocationTypesAndPreservesAuditTrail()
    {
        // Arrange
        var userId = 1;
        var existingEntity = new UserEntity { Id = userId, UserName = "testuser" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        // Setup multiple allocations of each type to verify comprehensive deactivation
        var originalCreatedDate = DateTime.Now.AddMonths(-3);
        var originalCreatedBy = 5;

        var departmentAllocations = new List<UserDepartmentAllocationEntity>
        {
            new() { Id = 1, UserId = userId, DepartmentId = 1, IsActive = true, CreatedBy = originalCreatedBy, CreatedDate = originalCreatedDate },
            new() { Id = 2, UserId = userId, DepartmentId = 2, IsActive = true, CreatedBy = originalCreatedBy, CreatedDate = originalCreatedDate },
            new() { Id = 3, UserId = userId, DepartmentId = 3, IsActive = false, CreatedBy = originalCreatedBy, CreatedDate = originalCreatedDate } // Already inactive
        };

        var moduleAllocations = new List<UserModuleAllocationEntity>
        {
            new() { Id = 1, UserId = userId, DepartmentId = 1, ModuleId = 1, IsActive = true, CreatedBy = originalCreatedBy, CreatedDate = originalCreatedDate },
            new() { Id = 2, UserId = userId, DepartmentId = 1, ModuleId = 2, IsActive = true, CreatedBy = originalCreatedBy, CreatedDate = originalCreatedDate }
        };

        var roleAllocations = new List<UserRoleAllocationEntity>
        {
            new() { Id = 1, UserId = userId, DepartmentId = 1, UserRoleId = 1, IsActive = true, CreatedBy = originalCreatedBy, CreatedDate = originalCreatedDate },
            new() { Id = 2, UserId = userId, DepartmentId = 2, UserRoleId = 2, IsActive = true, CreatedBy = originalCreatedBy, CreatedDate = originalCreatedDate }
        };

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(departmentAllocations.BuildMock());
        _departmentMapRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(moduleAllocations.BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserModuleAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(roleAllocations.BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserRoleAllocationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock.Setup(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _userService.DeleteAsync(userId);

        // Assert - Verify all active allocations deactivated across all types
        Assert.False(departmentAllocations[0].IsActive);
        Assert.False(departmentAllocations[1].IsActive);
        Assert.False(departmentAllocations[2].IsActive); // Already inactive, should remain so

        Assert.False(moduleAllocations[0].IsActive);
        Assert.False(moduleAllocations[1].IsActive);

        Assert.False(roleAllocations[0].IsActive);
        Assert.False(roleAllocations[1].IsActive);

        // Verify UpdatedDate stamped on all deactivated allocations
        Assert.NotNull(departmentAllocations[0].UpdatedDate);
        Assert.NotNull(departmentAllocations[1].UpdatedDate);
        Assert.NotNull(moduleAllocations[0].UpdatedDate);
        Assert.NotNull(moduleAllocations[1].UpdatedDate);
        Assert.NotNull(roleAllocations[0].UpdatedDate);
        Assert.NotNull(roleAllocations[1].UpdatedDate);

        // Verify audit trail preserved (CreatedBy and CreatedDate not overwritten)
        Assert.Equal(originalCreatedBy, departmentAllocations[0].CreatedBy);
        Assert.Equal(originalCreatedDate, departmentAllocations[0].CreatedDate);
        Assert.Equal(originalCreatedBy, moduleAllocations[0].CreatedBy);
        Assert.Equal(originalCreatedDate, moduleAllocations[0].CreatedDate);
        Assert.Equal(originalCreatedBy, roleAllocations[0].CreatedBy);
        Assert.Equal(originalCreatedDate, roleAllocations[0].CreatedDate);

        // Verify UpdateAsync called only for active allocations (2 depts + 2 modules + 2 roles = 6 times)
        _departmentMapRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserDepartmentAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _moduleAccessRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserModuleAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _roleAllocationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserRoleAllocationEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // Verify user deletion called after allocations deactivated
        _userRepositoryMock.Verify(r => r.DeleteAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Email Tests

    [Fact]
    public async Task CreateAsync_WithValidEmail_SendsWelcomeEmail()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            CreatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>(),
            ModuleAccess = new List<UserModuleAllocationCreateDto>(),
            RoleAllocations = new List<UserRoleAllocationCreateDto>()
        };

        var userEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            PasswordHash = "$2a$12$hashedpassword",
            MustChangePassword = true
        };

        var userDto = new UserDto
        {
            Id = 1,
            UserName = "testuser",
            Email = "test@example.com",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        var emailSettings = new EmailSettingsDto
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            SmtpUserName = "test",
            SmtpPassword = "pass",
            FromEmail = "noreply@test.com",
            FromName = "Test",
            SecureSocketOptions = "Auto",
            LoginUrl = "https://app.test.com/login"
        };

        // Setup mocks
        _passwordGeneratorMock.Setup(p => p.Generate()).Returns("TempPass123!");
        _passwordHasherMock.Setup(p => p.HashPassword("TempPass123!")).Returns("$2a$12$hashedpassword");
        _mapperMock.Setup(m => m.Map<UserEntity>(createDto)).Returns(userEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(userEntity)).Returns(userDto);
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity e, CancellationToken ct) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        _emailSettingsProviderMock.Setup(e => e.GetEmailSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailSettings);
        _emailTemplateServiceMock.Setup(e => e.GetTemplateAsync(
            "WelcomeEmail",
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>Welcome Email Body</html>");
        _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userService.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);

        // Verify email was sent
        _emailSettingsProviderMock.Verify(e => e.GetEmailSettingsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailTemplateServiceMock.Verify(e => e.GetTemplateAsync(
            "WelcomeEmail",
            It.Is<Dictionary<string, string>>(d =>
                d["UserName"] == "testuser" &&
                d["Email"] == "test@example.com" &&
                d["TemporaryPassword"] == "TempPass123!" &&
                d["LoginUrl"] == "https://app.test.com/login"),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(e => e.SendEmailAsync(
            It.Is<EmailRequest>(r =>
                r.ToEmail == "test@example.com" &&
                r.ToName == "Test User" &&
                r.Subject == "Welcome to NTIS Platform - Your Account Details" &&
                r.IsHtml == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutEmail_SkipsWelcomeEmail()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            Email = null, // No email provided
            CreatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>(),
            ModuleAccess = new List<UserModuleAllocationCreateDto>(),
            RoleAllocations = new List<UserRoleAllocationCreateDto>()
        };

        var userEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            Email = null,
            PasswordHash = "$2a$12$hashedpassword"
        };

        var userDto = new UserDto
        {
            Id = 1,
            UserName = "testuser",
            Email = null,
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        // Setup mocks
        _passwordGeneratorMock.Setup(p => p.Generate()).Returns("TempPass123!");
        _passwordHasherMock.Setup(p => p.HashPassword("TempPass123!")).Returns("$2a$12$hashedpassword");
        _mapperMock.Setup(m => m.Map<UserEntity>(createDto)).Returns(userEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(userEntity)).Returns(userDto);
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity e, CancellationToken ct) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        // Act
        var result = await _userService.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Email);

        // Verify email was NOT sent (no email address)
        _emailSettingsProviderMock.Verify(e => e.GetEmailSettingsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _emailTemplateServiceMock.Verify(e => e.GetTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(e => e.SendEmailAsync(
            It.IsAny<EmailRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyEmail_SkipsWelcomeEmail()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            Email = "   ", // Whitespace-only email
            CreatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>(),
            ModuleAccess = new List<UserModuleAllocationCreateDto>(),
            RoleAllocations = new List<UserRoleAllocationCreateDto>()
        };

        var userEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            Email = "   ",
            PasswordHash = "$2a$12$hashedpassword"
        };

        var userDto = new UserDto
        {
            Id = 1,
            UserName = "testuser",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        // Setup mocks
        _passwordGeneratorMock.Setup(p => p.Generate()).Returns("TempPass123!");
        _passwordHasherMock.Setup(p => p.HashPassword("TempPass123!")).Returns("$2a$12$hashedpassword");
        _mapperMock.Setup(m => m.Map<UserEntity>(createDto)).Returns(userEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(userEntity)).Returns(userDto);
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity e, CancellationToken ct) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        // Act
        var result = await _userService.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);

        // Verify email was NOT sent (empty/whitespace email)
        _emailServiceMock.Verify(e => e.SendEmailAsync(
            It.IsAny<EmailRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailSendingFails_StillCreatesUser()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            CreatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>(),
            ModuleAccess = new List<UserModuleAllocationCreateDto>(),
            RoleAllocations = new List<UserRoleAllocationCreateDto>()
        };

        var userEntity = new UserEntity
        {
            Id = 1,
            UserName = "testuser",
            Email = "test@example.com",
            PasswordHash = "$2a$12$hashedpassword"
        };

        var userDto = new UserDto
        {
            Id = 1,
            UserName = "testuser",
            Email = "test@example.com",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        var emailSettings = new EmailSettingsDto
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            SmtpUserName = "test",
            SmtpPassword = "pass",
            FromEmail = "noreply@test.com",
            FromName = "Test",
            SecureSocketOptions = "Auto",
            LoginUrl = "https://app.test.com/login"
        };

        // Setup mocks
        _passwordGeneratorMock.Setup(p => p.Generate()).Returns("TempPass123!");
        _passwordHasherMock.Setup(p => p.HashPassword("TempPass123!")).Returns("$2a$12$hashedpassword");
        _mapperMock.Setup(m => m.Map<UserEntity>(createDto)).Returns(userEntity);
        _mapperMock.Setup(m => m.Map<UserDto>(userEntity)).Returns(userDto);
        _mapperMock.Setup(m => m.Map<List<UserDepartmentAllocationDto>>(It.IsAny<List<UserDepartmentAllocationEntity>>()))
            .Returns(new List<UserDepartmentAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserModuleAllocationDto>>(It.IsAny<List<UserModuleAllocationEntity>>()))
            .Returns(new List<UserModuleAllocationDto>());
        _mapperMock.Setup(m => m.Map<List<UserRoleAllocationDto>>(It.IsAny<List<UserRoleAllocationEntity>>()))
            .Returns(new List<UserRoleAllocationDto>());

        _userRepositoryMock.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity e, CancellationToken ct) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _departmentMapRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserDepartmentAllocationEntity>().BuildMock());
        _moduleAccessRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserModuleAllocationEntity>().BuildMock());
        _roleAllocationRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(new List<UserRoleAllocationEntity>().BuildMock());

        _emailSettingsProviderMock.Setup(e => e.GetEmailSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailSettings);
        _emailTemplateServiceMock.Setup(e => e.GetTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>Welcome Email Body</html>");
        
        // Email sending throws exception
        _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP connection failed"));

        // Act
        var result = await _userService.CreateAsync(createDto);

        // Assert - User should still be created successfully despite email failure
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("testuser", result.UserName);
        Assert.Equal("test@example.com", result.Email);

        // Verify user was created (SaveChanges called)
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify email was attempted
        _emailServiceMock.Verify(e => e.SendEmailAsync(
            It.IsAny<EmailRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
