using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class TypeOfUseGroupServiceTests
{
    private readonly Mock<IRepository<TypeOfUseGroupEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TypeOfUseGroupService _service;

    public TypeOfUseGroupServiceTests()
    {
        _mockRepository = new Mock<IRepository<TypeOfUseGroupEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new TypeOfUseGroupService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new TypeOfUseGroupEntity
        {
            Id = 1,
            TypeOfUseGroupCode = "R",
            GroupName = "Residential",
            GroupIcon = "Home",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<TypeOfUseGroupDto>(It.IsAny<TypeOfUseGroupEntity>()))
            .Returns((TypeOfUseGroupEntity e) => new TypeOfUseGroupDto
            {
                Id = e.Id,
                TypeOfUseGroupCode = e.TypeOfUseGroupCode,
                GroupName = e.GroupName,
                GroupIcon = e.GroupIcon,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("R", result.TypeOfUseGroupCode);
        Assert.Equal("Residential", result.GroupName);
        Assert.Equal("Home", result.GroupIcon);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<TypeOfUseGroupEntity>
        {
            new() { Id = 1, TypeOfUseGroupCode = "R", GroupName = "Residential", GroupIcon = "Home", IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now },
            new() { Id = 2, TypeOfUseGroupCode = "C", GroupName = "Commercial", GroupIcon = "Building", IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TypeOfUseGroupEntity, TypeOfUseGroupDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TypeOfUseGroupService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new TypeOfUseGroupQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
            SearchTerm = null!,
            SortBy = null!
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.Id == 1);
        Assert.Contains(items, x => x.Id == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupDto
        {
            TypeOfUseGroupCode = "R",
            GroupName = "Residential",
            GroupIcon = "Home",
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<TypeOfUseGroupEntity>(It.IsAny<CreateTypeOfUseGroupDto>()))
            .Returns((CreateTypeOfUseGroupDto dto) => new TypeOfUseGroupEntity
            {
                Id = 1,
                TypeOfUseGroupCode = dto.TypeOfUseGroupCode,
                GroupName = dto.GroupName,
                GroupIcon = dto.GroupIcon,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<TypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<TypeOfUseGroupDto>(It.IsAny<TypeOfUseGroupEntity>()))
            .Returns((TypeOfUseGroupEntity e) => new TypeOfUseGroupDto
            {
                Id = e.Id,
                TypeOfUseGroupCode = e.TypeOfUseGroupCode,
                GroupName = e.GroupName,
                GroupIcon = e.GroupIcon,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("R", result.TypeOfUseGroupCode);
        Assert.Equal("Residential", result.GroupName);
        Assert.Equal("Home", result.GroupIcon);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<TypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupDto
        {
            TypeOfUseGroupCode = "R-UPD",
            GroupName = "Residential Updated",
            GroupIcon = "HomeNew",
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new TypeOfUseGroupEntity
        {
            Id = 1,
            TypeOfUseGroupCode = "R",
            GroupName = "Residential",
            GroupIcon = "Home",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTypeOfUseGroupDto>(), It.IsAny<TypeOfUseGroupEntity>()))
            .Callback((UpdateTypeOfUseGroupDto src, TypeOfUseGroupEntity dest) =>
            {
                dest.TypeOfUseGroupCode = src.TypeOfUseGroupCode;
                dest.GroupName = src.GroupName;
                dest.GroupIcon = src.GroupIcon;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<TypeOfUseGroupDto>(It.IsAny<TypeOfUseGroupEntity>()))
            .Returns((TypeOfUseGroupEntity e) => new TypeOfUseGroupDto
            {
                Id = e.Id,
                TypeOfUseGroupCode = e.TypeOfUseGroupCode,
                GroupName = e.GroupName,
                GroupIcon = e.GroupIcon,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("R-UPD", existingEntity.TypeOfUseGroupCode);
        Assert.Equal("Residential Updated", existingEntity.GroupName);
        Assert.Equal("HomeNew", existingEntity.GroupIcon);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupDto
        {
            TypeOfUseGroupCode = "R",
            GroupName = "Residential",
            GroupIcon = "Home",
            IsActive = true,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new TypeOfUseGroupEntity
        {
            Id = idToDelete,
            TypeOfUseGroupCode = "R",
            GroupName = "Residential",
            GroupIcon = "Home",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}