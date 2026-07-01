using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class WaterConnectionSizeServiceTests
{
    private readonly Mock<IRepository<WaterConnectionSizeEntity, int>> _mockRepository;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly WaterConnectionSizeService _service;

    public WaterConnectionSizeServiceTests()
    {
        _mockRepository = new Mock<IRepository<WaterConnectionSizeEntity, int>>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new WaterConnectionSizeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSuccessfully()
    {
        var createDto = new CreateWaterConnectionSizeDto { ConnectionSize = 0.5m, ConnectionSizeUnit = "inch" };
        var entity = new WaterConnectionSizeEntity { Id = 1, ConnectionSize = 0.5m, ConnectionSizeUnit = "inch", IsActive = true };

        _mockMapper.Setup(m => m.Map<WaterConnectionSizeEntity>(It.IsAny<CreateWaterConnectionSizeDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<WaterConnectionSizeDto>(It.IsAny<WaterConnectionSizeEntity>()))
            .Returns(new WaterConnectionSizeDto { Id = 1, ConnectionSize = 0.5m, ConnectionSizeUnit = "inch" });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0.5m, result.ConnectionSize);
        Assert.Equal("inch", result.ConnectionSizeUnit);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new WaterConnectionSizeEntity { Id = 1, ConnectionSize = 0.5m, ConnectionSizeUnit = "inch", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<WaterConnectionSizeDto>(entity))
            .Returns(new WaterConnectionSizeDto { Id = 1, ConnectionSize = 0.5m, ConnectionSizeUnit = "inch" });

        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(0.5m, result.ConnectionSize);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((WaterConnectionSizeEntity?)null);

        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesSuccessfully()
    {
        var updateDto = new UpdateWaterConnectionSizeDto { ConnectionSize = 1.0m, ConnectionSizeUnit = "cm", IsActive = true };
        var entity = new WaterConnectionSizeEntity { Id = 1, ConnectionSize = 0.5m, ConnectionSizeUnit = "inch", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionSizeDto>(), It.IsAny<WaterConnectionSizeEntity>()))
            .Callback((UpdateWaterConnectionSizeDto src, WaterConnectionSizeEntity dest) =>
            {
                dest.ConnectionSize = src.ConnectionSize;
                dest.ConnectionSizeUnit = src.ConnectionSizeUnit;
                dest.IsActive = src.IsActive;
            });
        _mockMapper.Setup(m => m.Map<WaterConnectionSizeDto>(It.IsAny<WaterConnectionSizeEntity>()))
            .Returns(new WaterConnectionSizeDto { Id = 1, ConnectionSize = 1.0m, ConnectionSizeUnit = "cm" });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1.0m, result.ConnectionSize);
        Assert.Equal("cm", result.ConnectionSizeUnit);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithReferences_ThrowsValidationException()
    {
        var updateDto = new UpdateWaterConnectionSizeDto { IsActive = false };
        var entity = new WaterConnectionSizeEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionSizeDto>(), It.IsAny<WaterConnectionSizeEntity>()))
            .Callback((UpdateWaterConnectionSizeDto src, WaterConnectionSizeEntity dest) => dest.IsActive = src.IsActive);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionSizeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate: referenced by Water Connections and Water Rate Masters"));

        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains(exception.Errors, e => e.Value != null && e.Value.Contains("Cannot deactivate"));
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithoutReferences_UpdatesSuccessfully()
    {
        var updateDto = new UpdateWaterConnectionSizeDto { IsActive = false };
        var entity = new WaterConnectionSizeEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionSizeDto>(), It.IsAny<WaterConnectionSizeEntity>()))
            .Callback((UpdateWaterConnectionSizeDto src, WaterConnectionSizeEntity dest) => dest.IsActive = src.IsActive);
        _mockMapper.Setup(m => m.Map<WaterConnectionSizeDto>(It.IsAny<WaterConnectionSizeEntity>()))
            .Returns(new WaterConnectionSizeDto { Id = 1, IsActive = false });
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionSizeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotDeactivating_DoesNotCheckReferences()
    {
        var updateDto = new UpdateWaterConnectionSizeDto { IsActive = true };
        var entity = new WaterConnectionSizeEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionSizeDto>(), It.IsAny<WaterConnectionSizeEntity>()))
            .Callback((UpdateWaterConnectionSizeDto src, WaterConnectionSizeEntity dest) => dest.IsActive = src.IsActive);
        _mockMapper.Setup(m => m.Map<WaterConnectionSizeDto>(It.IsAny<WaterConnectionSizeEntity>()))
            .Returns(new WaterConnectionSizeDto { Id = 1, IsActive = true });

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<WaterConnectionSizeEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
    {
        var entity = new WaterConnectionSizeEntity { Id = 1, ConnectionSize = 0.5m, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionSizeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        await _service.DeleteAsync(1, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var entity = new WaterConnectionSizeEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionSizeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete: referenced by Water Rate Masters and Water Connections"));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(1, CancellationToken.None));

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WaterConnectionSizeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_DoesNothing()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((WaterConnectionSizeEntity?)null);

        await _service.DeleteAsync(99, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("inch", new[] { 1 })]
    [InlineData("0.5", new[] { 1 })]
    [InlineData("0.5 inch", new[] { 1 })]
    [InlineData("0.5inch", new[] { 1 })]
    [InlineData("1.0", new[] { 2 })]
    [InlineData("cm", new[] { 2 })]
    [InlineData("1.0 cm", new[] { 2 })]
    [InlineData("1.0cm", new[] { 2 })]
    [InlineData("12", new[] { 3 })]
    public async Task GetAllAsync_SearchTermMatchesVariousFormats_ReturnsExpectedIds(string searchTerm, int[] expectedIds)
    {
        // Arrange
        var entities = new List<WaterConnectionSizeEntity>
        {
            new() { Id = 1, ConnectionSize = 0.5m, ConnectionSizeUnit = "inch", IsActive = true },
            new() { Id = 2, ConnectionSize = 1.0m, ConnectionSizeUnit = "cm", IsActive = true },
            new() { Id = 3, ConnectionSize = 12.0m, ConnectionSizeUnit = "mm", IsActive = true }
        };

        var mockQueryable = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        _mockMapper.Setup(m => m.Map<List<WaterConnectionSizeDto>>(It.IsAny<List<WaterConnectionSizeEntity>>()))
            .Returns((List<WaterConnectionSizeEntity> src) => src.Select(x => new WaterConnectionSizeDto
            {
                Id = x.Id,
                ConnectionSize = x.ConnectionSize,
                ConnectionSizeUnit = x.ConnectionSizeUnit,
                IsActive = x.IsActive
            }).ToList());

        var queryParams = new WaterConnectionSizeQueryParameters
        {
            SearchTerm = searchTerm,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedIds.Length, result.Items.Count());
        foreach (var expectedId in expectedIds)
        {
            Assert.Contains(result.Items, item => item.Id == expectedId);
        }
    }
}
