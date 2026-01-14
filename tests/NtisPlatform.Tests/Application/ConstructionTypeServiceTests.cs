using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class ConstructionTypeServiceTests
{
    private readonly Mock<IRepository<ConstructionTypeEntity, string>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ConstructionTypeService _service;

    public ConstructionTypeServiceTests()
    {
        _mockRepository = new Mock<IRepository<ConstructionTypeEntity, string>>();
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

        _service = new ConstructionTypeService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new ConstructionTypeEntity
        {
            ConstructionId = "A",
            Description = "RCC",
            DescriptionEnglish = "RCC",
            GroupID = "1",
            KeyboardShortCutKey = "Alt+D",
            KeyWiseSequence = 1,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = 31,
            UpdatedDate = DateTime.UtcNow,
            UpdatedBy = 31
        };

        _mockRepository.Setup(r => r.GetByIdAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<ConstructionTypeDto>(It.IsAny<ConstructionTypeEntity>()))
            .Returns(new ConstructionTypeDto
            {
                ConstructionId = "A",
                Description = "RCC",
                DescriptionEnglish = "RCC",
                GroupID = "1",
                KeyboardShortCutKey = "Alt+D",
                KeyWiseSequence = 1,
            });

        // Act
        var result = await _service.GetByIdAsync("A");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("A", result.ConstructionId);
        Assert.Equal("RCC", result.Description);
        Assert.Equal("RCC", result.DescriptionEnglish);
        Assert.Equal("1", result.GroupID);
        Assert.Equal("Alt+D", result.KeyboardShortCutKey);
        Assert.Equal(1, result.KeyWiseSequence);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync("ZZZZ", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConstructionTypeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync("ZZZZ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<ConstructionTypeEntity>
        {
            new() { ConstructionId = "A", Description = "Test1", DescriptionEnglish = "Desc1", GroupID="1", KeyboardShortCutKey="Alt+D", KeyWiseSequence=1, CreatedBy=31, CreatedDate = DateTime.UtcNow },
            new() { ConstructionId = "B", Description = "Test2", DescriptionEnglish = "Desc2", GroupID="2", KeyboardShortCutKey="Alt+L", KeyWiseSequence=2, CreatedBy=31, CreatedDate = DateTime.UtcNow },
        };

        var mockQuery = entities.BuildMock(); // async IQueryable
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConstructionTypeEntity, ConstructionTypeDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new ConstructionTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new ConstructionTypeQueryParameters
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
        Assert.Contains(items, x => x.ConstructionId == "A");
        Assert.Contains(items, x => x.ConstructionId == "B");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateConstructionTypeDto
        {
            ConstructionId = "A",
            Description = "New Description",
            DescriptionEnglish = "New English Description",
            GroupID = "1",
            KeyboardShortCutKey = "Alt+A",
            KeyWiseSequence = 3,
        };

        _mockMapper
            .Setup(m => m.Map<ConstructionTypeEntity>(It.IsAny<CreateConstructionTypeDto>()))
            .Returns((CreateConstructionTypeDto dto) => new ConstructionTypeEntity
            {
                ConstructionId = dto.ConstructionId,
                Description = dto.Description,
                DescriptionEnglish = dto.DescriptionEnglish,
                GroupID = dto.GroupID,
                KeyboardShortCutKey = dto.KeyboardShortCutKey,
                KeyWiseSequence = dto.KeyWiseSequence,
                CreatedBy = 31,
                CreatedDate = DateTime.UtcNow
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConstructionTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<ConstructionTypeDto>(It.IsAny<ConstructionTypeEntity>()))
            .Returns((ConstructionTypeEntity e) => new ConstructionTypeDto
            {
                ConstructionId = e.ConstructionId,
                Description = e.Description,
                DescriptionEnglish = e.DescriptionEnglish,
                GroupID = e.GroupID,
                KeyboardShortCutKey = e.KeyboardShortCutKey,
                KeyWiseSequence = e.KeyWiseSequence
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("A", result.ConstructionId);
        Assert.Equal("New Description", result.Description);
        Assert.Equal("New English Description", result.DescriptionEnglish);
        Assert.Equal("1", result.GroupID);
        Assert.Equal("Alt+A", result.KeyboardShortCutKey);
        Assert.Equal(3, result.KeyWiseSequence);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);

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
        var updateDto = new UpdateConstructionTypeDto
        {
            Description = "New Description",
            DescriptionEnglish = "New English Description",
            GroupID = "1",
            KeyboardShortCutKey = "Alt+A",
            KeyWiseSequence = 3,
        };

        var existingEntity = new ConstructionTypeEntity
        {
            ConstructionId = "A",
            Description = "Old Description",
            DescriptionEnglish = "Old English Description",
            GroupID = "1",
            KeyboardShortCutKey = "Alt+A",
            KeyWiseSequence = 3,
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateConstructionTypeDto>(), It.IsAny<ConstructionTypeEntity>()))
            .Callback((UpdateConstructionTypeDto src, ConstructionTypeEntity dest) =>
            {
                dest.Description = src.Description;
                dest.DescriptionEnglish = src.DescriptionEnglish;
            });

        // Act
        await _service.UpdateAsync("A", updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync("A", It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("New Description", existingEntity.Description);
        Assert.Equal("New English Description", existingEntity.DescriptionEnglish);
        Assert.Equal("1", existingEntity.GroupID);
        Assert.Equal("Alt+A", existingEntity.KeyboardShortCutKey);
        Assert.Equal(3, existingEntity.KeyWiseSequence);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateConstructionTypeDto
        {
            Description = "New Description",
            DescriptionEnglish = "New English Description",
            GroupID = "1",
            KeyboardShortCutKey = "Alt+A",
            KeyWiseSequence = 3
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync("ZZZ", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConstructionTypeEntity?)null);

        // Act
        await _service.UpdateAsync("ZZZ", updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
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
            .ReturnsAsync((ConstructionTypeEntity?)null);

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
        var idToDelete = "A";

        var existingEntity = new ConstructionTypeEntity
        {
            ConstructionId = idToDelete,
            Description = "RCC",
            DescriptionEnglish = "RCC",
            GroupID = "1",
            KeyboardShortCutKey = "Alt+A",
            KeyWiseSequence = 3,
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
