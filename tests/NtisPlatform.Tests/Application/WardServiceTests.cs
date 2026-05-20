using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

public class WardServiceTests
{
    private readonly Mock<IRepository<WardEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly WardService _service;

    public WardServiceTests()
    {
        _mockRepository = new Mock<IRepository<WardEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new WardService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new WardEntity
        {
            Id = 1,
            WardNo = "WKD1",
            ZoneId = 1,
            Description = "????",
            SequenceNo = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<WardDto>(It.IsAny<WardEntity>()))
            .Returns((WardEntity e) => new WardDto
            {
                Id = e.Id,
                WardNo = e.WardNo,
                ZoneId = e.ZoneId,
                Description = e.Description,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("WKD1", result.WardNo);
        Assert.Equal(1, result.ZoneId);
        Assert.Equal("????", result.Description);
        Assert.Equal(1, result.SequenceNo);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<WardEntity>
        {
            new() { Id = 1, WardNo = "MSH", ZoneId = 1, Description = "????", SequenceNo = 1, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now },
            new() { Id = 2, WardNo = "TRG", ZoneId = 1, Description = "??????", SequenceNo = 2, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<WardEntity, WardDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new WardService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new WardQueryParameters
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
        Assert.Contains(items, x => x.WardNo == "MSH");
        Assert.Contains(items, x => x.WardNo == "TRG");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateWardDto
        {
            WardNo = "WKD1",
            ZoneId = 1,
            Description = "????",
            SequenceNo = 1,
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<WardEntity>(It.IsAny<CreateWardDto>()))
            .Returns((CreateWardDto dto) => new WardEntity
            {
                Id = 1,
                WardNo = dto.WardNo,
                ZoneId = dto.ZoneId,

                Description = dto.Description,
                SequenceNo = dto.SequenceNo,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<WardDto>(It.IsAny<WardEntity>()))
            .Returns((WardEntity e) => new WardDto
            {
                Id = e.Id,
                WardNo = e.WardNo,
                Description = e.Description,

                ZoneId = e.ZoneId,

                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("WKD1", result.WardNo);
        Assert.Equal(1, result.ZoneId);
        Assert.Equal("????", result.Description);
        Assert.Equal(1, result.SequenceNo);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateWardDto
        {
            WardNo = "MSH1",
            ZoneId = 2,
            Description = "????",
            SequenceNo = 1,
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new WardEntity
        {
            Id = 1,
            WardNo = "WKD1",
            ZoneId = 1,
            Description = "????",
            SequenceNo = 1,
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
            .Setup(r => r.UpdateAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateWardDto>(), It.IsAny<WardEntity>()))
            .Callback((UpdateWardDto src, WardEntity dest) =>
            {
                dest.WardNo = src.WardNo;
                dest.ZoneId = src.ZoneId;
                dest.Description = src.Description;
                dest.SequenceNo = src.SequenceNo;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<WardDto>(It.IsAny<WardEntity>()))
            .Returns((WardEntity e) => new WardDto
            {
                Id = e.Id,
                WardNo = e.WardNo,
                ZoneId = e.ZoneId,
                Description = e.Description,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("MSH1", existingEntity.WardNo);
        Assert.Equal(2, existingEntity.ZoneId);
        Assert.Equal("????", existingEntity.Description);
        Assert.Equal(1, existingEntity.SequenceNo);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateWardDto
        {
            WardNo = "WKD1",
            ZoneId = 1,
            Description = "????",
            SequenceNo = 1,
            IsActive = true,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new WardEntity
        {

            Id = 1,
            WardNo = "WKD1",

            ZoneId = 1,
            Description = "????",
            SequenceNo = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WardEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateWardDto
        {
            WardNo = "WKD1",
            ZoneId = 1,
            Description = "Ward 1",
            SequenceNo = 1,
            IsActive = false
        };

        var existingEntity = new WardEntity
        {
            Id = 1,
            WardNo = "WKD1",
            ZoneId = 1,
            Description = "Ward 1",
            SequenceNo = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateWardDto>(), It.IsAny<WardEntity>()))
            .Callback((UpdateWardDto src, WardEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<WardEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate Ward. It is referenced by other records."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithoutReferences_Succeeds()
    {
        // Arrange
        var updateDto = new UpdateWardDto
        {
            WardNo = "WKD1",
            ZoneId = 1,
            Description = "Ward 1",
            SequenceNo = 1,
            IsActive = false
        };

        var existingEntity = new WardEntity
        {
            Id = 1,
            WardNo = "WKD1",
            ZoneId = 1,
            Description = "Ward 1",
            SequenceNo = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateWardDto>(), It.IsAny<WardEntity>()))
            .Callback((UpdateWardDto src, WardEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<WardEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.False(existingEntity.IsActive);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new WardEntity
        {
            Id = idToDelete,
            WardNo = "WKD1",
            ZoneId = 1,
            Description = "Ward 1",
            SequenceNo = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<WardEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete Ward. It is referenced by other records."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(idToDelete, CancellationToken.None));

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithoutReferences_Succeeds()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new WardEntity
        {
            Id = idToDelete,
            WardNo = "WKD1",
            ZoneId = 1,
            Description = "Ward 1",
            SequenceNo = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<WardEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task CreateFromRange_CallsServiceAndReturnsOkObjectResult()
    {
        // Arrange
        var mockService = new Mock<IWardService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();
        var mockLogger = new Mock<ILogger<WardController>>();
        var controller = new WardController(mockService.Object, mockCleanupService.Object, mockReferenceValidationService.Object, mockLogger.Object);

        var request = new RangeCreateRequest<CreateWardDto>
        {
            RangeFrom = "1",
            RangeTo = "3",
            Template = new CreateWardDto { ZoneId = 1, IsActive = true, CreatedBy = 1 }
        };
        var rangeResult = new RangeResult<WardDto>(3, 1, null);

        // Match any call to CreateFromRangeAsync, regardless of overload
        mockService
            .Setup(s => s.CreateFromRangeAsync(
                It.IsAny<RangeCreateRequest<CreateWardDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rangeResult);

        mockService
            .Setup(s => s.CreateFromRangeAsync(
                It.IsAny<RangeCreateRequest<CreateWardDto>>(),
                It.IsAny<Func<CreateWardDto, string, int, CreateWardDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rangeResult);

        // Act
        var result = await controller.CreateFromRange(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RangeResult<WardDto>>>(okResult.Value);
        Assert.Equal(rangeResult, apiResponse.Items);
        mockService.Verify(s => s.CreateFromRangeAsync(
            It.IsAny<RangeCreateRequest<CreateWardDto>>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BulkPurge_CallsBulkForceHardDeleteAsync_AndReturnsOkObjectResult()
    {
        // Arrange
        var mockService = new Mock<IWardService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();
        var mockLogger = new Mock<ILogger<WardController>>();
        var controller = new WardController(mockService.Object, mockCleanupService.Object, mockReferenceValidationService.Object, mockLogger.Object);

        var ids = new[] { 1, 2, 3 };
        var bulkResult = new BulkResult<int>(3, 0, new List<int> { 1, 2, 3 });

        // Mock validation to return no references for all IDs
        mockReferenceValidationService.Setup(s => s.GetReferencingTablesWithDataAsync<WardEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        mockCleanupService.Setup(s => s.BulkForceHardDeleteAsync<WardEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await controller.BulkPurge(ids, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<int>>>(okResult.Value);
        Assert.Equal(bulkResult, apiResponse.Items);
        mockCleanupService.Verify(s => s.BulkForceHardDeleteAsync<WardEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
