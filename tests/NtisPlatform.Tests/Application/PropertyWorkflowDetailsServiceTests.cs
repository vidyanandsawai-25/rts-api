using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.Property.PropertyWorkflowDetails;
using NtisPlatform.Application.Services.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for PropertyWorkflowDetailsService
/// </summary>
public class PropertyWorkflowDetailsServiceTests
{
    private readonly Mock<IRepository<PropertyWorkflowDetailsEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfigurationProvider> _configurationProviderMock;
    private readonly Mock<IPropertyWorkflowDetailsRepository> _workflowDetailsRepositoryMock;
    private readonly PropertyWorkflowDetailsService _service;

    public PropertyWorkflowDetailsServiceTests()
    {
        _repositoryMock = new Mock<IRepository<PropertyWorkflowDetailsEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationProviderMock = new Mock<IConfigurationProvider>();
        _workflowDetailsRepositoryMock = new Mock<IPropertyWorkflowDetailsRepository>();

        _mapperMock.Setup(m => m.ConfigurationProvider).Returns(_configurationProviderMock.Object);

        _service = new PropertyWorkflowDetailsService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _workflowDetailsRepositoryMock.Object
        );
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var entity = new PropertyWorkflowDetailsEntity
        {
            Id = 1,
            PropertyId = 10,
            WorkflowStageId = 2,
            ModuleId = 3,
            CurrentStatus = true,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        var dto = new PropertyWorkflowDetailsDto
        {
            Id = 1,
            PropertyId = 10,
            WorkflowStageId = 2,
            ModuleId = 3,
            CurrentStatus = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(entity))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.PropertyId);
        Assert.Equal(2, result.WorkflowStageId);
        Assert.True(result.CurrentStatus);
        _repositoryMock.Verify(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowDetailsEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithZeroId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowDetailsEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithNullableModuleId_ReturnsDto()
    {
        // Arrange
        var entity = new PropertyWorkflowDetailsEntity { Id = 1, PropertyId = 10, WorkflowStageId = 2, ModuleId = null, CurrentStatus = true };
        var dto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 2, ModuleId = null, CurrentStatus = true };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(entity)).Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ModuleId);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_SetsCurrentStatusTrueAndReturnsDto()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowDetailsDto
        {
            PropertyId = 10,
            WorkflowStageId = 2,
            ModuleId = 3,
            CreatedBy = 1
        };

        var entity = new PropertyWorkflowDetailsEntity
        {
            PropertyId = 10,
            WorkflowStageId = 2,
            ModuleId = 3,
            IsActive = true,
            CreatedBy = 1
        };

        var returnDto = new PropertyWorkflowDetailsDto
        {
            Id = 1,
            PropertyId = 10,
            WorkflowStageId = 2,
            ModuleId = 3,
            CurrentStatus = true
        };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsEntity>(createDto)).Returns(entity);
        _workflowDetailsRepositoryMock.Setup(x => x.ResetCurrentStatusAsync(10, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(entity)).Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.PropertyId);
        Assert.Equal(2, result.WorkflowStageId);
        Assert.True(result.CurrentStatus);
        Assert.True(entity.CurrentStatus);
        _workflowDetailsRepositoryMock.Verify(x => x.ResetCurrentStatusAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<PropertyWorkflowDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CallsBeginTransactionBeforeWork()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowDetailsDto { PropertyId = 10, WorkflowStageId = 2, CreatedBy = 1 };
        var entity = new PropertyWorkflowDetailsEntity { PropertyId = 10, WorkflowStageId = 2 };
        var returnDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 2, CurrentStatus = true };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsEntity>(createDto)).Returns(entity);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _workflowDetailsRepositoryMock.Setup(x => x.ResetCurrentStatusAsync(10, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(entity)).Returns(returnDto);

        // Act
        await _service.CreateAsync(createDto);

        // Assert
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenExceptionThrown_RollsBackTransaction()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowDetailsDto { PropertyId = 10, WorkflowStageId = 2, CreatedBy = 1 };
        var entity = new PropertyWorkflowDetailsEntity { PropertyId = 10, WorkflowStageId = 2 };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsEntity>(createDto)).Returns(entity);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _workflowDetailsRepositoryMock.Setup(x => x.ResetCurrentStatusAsync(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));
        _unitOfWorkMock.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));

        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNullModuleId_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowDetailsDto { PropertyId = 10, WorkflowStageId = 2, ModuleId = null, CreatedBy = 1 };
        var entity = new PropertyWorkflowDetailsEntity { PropertyId = 10, WorkflowStageId = 2, ModuleId = null };
        var returnDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 2, ModuleId = null, CurrentStatus = true };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsEntity>(createDto)).Returns(entity);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _workflowDetailsRepositoryMock.Setup(x => x.ResetCurrentStatusAsync(10, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(entity)).Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ModuleId);
        Assert.True(result.CurrentStatus);
    }

    [Fact]
    public async Task CreateAsync_ResetsCurrentStatusBeforeAdding()
    {
        // Arrange
        var callOrder = new List<string>();
        var createDto = new CreatePropertyWorkflowDetailsDto { PropertyId = 5, WorkflowStageId = 1, CreatedBy = 1 };
        var entity = new PropertyWorkflowDetailsEntity { PropertyId = 5, WorkflowStageId = 1 };
        var returnDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 5, WorkflowStageId = 1, CurrentStatus = true };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsEntity>(createDto)).Returns(entity);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _workflowDetailsRepositoryMock.Setup(x => x.ResetCurrentStatusAsync(5, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("reset"))
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("add"))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(entity)).Returns(returnDto);

        // Act
        await _service.CreateAsync(createDto);

        // Assert — reset must happen before add
        Assert.Equal(new[] { "reset", "add" }, callOrder);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsDto()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowDetailsDto
        {
            WorkflowStageId = 3,
            ModuleId = 5,
            CurrentStatus = false,
            UpdatedBy = 2
        };

        var existingEntity = new PropertyWorkflowDetailsEntity
        {
            Id = 1,
            PropertyId = 10,
            WorkflowStageId = 2,
            ModuleId = 3,
            CurrentStatus = true,
            IsActive = true
        };

        var returnDto = new PropertyWorkflowDetailsDto
        {
            Id = 1,
            PropertyId = 10,
            WorkflowStageId = 3,
            ModuleId = 5,
            CurrentStatus = false
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity)).Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(existingEntity)).Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.WorkflowStageId);
        Assert.Equal(5, result.ModuleId);
        Assert.False(result.CurrentStatus);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<PropertyWorkflowDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowDetailsDto { WorkflowStageId = 3, UpdatedBy = 2 };

        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowDetailsEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<PropertyWorkflowDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithCurrentStatusChange_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowDetailsDto { WorkflowStageId = 2, CurrentStatus = false, UpdatedBy = 2 };
        var existingEntity = new PropertyWorkflowDetailsEntity { Id = 1, PropertyId = 10, WorkflowStageId = 2, CurrentStatus = true };
        var returnDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 2, CurrentStatus = false };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity)).Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(existingEntity)).Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.CurrentStatus);
    }

    [Fact]
    public async Task UpdateAsync_WithNullModuleId_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowDetailsDto { WorkflowStageId = 2, ModuleId = null, UpdatedBy = 2 };
        var existingEntity = new PropertyWorkflowDetailsEntity { Id = 1, PropertyId = 10, WorkflowStageId = 2, ModuleId = 5 };
        var returnDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 2, ModuleId = null };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity)).Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(existingEntity)).Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ModuleId);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var entity = new PropertyWorkflowDetailsEntity { Id = 1, PropertyId = 10, WorkflowStageId = 2, IsActive = true };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<PropertyWorkflowDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowDetailsEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<PropertyWorkflowDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithZeroId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowDetailsEntity?)null);

        // Act
        var result = await _service.DeleteAsync(0);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_MultipleIds_AllDeleted()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };
        foreach (var id in ids)
        {
            var entity = new PropertyWorkflowDetailsEntity { Id = id, PropertyId = 10, WorkflowStageId = id, IsActive = true };
            _repositoryMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _repositoryMock.Setup(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act & Assert
        foreach (var id in ids)
        {
            var result = await _service.DeleteAsync(id);
            Assert.True(result);
        }
    }

    #endregion

    #region GetByPropertyIdAsync Tests

    [Fact]
    public async Task GetByPropertyIdAsync_WithExistingRecords_ReturnsMappedDtoList()
    {
        // Arrange
        var entities = new List<PropertyWorkflowDetailsEntity>
        {
            new PropertyWorkflowDetailsEntity { Id = 2, PropertyId = 10, WorkflowStageId = 2, CurrentStatus = true,  CreatedDate = DateTime.Now },
            new PropertyWorkflowDetailsEntity { Id = 1, PropertyId = 10, WorkflowStageId = 1, CurrentStatus = false, CreatedDate = DateTime.Now.AddDays(-1) }
        };

        var dtos = new List<PropertyWorkflowDetailsDto>
        {
            new PropertyWorkflowDetailsDto { Id = 2, PropertyId = 10, WorkflowStageId = 2, CurrentStatus = true },
            new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 1, CurrentStatus = false }
        };

        _workflowDetailsRepositoryMock.Setup(x => x.GetByPropertyIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
        _mapperMock.Setup(x => x.Map<List<PropertyWorkflowDetailsDto>>(entities))
            .Returns(dtos);

        // Act
        var result = await _service.GetByPropertyIdAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(10, result[0].PropertyId);
        _workflowDetailsRepositoryMock.Verify(x => x.GetByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_WithNoRecords_ReturnsEmptyList()
    {
        // Arrange
        _workflowDetailsRepositoryMock.Setup(x => x.GetByPropertyIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyWorkflowDetailsEntity>());
        _mapperMock.Setup(x => x.Map<List<PropertyWorkflowDetailsDto>>(It.IsAny<List<PropertyWorkflowDetailsEntity>>()))
            .Returns(new List<PropertyWorkflowDetailsDto>());

        // Act
        var result = await _service.GetByPropertyIdAsync(99);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_ReturnsMostRecentCurrentStatusRecord()
    {
        // Arrange
        var now = DateTime.Now;
        var entities = new List<PropertyWorkflowDetailsEntity>
        {
            new PropertyWorkflowDetailsEntity { Id = 3, PropertyId = 10, WorkflowStageId = 3, CurrentStatus = true,  CreatedDate = now },
            new PropertyWorkflowDetailsEntity { Id = 2, PropertyId = 10, WorkflowStageId = 2, CurrentStatus = false, CreatedDate = now.AddDays(-1) },
            new PropertyWorkflowDetailsEntity { Id = 1, PropertyId = 10, WorkflowStageId = 1, CurrentStatus = false, CreatedDate = now.AddDays(-2) }
        };

        var dtos = entities.Select(e => new PropertyWorkflowDetailsDto
        {
            Id = e.Id,
            PropertyId = e.PropertyId,
            WorkflowStageId = e.WorkflowStageId,
            CurrentStatus = e.CurrentStatus
        }).ToList();

        _workflowDetailsRepositoryMock.Setup(x => x.GetByPropertyIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
        _mapperMock.Setup(x => x.Map<List<PropertyWorkflowDetailsDto>>(entities)).Returns(dtos);

        // Act
        var result = await _service.GetByPropertyIdAsync(10);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result[0].CurrentStatus);  // newest record is current
        Assert.False(result[1].CurrentStatus);
        Assert.False(result[2].CurrentStatus);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_WithSingleRecord_ReturnsSingleItemList()
    {
        // Arrange
        var entities = new List<PropertyWorkflowDetailsEntity>
        {
            new PropertyWorkflowDetailsEntity { Id = 1, PropertyId = 5, WorkflowStageId = 1, CurrentStatus = true }
        };
        var dtos = new List<PropertyWorkflowDetailsDto>
        {
            new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 5, WorkflowStageId = 1, CurrentStatus = true }
        };

        _workflowDetailsRepositoryMock.Setup(x => x.GetByPropertyIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entities);
        _mapperMock.Setup(x => x.Map<List<PropertyWorkflowDetailsDto>>(entities)).Returns(dtos);

        // Act
        var result = await _service.GetByPropertyIdAsync(5);

        // Assert
        Assert.Single(result);
        Assert.Equal(5, result[0].PropertyId);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task CompleteWorkflow_CreateUpdateDelete_AllSuccessful()
    {
        // Arrange - Create
        var createDto = new CreatePropertyWorkflowDetailsDto { PropertyId = 10, WorkflowStageId = 1, CreatedBy = 1 };
        var entity = new PropertyWorkflowDetailsEntity { Id = 1, PropertyId = 10, WorkflowStageId = 1 };
        var createdDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 1, CurrentStatus = true };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsEntity>(createDto)).Returns(entity);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _workflowDetailsRepositoryMock.Setup(x => x.ResetCurrentStatusAsync(10, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(entity)).Returns(createdDto);

        // Act - Create
        var created = await _service.CreateAsync(createDto);
        Assert.NotNull(created);
        Assert.True(entity.CurrentStatus);

        // Arrange - Update
        var updateDto = new UpdatePropertyWorkflowDetailsDto { WorkflowStageId = 2, CurrentStatus = false, UpdatedBy = 2 };
        var updatedDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 2, CurrentStatus = false };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map(updateDto, entity)).Returns(entity);
        _repositoryMock.Setup(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(entity)).Returns(updatedDto);

        // Act - Update
        var updated = await _service.UpdateAsync(1, updateDto);
        Assert.NotNull(updated);

        // Arrange - Delete
        _repositoryMock.Setup(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act - Delete
        var deleted = await _service.DeleteAsync(1);
        Assert.True(deleted);
    }

    [Fact]
    public async Task CreateAsync_FollowedByGetByPropertyId_ReturnsBothRecords()
    {
        // Arrange - Create first record
        var createDto = new CreatePropertyWorkflowDetailsDto { PropertyId = 10, WorkflowStageId = 1, CreatedBy = 1 };
        var entity = new PropertyWorkflowDetailsEntity { Id = 1, PropertyId = 10, WorkflowStageId = 1 };
        var createdDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 1, CurrentStatus = true };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsEntity>(createDto)).Returns(entity);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _workflowDetailsRepositoryMock.Setup(x => x.ResetCurrentStatusAsync(10, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowDetailsDto>(entity)).Returns(createdDto);

        await _service.CreateAsync(createDto);

        // Arrange - GetByPropertyId
        var allEntities = new List<PropertyWorkflowDetailsEntity> { entity };
        var allDtos = new List<PropertyWorkflowDetailsDto> { createdDto };

        _workflowDetailsRepositoryMock.Setup(x => x.GetByPropertyIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(allEntities);
        _mapperMock.Setup(x => x.Map<List<PropertyWorkflowDetailsDto>>(allEntities)).Returns(allDtos);

        // Act
        var result = await _service.GetByPropertyIdAsync(10);

        // Assert
        Assert.Single(result);
        Assert.Equal(10, result[0].PropertyId);
    }

    #endregion
}
