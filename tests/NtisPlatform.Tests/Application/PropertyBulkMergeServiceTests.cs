using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.PropertyBulkMerge;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using MockQueryable;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class PropertyBulkMergeServiceTests
{
    private readonly Mock<IRepository<PropertyMapMasterEntity, int>> _mockPropertyMapMasterRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyOldRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockPropertyMapDetailRepository;
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IRepository<WardEntity, int>> _mockWardRepository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepository;
    private readonly Mock<IRepository<MergeDetailEntity, int>> _mockMergeDetailRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<PropertyBulkMergeService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyBulkMergeService _service;

    public PropertyBulkMergeServiceTests()
    {
        _mockPropertyMapMasterRepository = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        _mockPropertyOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _mockPropertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockWardRepository = new Mock<IRepository<WardEntity, int>>();
        _mockSocietyRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockMergeDetailRepository = new Mock<IRepository<MergeDetailEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<PropertyBulkMergeService>>();
        _mockMapper = new Mock<IMapper>();

        _mockMapper.Setup(m => m.Map<PropertyMapDetailEntity>(It.IsAny<PropertyMapDetailEntity>()))
            .Returns((PropertyMapDetailEntity src) => src);
        _mockMapper.Setup(m => m.Map<MergeDetailEntity>(It.IsAny<MergeDetailEntity>()))
            .Returns((MergeDetailEntity src) => src);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new PropertyBulkMergeService(
            _mockPropertyMapMasterRepository.Object,
            _mockPropertyOldRepository.Object,
            _mockPropertyMapDetailRepository.Object,
            _mockRepository.Object,
            _mockWardRepository.Object,
            _mockSocietyRepository.Object,
            _mockMergeDetailRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsValidationException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(null!));
        Assert.Contains("Invalid Request", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_EmptyPropertyIdList_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyBulkMergeDto { PropertyIdList = new List<PropertyBulkMergeDetailsDto>(), CreatedBy = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("Invalid Request", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNewProperty_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyBulkMergeDto
        {
            CreatedBy = 1,
            PropertyIdList = new List<PropertyBulkMergeDetailsDto>
            {
                new PropertyBulkMergeDetailsDto { PropertyId = 1, PropertyOldId = 100 },
                new PropertyBulkMergeDetailsDto { PropertyId = 1, PropertyOldId = 101 }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("Duplicate new property found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateOldProperty_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyBulkMergeDto
        {
            CreatedBy = 1,
            PropertyIdList = new List<PropertyBulkMergeDetailsDto>
            {
                new PropertyBulkMergeDetailsDto { PropertyId = 1, PropertyOldId = 100 },
                new PropertyBulkMergeDetailsDto { PropertyId = 2, PropertyOldId = 100 }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("Duplicate old property", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_NullDto_ThrowsValidationException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, null!));
        Assert.Contains("Invalid request", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_EmptyPropertyIdList_ThrowsValidationException()
    {
        // Arrange
        var dto = new UpdatePropertyBulkMergeDto { PropertyIdList = new List<PropertyBulkMergeDetailsDto>(), UpdatedBy = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, dto));
        Assert.Contains("At least one property pair is required", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_OldPropertiesNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new UpdatePropertyBulkMergeDto
        {
            UpdatedBy = 1,
            PropertyIdList = new List<PropertyBulkMergeDetailsDto>
            {
                new PropertyBulkMergeDetailsDto { PropertyId = 1, PropertyOldId = 100 }
            }
        };

        // Return empty list for old properties
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, dto));
        Assert.Contains("One or more old properties not found", ex.Message);
    }
}
