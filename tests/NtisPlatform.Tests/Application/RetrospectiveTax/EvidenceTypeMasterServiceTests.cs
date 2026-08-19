using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.EvidenceTypeMaster;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class EvidenceTypeMasterServiceTests
{
    private readonly Mock<IRepository<EvidenceTypeMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly EvidenceTypeMasterService _service;

    public EvidenceTypeMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<EvidenceTypeMasterEntity, int>>();
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

        _service = new EvidenceTypeMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new EvidenceTypeMasterEntity
        {
            Id = 1,
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsCertificate = true,
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = null,
            UpdatedBy = null
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<EvidenceTypeMasterDto>(It.IsAny<EvidenceTypeMasterEntity>()))
            .Returns((EvidenceTypeMasterEntity e) => new EvidenceTypeMasterDto
            {
                Id = e.Id,
                EvidenceCode = e.EvidenceCode,
                EvidenceName = e.EvidenceName,
                IsCertificate = e.IsCertificate,
                DisplayOrder = e.DisplayOrder,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("OC", result.EvidenceCode);
        Assert.Equal("Occupancy Certificate", result.EvidenceName);
        Assert.True(result.IsCertificate);
        Assert.Equal(1, result.DisplayOrder);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EvidenceTypeMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<EvidenceTypeMasterEntity>
        {
            new() { Id = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate", IsCertificate = true, DisplayOrder = 1, IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 2, EvidenceCode = "CC", EvidenceName = "Completion Certificate", IsCertificate = true, DisplayOrder = 2, IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<EvidenceTypeMasterEntity, EvidenceTypeMasterDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new EvidenceTypeMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new EvidenceTypeMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.EvidenceCode == "OC");
        Assert.Contains(items, x => x.EvidenceCode == "CC");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateEvidenceTypeMasterDto
        {
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsCertificate = true,
            DisplayOrder = 1,
            CreatedBy = 1,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<EvidenceTypeMasterEntity>(It.IsAny<CreateEvidenceTypeMasterDto>()))
            .Returns((CreateEvidenceTypeMasterDto dto) => new EvidenceTypeMasterEntity
            {
                Id = 1,
                EvidenceCode = dto.EvidenceCode,
                EvidenceName = dto.EvidenceName,
                IsCertificate = dto.IsCertificate,
                DisplayOrder = dto.DisplayOrder,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EvidenceTypeMasterEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<EvidenceTypeMasterDto>(It.IsAny<EvidenceTypeMasterEntity>()))
            .Returns((EvidenceTypeMasterEntity e) => new EvidenceTypeMasterDto
            {
                Id = e.Id,
                EvidenceCode = e.EvidenceCode,
                EvidenceName = e.EvidenceName,
                IsCertificate = e.IsCertificate,
                DisplayOrder = e.DisplayOrder,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("OC", result.EvidenceCode);
        Assert.Equal("Occupancy Certificate", result.EvidenceName);
        Assert.True(result.IsCertificate);
        Assert.Equal(1, result.DisplayOrder);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateEvidenceTypeMasterDto
        {
            EvidenceCode = "OC_Updated",
            EvidenceName = "Occupancy Certificate Updated",
            IsCertificate = true,
            DisplayOrder = 2,
            IsActive = true,
            UpdatedBy = 2
        };

        var existingEntity = new EvidenceTypeMasterEntity
        {
            Id = 1,
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsCertificate = true,
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateEvidenceTypeMasterDto>(), It.IsAny<EvidenceTypeMasterEntity>()))
            .Callback((UpdateEvidenceTypeMasterDto src, EvidenceTypeMasterEntity dest) =>
            {
                dest.EvidenceCode = src.EvidenceCode;
                dest.EvidenceName = src.EvidenceName;
                dest.IsCertificate = src.IsCertificate;
                dest.DisplayOrder = src.DisplayOrder;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<EvidenceTypeMasterDto>(It.IsAny<EvidenceTypeMasterEntity>()))
            .Returns((EvidenceTypeMasterEntity e) => new EvidenceTypeMasterDto
            {
                Id = e.Id,
                EvidenceCode = e.EvidenceCode,
                EvidenceName = e.EvidenceName,
                IsCertificate = e.IsCertificate,
                DisplayOrder = e.DisplayOrder,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("OC_Updated", existingEntity.EvidenceCode);
        Assert.Equal("Occupancy Certificate Updated", existingEntity.EvidenceName);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateEvidenceTypeMasterDto
        {
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsCertificate = true,
            DisplayOrder = 1,
            IsActive = true,
            UpdatedBy = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EvidenceTypeMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = new EvidenceTypeMasterEntity
        {
            Id = 1,
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<EvidenceTypeMasterEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<EvidenceTypeMasterEntity>(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EvidenceTypeMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateEvidenceTypeMasterDto
        {
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsActive = false
        };

        var existingEntity = new EvidenceTypeMasterEntity
        {
            Id = 1,
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateEvidenceTypeMasterDto>(), It.IsAny<EvidenceTypeMasterEntity>()))
            .Callback((UpdateEvidenceTypeMasterDto src, EvidenceTypeMasterEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<EvidenceTypeMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate EvidenceTypeMaster. It is referenced by other records."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        _mockReferenceValidator.Verify(v => v.ValidateReferencesAsync<EvidenceTypeMasterEntity>(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithoutReferences_Succeeds()
    {
        // Arrange
        var updateDto = new UpdateEvidenceTypeMasterDto
        {
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsActive = false
        };

        var existingEntity = new EvidenceTypeMasterEntity
        {
            Id = 1,
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateEvidenceTypeMasterDto>(), It.IsAny<EvidenceTypeMasterEntity>()))
            .Callback((UpdateEvidenceTypeMasterDto src, EvidenceTypeMasterEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        _mockMapper
            .Setup(m => m.Map<EvidenceTypeMasterDto>(It.IsAny<EvidenceTypeMasterEntity>()))
            .Returns((EvidenceTypeMasterEntity e) => new EvidenceTypeMasterDto
            {
                Id = e.Id,
                EvidenceCode = e.EvidenceCode,
                EvidenceName = e.EvidenceName,
                IsActive = e.IsActive
            });

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<EvidenceTypeMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockReferenceValidator.Verify(v => v.ValidateReferencesAsync<EvidenceTypeMasterEntity>(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = new EvidenceTypeMasterEntity
        {
            Id = 1,
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<EvidenceTypeMasterEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete record because it is referenced elsewhere."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(idToDelete, CancellationToken.None));

        _mockReferenceValidator.Verify(v => v.ValidateReferencesAsync<EvidenceTypeMasterEntity>(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithoutReferences_Succeeds()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = new EvidenceTypeMasterEntity
        {
            Id = 1,
            EvidenceCode = "OC",
            EvidenceName = "Occupancy Certificate",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<EvidenceTypeMasterEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockReferenceValidator.Verify(v => v.ValidateReferencesAsync<EvidenceTypeMasterEntity>(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<EvidenceTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #region CreateFromRangeAsync

    [Fact]
    public async Task CreateFromRangeAsync_GeneratesEntitiesWithEvidenceCodeFromRange()
    {
        var request = new NtisPlatform.Application.DTOs.Range.RangeCreateRequest<CreateEvidenceTypeMasterDto>
        {
            RangeFrom = "1",
            RangeTo = "2",
            Template = new CreateEvidenceTypeMasterDto { EvidenceName = "Generated {value}", IsCertificate = true, IsActive = true, CreatedBy = 1 }
        };

        _mockMapper
            .Setup(m => m.Map<EvidenceTypeMasterEntity>(It.IsAny<CreateEvidenceTypeMasterDto>()))
            .Returns((CreateEvidenceTypeMasterDto dto) => new EvidenceTypeMasterEntity { EvidenceCode = dto.EvidenceCode, EvidenceName = dto.EvidenceName });

        _mockMapper
            .Setup(m => m.Map<List<EvidenceTypeMasterDto>>(It.IsAny<List<EvidenceTypeMasterEntity>>()))
            .Returns((List<EvidenceTypeMasterEntity> entities) => entities.Select(e => new EvidenceTypeMasterDto { Id = e.Id, EvidenceCode = e.EvidenceCode }).ToList());

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EvidenceTypeMasterEntity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateFromRangeAsync(request, CancellationToken.None);

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        _mockRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<EvidenceTypeMasterEntity>>(list => list.Select(e => e.EvidenceCode).SequenceEqual(new[] { "1", "2" })
                && list.All(e => e.EvidenceName == "Generated 1" || e.EvidenceName == "Generated 2")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
