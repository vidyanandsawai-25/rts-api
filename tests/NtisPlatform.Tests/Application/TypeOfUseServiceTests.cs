using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class TypeOfUseServiceTests
{
    private readonly Mock<IRepository<TypeOfUseEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TypeOfUseService _service;

    public TypeOfUseServiceTests()
    {
        _mockRepository = new Mock<IRepository<TypeOfUseEntity, int>>();
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

        _service = new TypeOfUseService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new TypeOfUseEntity
        {
            Id = 1,
            TypeOfUseCode = "R",
            Description = "Residential",
            Type = "R",
            TypeOfUseGroupId = 1,
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

        _mockMapper.Setup(m => m.Map<TypeOfUseDto>(It.IsAny<TypeOfUseEntity>()))
            .Returns((TypeOfUseEntity e) => new TypeOfUseDto
            {
                Id = e.Id,
                TypeOfUseCode = e.TypeOfUseCode,
                Description = e.Description,
                Type = e.Type,
                TypeOfUseGroupId = e.TypeOfUseGroupId,
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
        Assert.Equal(1, result.Id);
        Assert.Equal("R", result.TypeOfUseCode);
        Assert.Equal("Residential", result.Description);
        Assert.Equal("R", result.Type);
        Assert.Equal(1, result.TypeOfUseGroupId);
        Assert.Equal("Alt+D", result.SearchKey);
        Assert.Equal(1, result.SearchSequence);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<TypeOfUseEntity>
        {
            new() { Id = 1, TypeOfUseCode = "R", Description = "Residential", Type = "R", TypeOfUseGroupId = 1, SearchKey = "Alt+D", SearchSequence = 1, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now },
            new() { Id = 2, TypeOfUseCode = "C", Description = "Commercial", Type = "C", TypeOfUseGroupId = 2, SearchKey = "Alt+C", SearchSequence = 2, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TypeOfUseEntity, TypeOfUseDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TypeOfUseService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new TypeOfUseQueryParameters
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
        var createDto = new CreateTypeOfUseDto
        {
            TypeOfUseCode = "R",
            Description = "Residential",
            Type = "R",
            TypeOfUseGroupId = 1,
            SearchKey = "Alt+D",
            SearchSequence = 1,
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<TypeOfUseEntity>(It.IsAny<CreateTypeOfUseDto>()))
            .Returns((CreateTypeOfUseDto dto) => new TypeOfUseEntity
            {
                Id = 1,
                TypeOfUseCode = dto.TypeOfUseCode,
                Description = dto.Description,
                Type = dto.Type,
                TypeOfUseGroupId = dto.TypeOfUseGroupId,
                SearchKey = dto.SearchKey,
                SearchSequence = dto.SearchSequence,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<TypeOfUseDto>(It.IsAny<TypeOfUseEntity>()))
            .Returns((TypeOfUseEntity e) => new TypeOfUseDto
            {
                Id = e.Id,
                TypeOfUseCode = e.TypeOfUseCode,
                Description = e.Description,
                Type = e.Type,
                TypeOfUseGroupId = e.TypeOfUseGroupId,
                SearchKey = e.SearchKey,
                SearchSequence = e.SearchSequence,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("R", result.TypeOfUseCode);
        Assert.Equal("Residential", result.Description);
        Assert.Equal("R", result.Type);
        Assert.Equal(1, result.TypeOfUseGroupId);
        Assert.Equal("Alt+D", result.SearchKey);
        Assert.Equal(1, result.SearchSequence);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseDto
        {
            TypeOfUseCode = "R-UPD",
            Description = "Residential Updated",
            Type = "R",
            TypeOfUseGroupId = 1,
            SearchKey = "Alt+R",
            SearchSequence = 2,
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new TypeOfUseEntity
        {
            Id = 1,
            TypeOfUseCode = "R",
            Description = "Residential",
            Type = "R",
            TypeOfUseGroupId = 1,
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
            .Setup(r => r.UpdateAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTypeOfUseDto>(), It.IsAny<TypeOfUseEntity>()))
            .Callback((UpdateTypeOfUseDto src, TypeOfUseEntity dest) =>
            {
                dest.TypeOfUseCode = src.TypeOfUseCode;
                dest.Description = src.Description;
                dest.Type = src.Type;
                dest.TypeOfUseGroupId = src.TypeOfUseGroupId;
                dest.SearchKey = src.SearchKey;
                dest.SearchSequence = src.SearchSequence;
                dest.UpdatedBy = src.UpdatedBy;
            });

        _mockMapper
            .Setup(m => m.Map<TypeOfUseDto>(It.IsAny<TypeOfUseEntity>()))
            .Returns((TypeOfUseEntity e) => new TypeOfUseDto
            {
                Id = e.Id,
                TypeOfUseCode = e.TypeOfUseCode,
                Description = e.Description,
                Type = e.Type,
                TypeOfUseGroupId = e.TypeOfUseGroupId,
                SearchKey = e.SearchKey,
                SearchSequence = e.SearchSequence,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("R-UPD", existingEntity.TypeOfUseCode);
        Assert.Equal("Residential Updated", existingEntity.Description);
        Assert.Equal("R", existingEntity.Type);
        Assert.Equal(1, existingEntity.TypeOfUseGroupId);
        Assert.Equal("Alt+R", existingEntity.SearchKey);
        Assert.Equal(2, existingEntity.SearchSequence);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseDto
        {
            TypeOfUseCode = "R",
            Description = "Residential",
            Type = "R",
            TypeOfUseGroupId = 1,
            SearchKey = "Alt+D",
            SearchSequence = 1,
            IsActive = true,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()), Times.Never);
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
            .ReturnsAsync((TypeOfUseEntity?)null);

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

        var existingEntity = new TypeOfUseEntity
        {
            Id = idToDelete,
            TypeOfUseCode = "R",
            Description = "Residential",
            Type = "R",
            TypeOfUseGroupId = 1,
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
            .Setup(r => r.DeleteAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
