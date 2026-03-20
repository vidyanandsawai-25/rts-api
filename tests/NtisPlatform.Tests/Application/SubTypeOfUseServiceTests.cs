using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class SubTypeOfUseServiceTests
{
    private readonly Mock<IRepository<SubTypeOfUseEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly SubTypeOfUseService _service;

    public SubTypeOfUseServiceTests()
    {
        _mockRepository = new Mock<IRepository<SubTypeOfUseEntity, int>>();
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

        _service = new SubTypeOfUseService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new SubTypeOfUseEntity
        {
            SubTypeOfUseId = 1,
            TypeOfUseId = 1,
            Description = "Residential",
            SearchKey = "Alt+D",
            SearchSequence = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<SubTypeOfUseDto>(It.IsAny<SubTypeOfUseEntity>()))
            .Returns((SubTypeOfUseEntity e) => new SubTypeOfUseDto
            {
                SubTypeOfUseId = e.SubTypeOfUseId,
                TypeOfUseId = e.TypeOfUseId,
                Description = e.Description,
                SearchKey = e.SearchKey,
                SearchSequence = e.SearchSequence,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SubTypeOfUseId);
        Assert.Equal(1, result.TypeOfUseId);
        Assert.Equal("Residential", result.Description);
        Assert.Equal("Alt+D", result.SearchKey);
        Assert.Equal(1, result.SearchSequence);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubTypeOfUseEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<SubTypeOfUseEntity>
        {
            new() { SubTypeOfUseId = 1, TypeOfUseId = 1, Description = "Residential", SearchKey = "Alt+D", SearchSequence = 1, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now },
            new() { SubTypeOfUseId = 2, TypeOfUseId = 2, Description = "Commercial", SearchKey = "Alt+C", SearchSequence = 2, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SubTypeOfUseEntity, SubTypeOfUseDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new SubTypeOfUseService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new SubTypeOfUseQueryParameters
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
        Assert.Contains(items, x => x.Description == "Residential");
        Assert.Contains(items, x => x.Description == "Commercial");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateSubTypeOfUseDto
        {
            TypeOfUseId = 1,
            Description = "Residential",
            SearchKey = "Alt+D",
            SearchSequence = 1,
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<SubTypeOfUseEntity>(It.IsAny<CreateSubTypeOfUseDto>()))
            .Returns((CreateSubTypeOfUseDto dto) => new SubTypeOfUseEntity
            {
                SubTypeOfUseId = 1,
                TypeOfUseId = dto.TypeOfUseId,
                Description = dto.Description,
                SearchKey = dto.SearchKey,
                SearchSequence = dto.SearchSequence,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<SubTypeOfUseEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubTypeOfUseEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<SubTypeOfUseDto>(It.IsAny<SubTypeOfUseEntity>()))
            .Returns((SubTypeOfUseEntity e) => new SubTypeOfUseDto
            {
                SubTypeOfUseId = e.SubTypeOfUseId,
                TypeOfUseId = e.TypeOfUseId,
                Description = e.Description,
                SearchKey = e.SearchKey,
                SearchSequence = e.SearchSequence,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SubTypeOfUseId);
        Assert.Equal(1, result.TypeOfUseId);
        Assert.Equal("Residential", result.Description);
        Assert.Equal("Alt+D", result.SearchKey);
        Assert.Equal(1, result.SearchSequence);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<SubTypeOfUseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateSubTypeOfUseDto
        {
            TypeOfUseId =1,
            Description = "Residential Updated",
            SearchKey = "Alt+R",
            SearchSequence = 2,
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new SubTypeOfUseEntity
        {
            SubTypeOfUseId = 1,
            TypeOfUseId = 1,
            Description = "Residential",
            SearchKey = "Alt+D",
            SearchSequence = 1,
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
            .Setup(r => r.UpdateAsync(It.IsAny<SubTypeOfUseEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateSubTypeOfUseDto>(), It.IsAny<SubTypeOfUseEntity>()))
            .Callback((UpdateSubTypeOfUseDto src, SubTypeOfUseEntity dest) =>
            {
                dest.TypeOfUseId = src.TypeOfUseId;
                dest.Description = src.Description;
                dest.SearchKey = src.SearchKey;
                dest.SearchSequence = src.SearchSequence;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<SubTypeOfUseDto>(It.IsAny<SubTypeOfUseEntity>()))
            .Returns((SubTypeOfUseEntity e) => new SubTypeOfUseDto
            {
                SubTypeOfUseId = e.SubTypeOfUseId,
                TypeOfUseId = e.TypeOfUseId,
                Description = e.Description,
                SearchKey = e.SearchKey,
                SearchSequence = e.SearchSequence,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SubTypeOfUseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("Residential Updated", existingEntity.Description);
        Assert.Equal("Alt+R", existingEntity.SearchKey);
        Assert.Equal(2, existingEntity.SearchSequence);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateSubTypeOfUseDto
        {
            TypeOfUseId = 1,
            Description = "Residential",
            SearchKey = "Alt+D",
            SearchSequence = 1,
            IsActive = true,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubTypeOfUseEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SubTypeOfUseEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        int idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubTypeOfUseEntity?)null);

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
        int idToDelete = 1;

        var existingEntity = new SubTypeOfUseEntity
        {
            SubTypeOfUseId = idToDelete,
            TypeOfUseId = 1,
            Description = "Residential",
            SearchKey = "Alt+D",
            SearchSequence = 1,
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