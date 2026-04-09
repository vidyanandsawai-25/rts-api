using NtisPlatform.Application.DTOs.Master.ScreenGroupMaster;
using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master.BankMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for ScreenGroupMasterService
/// </summary>
public class ScreenGroupMasterServiceTests    
{
    private readonly Mock<IRepository<ScreenGroupMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfigurationProvider> _configurationProviderMock;
    private readonly ScreenGroupMasterService _service;

    public ScreenGroupMasterServiceTests()
    {
        _repositoryMock = new Mock<IRepository<ScreenGroupMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationProviderMock = new Mock<IConfigurationProvider>();

        _mapperMock.Setup(m => m.ConfigurationProvider).Returns(_configurationProviderMock.Object);

        _service = new ScreenGroupMasterService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var entity = new ScreenGroupMasterEntity
        {
            Id = 1,
            ScreenGroupCode = "SG001",
            ScreenGroupName = "Administration",
            ScreenGroupNameLocal = "प्रशासन",
            DisplayOrder = 1,
            IsActive = true
        };

        var dto = new ScreenGroupMasterDto
        {
            Id = 1,
            ScreenGroupCode = "SG001",
            ScreenGroupName = "Administration",
            ScreenGroupNameLocal = "प्रशासन",
            DisplayOrder = 1,
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<ScreenGroupMasterDto>(entity))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("SG001", result.ScreenGroupCode);
        Assert.Equal("Administration", result.ScreenGroupName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScreenGroupMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesAndReturnsDto()
    {
        // Arrange
        var createDto = new CreateScreenGroupMasterDto
        {
            ScreenGroupCode = "SG001",
            ScreenGroupName = "Administration",
            ScreenGroupNameLocal = "प्रशासन",
            DisplayOrder = 1,
            IsActive = true
        };

        var entity = new ScreenGroupMasterEntity
        {
            Id = 1,
            ScreenGroupCode = "SG001",
            ScreenGroupName = "Administration",
            DisplayOrder = 1,
            IsActive = true
        };

        var returnDto = new ScreenGroupMasterDto
        {
            Id = 1,
            ScreenGroupCode = "SG001",
            ScreenGroupName = "Administration",
            DisplayOrder = 1,
            IsActive = true
        };

        _mapperMock.Setup(x => x.Map<ScreenGroupMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ScreenGroupMasterDto>(entity))
            .Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SG001", result.ScreenGroupCode);
        Assert.Equal("Administration", result.ScreenGroupName);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<ScreenGroupMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsDto()
    {
        // Arrange
        var updateDto = new UpdateScreenGroupMasterDto
        {
            ScreenGroupCode = "SG001",
            ScreenGroupName = "Administration Updated",
            DisplayOrder = 2,
            IsActive = true
        };

        var existingEntity = new ScreenGroupMasterEntity
        {
            Id = 1,
            ScreenGroupCode = "SG001",
            ScreenGroupName = "Administration",
            DisplayOrder = 1
        };

        var returnDto = new ScreenGroupMasterDto
        {
            Id = 1,
            ScreenGroupCode = "SG001",
            ScreenGroupName = "Administration Updated",
            DisplayOrder = 2,
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity))
            .Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ScreenGroupMasterDto>(existingEntity))
            .Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Administration Updated", result.ScreenGroupName);
        Assert.Equal(2, result.DisplayOrder);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var entity = new ScreenGroupMasterEntity
        {
            Id = 1,
            ScreenGroupCode = "SG001",
            ScreenGroupName = "Administration"
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
    }

    #endregion
    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<ScreenGroupMasterEntity>
        {
            new() { Id = 1, ScreenGroupCode = "SG001", ScreenGroupName = "Admin", IsActive = true, DisplayOrder = 1 },
            new() { Id = 2, ScreenGroupCode = "SG002", ScreenGroupName = "HR", IsActive = true, DisplayOrder = 2 },
            new() { Id = 3, ScreenGroupCode = "SG003", ScreenGroupName = "Finance", IsActive = false, DisplayOrder = 3 }
        };

        var mockQuery = entities.BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ScreenGroupMasterEntity, ScreenGroupMasterDto>();
        });
        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new ScreenGroupMasterService(_repositoryMock.Object, _unitOfWorkMock.Object, mapper);

        var queryParams = new ScreenGroupMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(items, x => x.ScreenGroupCode == "SG001" && x.ScreenGroupName == "Admin");
        Assert.Contains(items, x => x.ScreenGroupCode == "SG002" && x.ScreenGroupName == "HR");
        Assert.Contains(items, x => x.ScreenGroupCode == "SG003" && x.ScreenGroupName == "Finance");
    }
    #endregion
}
