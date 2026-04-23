using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class PropertyDescriptionAndTypeOfUseValidationServiceTests
{
    private readonly Mock<IRepository<PropertyDescriptionAndTypeOfUseValidationEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyDescriptionAndTypeOfUseValidationService _service;

    public PropertyDescriptionAndTypeOfUseValidationServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyDescriptionAndTypeOfUseValidationEntity, int>>();
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

        _service = new PropertyDescriptionAndTypeOfUseValidationService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<PropertyDescriptionAndTypeOfUseValidationDto>(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>()))
            .Returns(new PropertyDescriptionAndTypeOfUseValidationDto
            {
                Id = 1,
                PropertyTypeId = 5,
                TypeOfUseId = 10,
                IsActive = true,
            });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(5, result.PropertyTypeId);
        Assert.Equal(10, result.TypeOfUseId);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDescriptionAndTypeOfUseValidationEntity?)null);

        var result = await _service.GetByIdAsync(9999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<PropertyDescriptionAndTypeOfUseValidationEntity>
        {
            new() {Id=1, PropertyTypeId = 5, TypeOfUseId = 10, CreatedBy=1, CreatedDate = DateTime.Now, IsActive=true},
            new() {Id=2, PropertyTypeId = 6, TypeOfUseId = 11, CreatedBy=1, CreatedDate = DateTime.Now, IsActive=true},
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyDescriptionAndTypeOfUseValidationEntity, PropertyDescriptionAndTypeOfUseValidationDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyDescriptionAndTypeOfUseValidationService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
            SearchTerm = null!,
            SortBy = null!
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.PropertyTypeId == 5);
        Assert.Contains(items, x => x.PropertyTypeId == 6);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
        };

        _mockMapper
            .Setup(m => m.Map<PropertyDescriptionAndTypeOfUseValidationEntity>(It.IsAny<CreatePropertyDescriptionAndTypeOfUseValidationDto>()))
            .Returns((CreatePropertyDescriptionAndTypeOfUseValidationDto dto) => new PropertyDescriptionAndTypeOfUseValidationEntity
            {
                PropertyTypeId = dto.PropertyTypeId,
                TypeOfUseId = dto.TypeOfUseId,
                CreatedBy = 1,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDescriptionAndTypeOfUseValidationEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<PropertyDescriptionAndTypeOfUseValidationDto>(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>()))
            .Returns((PropertyDescriptionAndTypeOfUseValidationEntity e) => new PropertyDescriptionAndTypeOfUseValidationDto
            {
                PropertyTypeId = e.PropertyTypeId,
                TypeOfUseId = e.TypeOfUseId,
                IsActive = true,
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result.PropertyTypeId);
        Assert.Equal(10, result.TypeOfUseId);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            IsActive = true,
        };

        var existingEntity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyDescriptionAndTypeOfUseValidationDto>(), It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>()))
            .Callback((UpdatePropertyDescriptionAndTypeOfUseValidationDto src, PropertyDescriptionAndTypeOfUseValidationEntity dest) =>
            {
                dest.PropertyTypeId = src.PropertyTypeId;
                dest.TypeOfUseId = src.TypeOfUseId;
                dest.IsActive = src.IsActive;
            });

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal(6, existingEntity.PropertyTypeId);
        Assert.Equal(11, existingEntity.TypeOfUseId);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        var updateDto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDescriptionAndTypeOfUseValidationEntity?)null);

        await _service.UpdateAsync(9999, updateDto, CancellationToken.None);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        int idToDelete = 9999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDescriptionAndTypeOfUseValidationEntity?)null);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        int idToDelete = 1;

        var existingEntity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = idToDelete,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithFiltering_ReturnsFilteredResults()
    {
        var entities = new List<PropertyDescriptionAndTypeOfUseValidationEntity>
        {
            new() {Id=1, PropertyTypeId = 5, TypeOfUseId = 10, CreatedBy=1, CreatedDate = DateTime.Now, IsActive=true},
            new() {Id=2, PropertyTypeId = 5, TypeOfUseId = 11, CreatedBy=1, CreatedDate = DateTime.Now, IsActive=true},
            new() {Id=3, PropertyTypeId = 6, TypeOfUseId = 10, CreatedBy=1, CreatedDate = DateTime.Now, IsActive=true},
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyDescriptionAndTypeOfUseValidationEntity, PropertyDescriptionAndTypeOfUseValidationDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyDescriptionAndTypeOfUseValidationService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PropertyTypeId = 5,
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
            SearchTerm = null!,
            SortBy = null!
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(5, item.PropertyTypeId));
    }

    [Fact]
    public async Task GetAllAsync_WithSorting_ReturnsSortedResults()
    {
        var entities = new List<PropertyDescriptionAndTypeOfUseValidationEntity>
        {
            new() {Id=3, PropertyTypeId = 7, TypeOfUseId = 10, CreatedBy=1, CreatedDate = DateTime.Now, IsActive=true},
            new() {Id=1, PropertyTypeId = 5, TypeOfUseId = 11, CreatedBy=1, CreatedDate = DateTime.Now, IsActive=true},
            new() {Id=2, PropertyTypeId = 6, TypeOfUseId = 12, CreatedBy=1, CreatedDate = DateTime.Now, IsActive=true},
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyDescriptionAndTypeOfUseValidationEntity, PropertyDescriptionAndTypeOfUseValidationDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyDescriptionAndTypeOfUseValidationService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "PropertyTypeId",
            SortOrder = "asc"
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        var items = result.Items.ToList();
        Assert.Equal(5, items[0].PropertyTypeId);
        Assert.Equal(6, items[1].PropertyTypeId);
        Assert.Equal(7, items[2].PropertyTypeId);
    }

    [Fact]
    public async Task CreateAsync_MultipleEntities_CreatesSuccessfully()
    {
        var createDto1 = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
            CreatedBy = 1
        };

        var createDto2 = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<PropertyDescriptionAndTypeOfUseValidationEntity>(It.IsAny<CreatePropertyDescriptionAndTypeOfUseValidationDto>()))
            .Returns((CreatePropertyDescriptionAndTypeOfUseValidationDto dto) => new PropertyDescriptionAndTypeOfUseValidationEntity
            {
                PropertyTypeId = dto.PropertyTypeId,
                TypeOfUseId = dto.TypeOfUseId,
                CreatedBy = dto.CreatedBy ?? 1,
                CreatedDate = DateTime.Now,
                IsActive = dto.IsActive
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDescriptionAndTypeOfUseValidationEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<PropertyDescriptionAndTypeOfUseValidationDto>(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>()))
            .Returns((PropertyDescriptionAndTypeOfUseValidationEntity e) => new PropertyDescriptionAndTypeOfUseValidationDto
            {
                PropertyTypeId = e.PropertyTypeId,
                TypeOfUseId = e.TypeOfUseId,
                IsActive = e.IsActive,
            });

        var result1 = await _service.CreateAsync(createDto1, CancellationToken.None);
        var result2 = await _service.CreateAsync(createDto2, CancellationToken.None);

        Assert.NotNull(result1);
        Assert.Equal(5, result1.PropertyTypeId);
        Assert.Equal(10, result1.TypeOfUseId);

        Assert.NotNull(result2);
        Assert.Equal(6, result2.PropertyTypeId);
        Assert.Equal(11, result2.TypeOfUseId);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_ChangeIsActive_UpdatesSuccessfully()
    {
        var updateDto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = false,
            UpdatedBy = 2
        };

        var existingEntity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyDescriptionAndTypeOfUseValidationDto>(), It.IsAny<PropertyDescriptionAndTypeOfUseValidationEntity>()))
            .Callback((UpdatePropertyDescriptionAndTypeOfUseValidationDto src, PropertyDescriptionAndTypeOfUseValidationEntity dest) =>
            {
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
            });

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.False(existingEntity.IsActive);
        Assert.Equal(2, existingEntity.UpdatedBy);
    }
}
