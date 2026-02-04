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
    private readonly Mock<IRepository<TypeOfUseGroupEntity, string>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TypeOfUseGroupService _service;

    public TypeOfUseGroupServiceTests()
    {
        _mockRepository = new Mock<IRepository<TypeOfUseGroupEntity, string>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        // Service is calling SaveChangesAsync (NOT transactions), so setup SaveChangesAsync.
        // If your SaveChangesAsync returns Task (not Task<int>), change ReturnsAsync(1) to Returns(Task.CompletedTask).
        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Optional: keep these setups if your interface has them (harmless even if not called)
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
            TypeOfUseGroupID = "R",
            GroupNameEnglish = "R",
            GroupName = "Residential",
            GroupIcon = "Home",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository.Setup(r => r.GetByIdAsync("R", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<TypeOfUseGroupDto>(It.IsAny<TypeOfUseGroupEntity>()))
            .Returns(new TypeOfUseGroupDto
            {
                TypeOfUseGroupID = "R",
                GroupNameEnglish = "R",
                GroupName = "Residential",
                GroupIcon = "Home",
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

        // Act
        var result = await _service.GetByIdAsync("R");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("R", result.TypeOfUseGroupID);
        Assert.Equal("R", result.GroupNameEnglish);
        Assert.Equal("Residential", result.GroupName);
        Assert.Equal("Home", result.GroupIcon);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync("ZZZZ", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupEntity?)null);

        // Act
        var result = await _service.GetByIdAsync("ZZZZ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<TypeOfUseGroupEntity>
        {
            new() { TypeOfUseGroupID = "R", GroupNameEnglish = "R", GroupName = "Residential", GroupIcon="Home",IsActive=true, CreatedBy=31, CreatedDate = DateTime.Now, UpdatedBy=31, UpdatedDate=DateTime.Now },
            new() { TypeOfUseGroupID = "C", GroupNameEnglish = "C", GroupName = "Commercial", GroupIcon="Building",IsActive=true, CreatedBy=31, CreatedDate = DateTime.Now, UpdatedBy=31, UpdatedDate=DateTime.Now }
        };

        var mockQuery = entities.BuildMock(); // async IQueryable
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
        Assert.Contains(items, x => x.TypeOfUseGroupID == "R");
        Assert.Contains(items, x => x.TypeOfUseGroupID == "C");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupDto
        {
            TypeOfUseGroupID = "R",
            GroupNameEnglish = "R",
            GroupName = "Residential",
            GroupIcon = "Home",
            IsActive = true,
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<TypeOfUseGroupEntity>(It.IsAny<CreateTypeOfUseGroupDto>()))
            .Returns((CreateTypeOfUseGroupDto dto) => new TypeOfUseGroupEntity
            {
                TypeOfUseGroupID = dto.TypeOfUseGroupID,
                GroupNameEnglish = dto.GroupNameEnglish,
                GroupName = dto.GroupName,
                GroupIcon = dto.GroupIcon,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = 31,
                UpdatedDate = DateTime.Now,
                UpdatedBy = 31
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<TypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<TypeOfUseGroupDto>(It.IsAny<TypeOfUseGroupEntity>()))
            .Returns((TypeOfUseGroupEntity e) => new TypeOfUseGroupDto
            {
                TypeOfUseGroupID = e.TypeOfUseGroupID,
                GroupNameEnglish = e.GroupNameEnglish,
                GroupName = e.GroupName,
                GroupIcon = e.GroupIcon,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("R", result.TypeOfUseGroupID);
        Assert.Equal("R", result.GroupNameEnglish);
        Assert.Equal("Residential", result.GroupName);
        Assert.Equal("Home", result.GroupIcon);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<TypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        // Service calls SaveChangesAsync (based on your test output)
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Not called by service (based on your test output)
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupDto
        {
            GroupNameEnglish = "R",
            GroupName = "Residential",
            GroupIcon = "Home",
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new TypeOfUseGroupEntity
        {
            TypeOfUseGroupID = "R",
            GroupNameEnglish = "RR",
            GroupName = "Old Residential",
            GroupIcon = "Home",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync("R", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTypeOfUseGroupDto>(), It.IsAny<TypeOfUseGroupEntity>()))
            .Callback((UpdateTypeOfUseGroupDto src, TypeOfUseGroupEntity dest) =>
            {
                dest.GroupNameEnglish = src.GroupNameEnglish;
                dest.GroupName = src.GroupName;
                dest.GroupIcon = src.GroupIcon;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        // Act
        await _service.UpdateAsync("R", updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync("R", It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("R", existingEntity.TypeOfUseGroupID);
        Assert.Equal("R", existingEntity.GroupNameEnglish);
        Assert.Equal("Residential", existingEntity.GroupName);
        Assert.Equal("Home", existingEntity.GroupIcon);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupDto
        {
            GroupNameEnglish = "R",
            GroupName = "Residential",
            GroupIcon = "Home",
            IsActive = true,
            UpdatedBy = 32
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync("ZZZ", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupEntity?)null);

        // Act
        await _service.UpdateAsync("ZZZ", updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = "ZZZ";

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = "R";

        var existingEntity = new TypeOfUseGroupEntity
        {
            TypeOfUseGroupID = idToDelete,
            GroupNameEnglish = "R",
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

