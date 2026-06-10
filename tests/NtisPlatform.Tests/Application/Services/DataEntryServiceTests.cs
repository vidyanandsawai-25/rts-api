using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Application.DTOs.RenterMast;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.Linq.Expressions;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive test suite for DataEntryService
/// Tests CRUD operations, nested entity handling, and error scenarios
/// </summary>
public class DataEntryServiceTests
{
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IRenterDetailService> _mockRenterDetailService;
    private readonly Mock<IRenterMastService> _mockRenterMastService;
    private readonly Mock<IRoomWiseSubmissionDetailsService> _mockRoomWiseService;
    private readonly Mock<IRepository<PropertyEntity, int>> _mockPropertyRepository;
    private readonly DataEntryService _service;
    private readonly Mock<IQueryable<PropertyDetailsEntity>> _mockQueryable;

    public DataEntryServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyDetailsEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockRenterDetailService = new Mock<IRenterDetailService>();
        _mockRenterMastService = new Mock<IRenterMastService>();
        _mockRoomWiseService = new Mock<IRoomWiseSubmissionDetailsService>();
        _mockPropertyRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockQueryable = new Mock<IQueryable<PropertyDetailsEntity>>();

        // Setup transaction methods to prevent null Task returns
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup empty queryable for PropertyRepository to avoid test failures
        var emptyProperty = new List<PropertyEntity>().BuildMock();
        _mockPropertyRepository.Setup(r => r.GetQueryable()).Returns(emptyProperty);

