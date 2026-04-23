using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class PropertyTypeMasterServiceTests
{
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyTypeMasterService _service;

    public PropertyTypeMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyTypeMasterEntity, int>>();
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

        _service = new PropertyTypeMasterService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new PropertyTypeMasterEntity
        {
            Id = 1,
            PropertyDescription = "Residential",
            Type = "R",
            PropertyTypeGroup = "Residential Group",
            SearchSequence = 1,
            PropertyTypeCategoryId = 1,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<PropertyTypeMasterDto>(It.IsAny<PropertyTypeMasterEntity>()))
            .Returns(new PropertyTypeMasterDto
            {
                Id = 1,
                PropertyDescription = "Residential",
                Type = "R",
                PropertyTypeGroup = "Residential Group",
                SearchSequence = 1,
                PropertyTypeCategoryId = 1,
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Residential", result.PropertyDescription);
        Assert.Equal("R", result.Type);
        Assert.Equal("Residential Group", result.PropertyTypeGroup);
        Assert.Equal(1, result.SearchSequence);
        Assert.Equal(1, result.PropertyTypeCategoryId);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTypeMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<PropertyTypeMasterEntity>
        {
            new() { Id = 1, PropertyDescription = "Type1", Type = "T1", IsActive = true },
            new() { Id = 2, PropertyDescription = "Type2", Type = "T2", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyTypeMasterEntity, PropertyTypeMasterDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyTypeMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyTypeMasterQueryParameters
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
        Assert.Contains(items, x => x.PropertyDescription == "Type1");
        Assert.Contains(items, x => x.PropertyDescription == "Type2");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreatePropertyTypeMasterDto
        {
            PropertyDescription = "New Type",
            Type = "N",
            PropertyTypeGroup = "New Group",
            SearchSequence = 3,
            PropertyTypeCategoryId = 2,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<PropertyTypeMasterEntity>(It.IsAny<CreatePropertyTypeMasterDto>()))
            .Returns((CreatePropertyTypeMasterDto dto) => new PropertyTypeMasterEntity
            {
                PropertyDescription = dto.PropertyDescription,
                Type = dto.Type,
                PropertyTypeGroup = dto.PropertyTypeGroup,
                SearchSequence = dto.SearchSequence,
                PropertyTypeCategoryId = dto.PropertyTypeCategoryId,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTypeMasterEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<PropertyTypeMasterDto>(It.IsAny<PropertyTypeMasterEntity>()))
            .Returns((PropertyTypeMasterEntity e) => new PropertyTypeMasterDto
            {
                PropertyDescription = e.PropertyDescription,
                Type = e.Type,
                PropertyTypeGroup = e.PropertyTypeGroup,
                SearchSequence = e.SearchSequence,
                PropertyTypeCategoryId = e.PropertyTypeCategoryId,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Type", result.PropertyDescription);
        Assert.Equal("N", result.Type);
        Assert.Equal("New Group", result.PropertyTypeGroup);
        Assert.Equal(3, result.SearchSequence);
        Assert.Equal(2, result.PropertyTypeCategoryId);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyTypeMasterDto
        {
            PropertyDescription = "Updated Description",
            Type = "U",
            PropertyTypeGroup = "Updated Group",
            SearchSequence = 5,
            PropertyTypeCategoryId = 3,
            IsActive = true
        };

        var existingEntity = new PropertyTypeMasterEntity
        {
            Id = 1,
            PropertyDescription = "Old Description",
            Type = "O",
            PropertyTypeGroup = "Old Group",
            SearchSequence = 1,
            PropertyTypeCategoryId = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyTypeMasterDto>(), It.IsAny<PropertyTypeMasterEntity>()))
            .Callback((UpdatePropertyTypeMasterDto src, PropertyTypeMasterEntity dest) =>
            {
                dest.PropertyDescription = src.PropertyDescription;
                dest.Type = src.Type;
                dest.PropertyTypeGroup = src.PropertyTypeGroup;
                dest.SearchSequence = src.SearchSequence;
                dest.PropertyTypeCategoryId = src.PropertyTypeCategoryId;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("Updated Description", existingEntity.PropertyDescription);
        Assert.Equal("U", existingEntity.Type);
        Assert.Equal("Updated Group", existingEntity.PropertyTypeGroup);
        Assert.Equal(5, existingEntity.SearchSequence);
        Assert.Equal(3, existingEntity.PropertyTypeCategoryId);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdatePropertyTypeMasterDto
        {
            PropertyDescription = "Test",
            Type = "T",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTypeMasterEntity?)null);

        // Act
        await _service.UpdateAsync(9999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        int idToDelete = 9999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTypeMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        int idToDelete = 1;

        var existingEntity = new PropertyTypeMasterEntity
        {
            Id = idToDelete,
            PropertyDescription = "Type to Delete",
            Type = "D",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<PropertyTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
