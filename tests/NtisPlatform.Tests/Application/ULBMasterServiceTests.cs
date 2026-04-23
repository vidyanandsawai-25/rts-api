using System.Data;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Moq;
using NtisPlatform.Application.DTOs.Master.ULBMaster;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for ULBMasterService using BaseCommonCrudService architecture
/// </summary>
public class ULBMasterServiceTests
{
    private readonly Mock<IRepository<ULBMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfigurationProvider> _configurationProviderMock;
    private readonly ULBMasterService _service;

    public ULBMasterServiceTests()
    {
        _repositoryMock = new Mock<IRepository<ULBMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationProviderMock = new Mock<IConfigurationProvider>();

        _mapperMock.Setup(m => m.ConfigurationProvider).Returns(_configurationProviderMock.Object);

        _service = new ULBMasterService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );
    }

    #region GetAllAsync Tests
    
    // Note: GetAllAsync tests require MockQueryable or similar package for EF Core async operations
    // These tests are skipped for now. The service uses BaseCommonCrudService which requires
    // an IAsyncQueryProvider for CountAsync and ToListAsync operations.
    // TODO: Add MockQueryable.Moq package and implement proper async query mocking

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var entity = CreateTestEntity(1, "ULB001", "Test ULB");
        var dto = CreateTestDto(1, "ULB001", "Test ULB");

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<ULBMasterDto>(entity))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("ULB001", result.UlbCode);
        Assert.Equal("Test ULB", result.UlbName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ULBMasterEntity?)null);

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
        var createDto = new CreateULBMasterDto
        {
            UlbCode = "ULB001",
            UlbName = "New ULB",
            UlbNameLocal = "??? ??????",
            UlbTypeId = 1,
            EmailId = "test@ulb.com",
            MobileNo = "1234567890",
            IsActive = true
        };

        var entity = CreateTestEntity(1, createDto.UlbCode, createDto.UlbName);
        var resultDto = CreateTestDto(1, createDto.UlbCode, createDto.UlbName);

        _mapperMock.Setup(x => x.Map<ULBMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ULBMasterDto>(entity))
            .Returns(resultDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createDto.UlbCode, result.UlbCode);
        Assert.Equal(createDto.UlbName, result.UlbName);
        _repositoryMock.Verify(x => x.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CallsMapper_ToConvertDtoToEntity()
    {
        // Arrange
        var createDto = new CreateULBMasterDto
        {
            UlbCode = "ULB001",
            UlbName = "New ULB",
            UlbTypeId = 1,
            IsActive = true
        };

        var entity = CreateTestEntity(1, createDto.UlbCode, createDto.UlbName);
        var resultDto = CreateTestDto(1, createDto.UlbCode, createDto.UlbName);

        _mapperMock.Setup(x => x.Map<ULBMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<ULBMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ULBMasterDto>(It.IsAny<ULBMasterEntity>()))
            .Returns(resultDto);

        // Act
        await _service.CreateAsync(createDto);

        // Assert
        _mapperMock.Verify(x => x.Map<ULBMasterEntity>(createDto), Times.Once);
        _mapperMock.Verify(x => x.Map<ULBMasterDto>(It.IsAny<ULBMasterEntity>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesAndReturnsDto()
    {
        // Arrange
        var existingEntity = CreateTestEntity(1, "ULB001", "Old Name");
        var updateDto = new UpdateULBMasterDto
        {
            UlbCode = "ULB001",
            UlbName = "Updated Name",
            UlbNameLocal = "??????? ???",
            UlbTypeId = 1,
            IsActive = true
        };

        var updatedDto = CreateTestDto(1, "ULB001", "Updated Name");

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity))
            .Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ULBMasterDto>(existingEntity))
            .Returns(updatedDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.UlbName);
        _repositoryMock.Verify(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateULBMasterDto
        {
            UlbCode = "ULB001",
            UlbName = "Updated Name",
            UlbTypeId = 1
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ULBMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ULBMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MapsUpdateDtoToExistingEntity()
    {
        // Arrange
        var existingEntity = CreateTestEntity(1, "ULB001", "Old Name");
        var updateDto = new UpdateULBMasterDto
        {
            UlbCode = "ULB001",
            UlbName = "Updated Name",
            UlbTypeId = 1,
            IsActive = true
        };

        var updatedDto = CreateTestDto(1, "ULB001", "Updated Name");

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity))
            .Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ULBMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ULBMasterDto>(It.IsAny<ULBMasterEntity>()))
            .Returns(updatedDto);

        // Act
        await _service.UpdateAsync(1, updateDto);

        // Assert
        _mapperMock.Verify(x => x.Map(updateDto, existingEntity), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesAndReturnsTrue()
    {
        // Arrange
        var entity = CreateTestEntity(1, "ULB001", "Test ULB");
        
        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ULBMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private static ULBMasterEntity CreateTestEntity(int id, string ulbCode, string ulbName, byte ulbTypeId = 1, bool isActive = true)
    {
        return new ULBMasterEntity
        {
            Id = id,
            UlbCode = ulbCode,
            UlbName = ulbName,
            UlbNameLocal = $"{ulbName} Local",
            UlbTypeId = ulbTypeId,
            EmailId = $"{ulbCode}@test.com",
            MobileNo = "1234567890",
            IsActive = isActive,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
    }

    private static ULBMasterDto CreateTestDto(int id, string ulbCode, string ulbName, byte ulbTypeId = 1, bool isActive = true)
    {
        return new ULBMasterDto
        {
            Id = id,
            UlbCode = ulbCode,
            UlbName = ulbName,
            UlbNameLocal = $"{ulbName} Local",
            UlbTypeId = ulbTypeId,
            EmailId = $"{ulbCode}@test.com",
            MobileNo = "1234567890",
            IsActive = isActive
        };
    }

    #endregion
}