        _service = new DataEntryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockRenterDetailService.Object,
            _mockRenterMastService.Object,
            _mockRoomWiseService.Object,
            _mockPropertyRepository.Object
        );
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var queryParameters = new PropertyDetailsQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            PropertyId = 0
        };

        var entities = new List<PropertyDetailsEntity>
        {
            CreateTestEntity(1),
            CreateTestEntity(2)
        }.AsQueryable();

        var dtos = new List<PropertyDetailsDto>
        {
            CreateTestDto(1),
            CreateTestDto(2)
        };

        SetupMockQueryable(entities);
        _mockMapper.Setup(m => m.Map<List<PropertyDetailsDto>>(It.IsAny<List<PropertyDetailsEntity>>()))
            .Returns(dtos);

        // Act
        var result = await _service.GetAllAsync(queryParameters, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetAllAsync_WithPropertyIdFilter_ReturnsFilteredResults()
    {
        // Arrange
        var queryParameters = new PropertyDetailsQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            PropertyId = 5
        };

        var entities = new List<PropertyDetailsEntity>
        {
            CreateTestEntity(1, propertyId: 5),
            CreateTestEntity(2, propertyId: 5)
        }.AsQueryable();

        var dtos = new List<PropertyDetailsDto>
        {
            CreateTestDto(1),
            CreateTestDto(2)
        };

        SetupMockQueryable(entities);
        _mockMapper.Setup(m => m.Map<List<PropertyDetailsDto>>(It.IsAny<List<PropertyDetailsEntity>>()))
            .Returns(dtos);

        // Act
        var result = await _service.GetAllAsync(queryParameters, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.NotNull(item));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var queryParameters = new PropertyDetailsQueryParameters
        {
            PageNumber = 2,
            PageSize = 5
        };

        var entities = Enumerable.Range(1, 15)
            .Select(i => CreateTestEntity(i))
            .AsQueryable();

        SetupMockQueryable(entities);
        _mockMapper.Setup(m => m.Map<List<PropertyDetailsDto>>(It.IsAny<List<PropertyDetailsEntity>>()))
            .Returns(new List<PropertyDetailsDto> { CreateTestDto(6), CreateTestDto(7) });

        // Act
        var result = await _service.GetAllAsync(queryParameters, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyResult_ReturnsEmptyPagedResult()
    {
        // Arrange
        var queryParameters = new PropertyDetailsQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var entities = new List<PropertyDetailsEntity>().AsQueryable();

        SetupMockQueryable(entities);
        _mockMapper.Setup(m => m.Map<List<PropertyDetailsDto>>(It.IsAny<List<PropertyDetailsEntity>>()))
            .Returns(new List<PropertyDetailsDto>());

        // Act
        var result = await _service.GetAllAsync(queryParameters, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var entityId = 1;
        var entity = CreateTestEntity(entityId);
        var dto = CreateTestDto(entityId);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        _mockMapper.Setup(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>()))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(entityId, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entityId, result.Id);
        _mockMapper.Verify(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var entityId = 999;
        var entities = new List<PropertyDetailsEntity>().AsQueryable();
        SetupMockQueryable(entities);

        // Act
        var result = await _service.GetByIdAsync(entityId, default);

        // Assert
        Assert.Null(result);
        _mockMapper.Verify(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WithInactiveEntity_ReturnsNull()
    {
        // Arrange
        var entityId = 1;
        var inactiveEntity = CreateTestEntity(entityId);
        inactiveEntity.IsActive = false;

        var entities = new List<PropertyDetailsEntity> { inactiveEntity }.AsQueryable();
        SetupMockQueryable(entities);

        // Act
        var result = await _service.GetByIdAsync(entityId, default);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesEntityAndChildren()
    {
        // Arrange
        var createDto = CreateTestCreateDto();
        var entity = CreateTestEntity(0);
        entity.Id = 1; // Simulates database-generated ID

        var resultDto = CreateTestDto(1);

        _mockMapper.Setup(m => m.Map<PropertyDetailsEntity>(createDto))
            .Returns(entity);
        _mockMapper.Setup(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>()))
            .Returns(resultDto);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        // Act
        var result = await _service.CreateAsync(createDto, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);

        // Verify parent entity was added
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify SaveChanges was called once (to get parent Id; CommitTransactionAsync handles children)
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify transaction was committed (which internally calls SaveChangesAsync again for children)
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithRenterDetails_CallsRenterDetailService()
    {
        // Arrange
        var createDto = CreateTestCreateDto();
        createDto.RenterDetails = new List<CreateRenterDetailsDto>
        {
            new CreateRenterDetailsDto { /* properties */ }
        };

        var entity = CreateTestEntity(0);
        entity.Id = 1;
        var resultDto = CreateTestDto(1);

        _mockMapper.Setup(m => m.Map<PropertyDetailsEntity>(createDto)).Returns(entity);
        _mockMapper.Setup(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>())).Returns(resultDto);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        // Act
        await _service.CreateAsync(createDto, default);

        // Assert
        _mockRenterDetailService.Verify(
            s => s.CreateRangeAsync(
                It.Is<int>(id => id == 1),
                It.IsAny<IEnumerable<CreateRenterDetailsDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithRenterMasts_CallsRenterMastService()
    {
        // Arrange
        var createDto = CreateTestCreateDto();
        createDto.Renters = new List<CreateRenterMastDto>
        {
            new CreateRenterMastDto { /* properties */ }
        };

        var entity = CreateTestEntity(0);
        entity.Id = 1;
        var resultDto = CreateTestDto(1);

        _mockMapper.Setup(m => m.Map<PropertyDetailsEntity>(createDto)).Returns(entity);
        _mockMapper.Setup(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>())).Returns(resultDto);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        // Act
        await _service.CreateAsync(createDto, default);

        // Assert
        _mockRenterMastService.Verify(
            s => s.CreateRangeAsync(
                It.Is<int>(id => id == 1),
                It.IsAny<IEnumerable<CreateRenterMastDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithRoomWiseSubmissionDetails_CallsRoomWiseService()
    {
        // Arrange
        var createDto = CreateTestCreateDto();
        createDto.RoomWiseSubmissionDetails = new List<CreateRoomWiseSubmissionDetailsDto>
        {
            new CreateRoomWiseSubmissionDetailsDto { /* properties */ }
        };

        var entity = CreateTestEntity(0);
        entity.Id = 1;
        var resultDto = CreateTestDto(1);

        _mockMapper.Setup(m => m.Map<PropertyDetailsEntity>(createDto)).Returns(entity);
        _mockMapper.Setup(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>())).Returns(resultDto);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        // Act
        await _service.CreateAsync(createDto, default);

        // Assert
        _mockRoomWiseService.Verify(
            s => s.CreateRangeAsync(
                It.Is<int>(id => id == 1),
                It.IsAny<IEnumerable<CreateRoomWiseSubmissionDetailsDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyNestedLists_DoesNotCallChildServices()
    {
        // Arrange
        var createDto = CreateTestCreateDto();
        createDto.RenterDetails = new List<CreateRenterDetailsDto>();
        createDto.Renters = new List<CreateRenterMastDto>();
        createDto.RoomWiseSubmissionDetails = new List<CreateRoomWiseSubmissionDetailsDto>();

        var entity = CreateTestEntity(0);
        entity.Id = 1;
        var resultDto = CreateTestDto(1);

        _mockMapper.Setup(m => m.Map<PropertyDetailsEntity>(createDto)).Returns(entity);
        _mockMapper.Setup(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>())).Returns(resultDto);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        // Act
        await _service.CreateAsync(createDto, default);

        // Assert
        _mockRenterDetailService.Verify(
            s => s.CreateRangeAsync(It.IsAny<int>(), It.IsAny<IEnumerable<CreateRenterDetailsDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockRenterMastService.Verify(
            s => s.CreateRangeAsync(It.IsAny<int>(), It.IsAny<IEnumerable<CreateRenterMastDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockRoomWiseService.Verify(
            s => s.CreateRangeAsync(It.IsAny<int>(), It.IsAny<IEnumerable<CreateRoomWiseSubmissionDetailsDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDto_UpdatesEntityAndChildren()
    {
        // Arrange
        var entityId = 1;
        var updateDto = CreateTestUpdateDto(entityId);
        var entity = CreateTestEntity(entityId);
        var resultDto = CreateTestDto(entityId);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        _mockMapper.Setup(m => m.Map(updateDto, entity)).Callback(() => { });
        _mockMapper.Setup(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>())).Returns(resultDto);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(entityId, updateDto, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entityId, result.Id);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var entityId = 999;
        var updateDto = CreateTestUpdateDto(entityId);
        var entities = new List<PropertyDetailsEntity>().AsQueryable();
        SetupMockQueryable(entities);

        // Act
        var result = await _service.UpdateAsync(entityId, updateDto, default);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithRenterDetails_CallsRenterDetailServiceUpdate()
    {
        // Arrange
        var entityId = 1;
        var updateDto = CreateTestUpdateDto(entityId);
        updateDto.RenterDetails = new List<UpdateRenterDetailsDto>
        {
            new UpdateRenterDetailsDto { /* properties */ }
        };

        var entity = CreateTestEntity(entityId);
        var resultDto = CreateTestDto(entityId);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        _mockMapper.Setup(m => m.Map(updateDto, entity));
        _mockMapper.Setup(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>())).Returns(resultDto);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _service.UpdateAsync(entityId, updateDto, default);

        // Assert
        _mockRenterDetailService.Verify(
            s => s.UpdateRangeAsync(
                It.Is<int>(id => id == entityId),
                It.IsAny<IEnumerable<UpdateRenterDetailsDto>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNullNestedLists_DoesNotCallChildServices()
    {
        // Arrange
        var entityId = 1;
        var updateDto = CreateTestUpdateDto(entityId);
        updateDto.RenterDetails = null;
        updateDto.Renters = null;
        updateDto.RoomWiseSubmissionDetails = null;

        var entity = CreateTestEntity(entityId);
        var resultDto = CreateTestDto(entityId);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        _mockMapper.Setup(m => m.Map(updateDto, entity));
        _mockMapper.Setup(m => m.Map<PropertyDetailsDto>(It.IsAny<PropertyDetailsEntity>())).Returns(resultDto);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PropertyDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _service.UpdateAsync(entityId, updateDto, default);

        // Assert
        _mockRenterDetailService.Verify(
            s => s.UpdateRangeAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UpdateRenterDetailsDto>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockRenterMastService.Verify(
            s => s.UpdateRangeAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UpdateRenterMastDto>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockRoomWiseService.Verify(
            s => s.UpdateRangeAsync(It.IsAny<int>(), It.IsAny<IEnumerable<UpdateRoomWiseSubmissionDetailsDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_SoftDeletesEntityAndChildren()
    {
        // Arrange
        var entityId = 1;
        var entity = CreateTestEntity(entityId);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        _mockRepository.Setup(r => r.DeleteAsync(entityId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(entityId, default);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(entityId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRenterDetailService.Verify(s => s.DeleteByPropertyIdAsync(entityId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRenterMastService.Verify(s => s.DeleteByPropertyIdAsync(entityId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRoomWiseService.Verify(s => s.DeleteByPropertyIdAsync(entityId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var entityId = 999;
        var entities = new List<PropertyDetailsEntity>().AsQueryable();
        SetupMockQueryable(entities);

        // Act
        var result = await _service.DeleteAsync(entityId, default);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithInactiveEntity_ReturnsFalse()
    {
        // Arrange
        var entityId = 1;
        var entity = CreateTestEntity(entityId);
        entity.IsActive = false;

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);

        // Act
        var result = await _service.DeleteAsync(entityId, default);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CascadeDeletesAllChildren()
    {
        // Arrange
        var entityId = 1;
        var entity = CreateTestEntity(entityId);

        var entities = new List<PropertyDetailsEntity> { entity }.AsQueryable();
        SetupMockQueryable(entities);
        _mockRepository.Setup(r => r.DeleteAsync(entityId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(entityId, default);

        // Assert - Verify all child services are called for cascade delete
        _mockRenterDetailService.Verify(
            s => s.DeleteByPropertyIdAsync(entityId, It.IsAny<CancellationToken>()),
            Times.Once,
            "RenterDetailService should be called to delete related records");

        _mockRenterMastService.Verify(
            s => s.DeleteByPropertyIdAsync(entityId, It.IsAny<CancellationToken>()),
            Times.Once,
            "RenterMastService should be called to delete related records");

        _mockRoomWiseService.Verify(
            s => s.DeleteByPropertyIdAsync(entityId, It.IsAny<CancellationToken>()),
            Times.Once,
            "RoomWiseService should be called to delete related records");
    }

    #endregion

    #region Helper Methods

    private PropertyDetailsEntity CreateTestEntity(int id, int propertyId = 1)
    {
        return new PropertyDetailsEntity
        {
            Id = id,
            PropertyId = propertyId,
            IsActive = true,
            MarkedForDeletion = false,
            FloorId = 1,
            SubFloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            RenterDetails = new List<RenterDetailEntity>(),
            Renters = new List<RenterMastEntity>(),
            RoomWiseSubmissionDetails = new List<RoomWiseSubmissionDetailsEntity>()
        };
    }

    private PropertyDetailsDto CreateTestDto(int id)
    {
        return new PropertyDetailsDto
        {
            Id = id,
            PropertyId = 1,
            FloorId = 1,
            SubFloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1
        };
    }

    private CreatePropertyDetailsDto CreateTestCreateDto()
    {
        return new CreatePropertyDetailsDto
        {
            PropertyId = 1,
            FloorId = 1,
            SubFloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            RenterDetails = null,
            Renters = null,
            RoomWiseSubmissionDetails = null
        };
    }

    private UpdatePropertyDetailsDto CreateTestUpdateDto(int id)
    {
        return new UpdatePropertyDetailsDto
        {
            PropertyId = 1,
            FloorId = 1,
            SubFloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            RenterDetails = null,
            Renters = null,
            RoomWiseSubmissionDetails = null
        };
    }

    private void SetupMockQueryable(IQueryable<PropertyDetailsEntity> entities)
    {
        var mockQueryable = entities.ToList().BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);
    }

    #endregion
}
